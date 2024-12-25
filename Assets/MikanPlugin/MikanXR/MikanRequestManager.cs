using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Linq;

namespace MikanXR
{
	public class MikanRequestManager
	{
		private MikanCoreAPI _coreAPI = null;
		public MikanCoreAPI CoreAPI => _coreAPI;

		private int m_nextRequestID= 0;
		private Dictionary<long, Type> _responseTypeCache = null;

		private class PendingRequest
		{
			public int requestId;
			public TaskCompletionSource<MikanResponse> promise;
		};
		private Dictionary<int, PendingRequest> _pendingRequests;

		public MikanRequestManager(MikanCoreAPI coreAPI)
		{
			_coreAPI = coreAPI;
			_pendingRequests = new Dictionary<int, PendingRequest>();
			_responseTypeCache = new Dictionary<long, Type>();
		}

		public MikanAPIResult Initialize()
		{
			if (_coreAPI.MessageClient == null)
			{
				return MikanAPIResult.GeneralError;
			}

			_coreAPI.MessageClient.SetTextResponseHandler(InternalTextResponseHandler);
			_coreAPI.MessageClient.SetBinaryResponseHandler(InternalBinaryResponseHandler);

			// Build a map from ClassId to MikanResponse Type
			var eventTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
							 where t.IsClass && t.Namespace == "MikanXR" && typeof(MikanResponse).IsAssignableFrom(t)
							 select t;
			eventTypes.ToList().ForEach(t =>
			{
				long classId = Utils.getMikanClassId(t);

				_responseTypeCache[classId] = t;
			});

			return MikanAPIResult.Success;
		}

		void InsertPendingRequest(PendingRequest pendingRequest)
		{
			lock (_pendingRequests)
			{
				_pendingRequests.Add(pendingRequest.requestId, pendingRequest);
			}
		}

		PendingRequest RemovePendingRequest(int requestId)
		{
			PendingRequest pendingRequest = null;

			lock (_pendingRequests)
			{
				if (_pendingRequests.TryGetValue(requestId, out pendingRequest))
				{
					_pendingRequests.Remove(requestId);
				}
			}

			return pendingRequest;
		}

		public MikanResponseFuture AddResponseHandler(int requestId, MikanAPIResult result)
		{
			TaskCompletionSource<MikanResponse> promise = new TaskCompletionSource<MikanResponse>();
			var future= new MikanResponseFuture(this, requestId, promise);

			if (result == MikanAPIResult.Success)
			{
				PendingRequest pendingRequest = new PendingRequest()
				{
					requestId = requestId,
					promise = promise
				};

				InsertPendingRequest(pendingRequest);
			}
			else
			{
				MikanResponse response = new MikanResponse()
				{
					responseTypeName = typeof(MikanResponse).Name,
					responseTypeId = MikanResponse.classId,
					requestId = requestId,
					resultCode = result
				};

				promise.SetResult(response);
			}

			return future;
		}

		public MikanAPIResult CancelRequest(int requestId)
		{
			PendingRequest existingRequest = RemovePendingRequest(requestId);

			return existingRequest != null ? MikanAPIResult.Success : MikanAPIResult.InvalidParam;
		}

		public MikanResponseFuture SendRequest(MikanRequest request)
		{
			// Stamp the request with the next request id
			request.requestId= m_nextRequestID;
			m_nextRequestID++;

			// Serialize the request to a Json string
			string jsonRequestString= 
				JsonSerializer.serializeToJsonString(
					request, request.GetType());

			// Send the request string to Mikan
			MikanAPIResult result = _coreAPI.SendRequestJSON(jsonRequestString);

			// Create a request handler
			return AddResponseHandler(request.requestId, result);
		}

		private bool InternalTextResponseHandler(string utf8ResponseString, out string errorMesg)
		{
			bool success = true;

			errorMesg = "";

			// Check if the key "responseType" exists
			try
			{
				var root = JObject.Parse(utf8ResponseString);

				// Extract the response type name from JSON
				string responseTypeName = "";
				if (root.TryGetValue("responseTypeName", out JToken responseTypeNameElement))
				{
					if (responseTypeNameElement.Type == JTokenType.String)
					{
						responseTypeName = (string)responseTypeNameElement;
					}
					else
					{
						throw new Exception("responseTypeName is not a string");
					}
				}
				else
				{
					throw new Exception("response missing responseTypeName field");
				}

				// Extract the response type Id from JSON
				long responseTypeId = 0;
				if (root.TryGetValue("responseTypeId", out JToken responseTypeIdElement))
				{
					if (responseTypeIdElement.Type == JTokenType.Integer)
					{
						responseTypeId = (long)responseTypeIdElement;
					}
					else
					{
						throw new Exception("responseTypeId is not an integer");
					}
				}
				else
				{
					throw new Exception("response missing responseTypeId field");
				}

				// Extract the request type name from JSON
				int requestId = -1;
				if (root.TryGetValue("requestId", out JToken requestIdElement))
				{
					if (requestIdElement.Type == JTokenType.Integer)
					{
						requestId = (int)requestIdElement;
					}
					else
					{
						throw new Exception("responseTypeId is not an integer");
					}
				}
				else
				{
					throw new Exception("response missing requestId field");
				}

				// Look up the pending request
				PendingRequest pendingRequest = RemovePendingRequest(requestId);

				// Bail if the corresponding pending request is not found
				if (pendingRequest != null)
				{
					// Attempt to create the response object by class name
					MikanResponse response = null;
					if (_responseTypeCache.TryGetValue(responseTypeId, out Type responseType))
					{
						object responseObject = Activator.CreateInstance(responseType);

						// Deserialize the event object from the JSON string
						if (JsonDeserializer.deserializeFromJsonString(utf8ResponseString, responseObject, responseType))
						{
							response = (MikanResponse)responseObject;
						}
						else
						{
							errorMesg = "Failed to deserialize response object from byte array";
						}
					}
					else
					{
						errorMesg = $"Unknown response type: ${responseTypeName}";
					}

					if (response == null)
					{
						// Even though we failed to deserialize the full response,
						// because we have the request id, we can still return an error response
						response = new MikanResponse()
						{
							requestId = requestId,
							resultCode = MikanAPIResult.MalformedResponse
						};
					}

					pendingRequest.promise.SetResult(response);
				}
				else
				{
					errorMesg = $"Invalid pending request id({requestId}) for response type {responseTypeName}";
				}
			}
			catch (Exception e)
			{
				errorMesg = $"Malformed json response: {e.Message}";
				success= false;
			}

			return success;
		}

		private bool InternalBinaryResponseHandler(byte[] responseBytes, out string errorMesg)
		{
			var binaryReader = new BinaryReader(responseBytes);
			bool success = true;

			errorMesg= "";

			try
			{
				// Read the response type id
				long responseTypeId = binaryReader.ReadInt64();

				// Read the response type name
				int requestTypeUTF8StringLength = binaryReader.ReadInt32();
				string responseTypeName =
					requestTypeUTF8StringLength > 0
					? System.Text.Encoding.UTF8.GetString(binaryReader.ReadBytes(requestTypeUTF8StringLength))
					: "";

				// Read the request ID
				int requestId = binaryReader.ReadInt32();

				// Look up the pending request
				PendingRequest pendingRequest = RemovePendingRequest(requestId);

				// Bail if the corresponding pending request is not found
				if (pendingRequest != null)
				{
					// Attempt to create the response object by class name
					MikanResponse response = null;
					if (_responseTypeCache.TryGetValue(responseTypeId, out Type responseType))
					{
						object responseObject = Activator.CreateInstance(responseType);

						// Deserialize the event object from the byte array
						if (BinaryDeserializer.DeserializeFromBytes(responseBytes, responseObject, responseType))
						{
							response = (MikanResponse)responseObject;
						}
						else
						{
							errorMesg= "Failed to deserialize response object from byte array";
						}
					}
					else
					{
						errorMesg= $"Unknown response type: ${responseTypeName}";
					}

					if (response == null)
					{
						// Even though we failed to deserialize the full response,
						// because we have the request id, we can still return an error response
						response = new MikanResponse()
						{
							requestId = requestId,
							resultCode = MikanAPIResult.MalformedResponse
						};
					}

					pendingRequest.promise.SetResult(response);
				}
				else
				{
					errorMesg= $"Invalid pending request id({requestId}) for response type {responseTypeName}";
					success= false;
				}
			}
			catch (Exception e)
			{
				errorMesg= $"Malformed binary response: {e.Message}";
				success= false;
			}

			return success;
		}
	}
}