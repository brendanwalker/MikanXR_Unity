using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace MikanXR
{
	public class MikanEventManager
	{
		public static readonly string WEBSOCKET_DISCONNECT_EVENT = "disconnect";

		private MikanCoreAPI _coreAPI;
		private MikanLogger _logger;
		private Dictionary<long, Type> _eventTypeCache = null;

		public MikanEventManager(MikanCoreAPI coreAPI)
		{
			_coreAPI = coreAPI;
			_logger = coreAPI.Logger;
			_eventTypeCache = new Dictionary<long, Type>();
		}

		public void Initialize()
		{
			// Build a map from ClassId to MikanEvent Type
			var eventTypes = from t in Assembly.GetExecutingAssembly().GetTypes()
					where t.IsClass && t.Namespace == "MikanXR" && typeof(MikanEvent).IsAssignableFrom(t)
					select t;
			eventTypes.ToList().ForEach(t =>
			{
				long classId = Utils.getMikanClassId(t);

				_eventTypeCache[classId] = t;
			});
		}

		public MikanAPIResult FetchNextEvent(out MikanEvent outEvent)
		{
			outEvent= null;

			var result = _coreAPI.FetchNextEvent(out string message);
			if (result == MikanAPIResult.Success)
			{			
				outEvent = parseEventString(message);
				if (outEvent == null)
				{
					_logger.Log(
						MikanLogLevel.Error, 
						$"fetchNextEvent() - failed to parse event string: {message}");
					result = MikanAPIResult.MalformedResponse;
				}
			}
			
			return (MikanAPIResult)result;
		}

		private MikanEvent parseEventString(string utf8ResponseString)
		{
			MikanEvent mikanEvent= null;

			if (utf8ResponseString.StartsWith(WEBSOCKET_DISCONNECT_EVENT))
			{
				int disconnectCode = 0;
				string disconnectReason = "";

				string[] tokens = utf8ResponseString.Split(new char[] {':'});
				if (tokens.Length >= 3)
				{
					int.TryParse(tokens[1], out disconnectCode);
					disconnectReason = tokens[2];
				}

				var disconnectEventPtr = new MikanDisconnectedEvent();
				disconnectEventPtr.eventTypeId = MikanDisconnectedEvent.classId;
				disconnectEventPtr.eventTypeName = typeof(MikanDisconnectedEvent).Name;
				disconnectEventPtr.code = (MikanDisconnectCode)disconnectCode;
				disconnectEventPtr.reason= disconnectReason;

				mikanEvent= disconnectEventPtr;
			}
			else
			{
				var root = JObject.Parse(utf8ResponseString);

				// Check if the "eventTypeName" and "eventTypeId" keys exist
				if (root.TryGetValue("eventTypeName", out JToken eventTypeNameElement) &&
					root.TryGetValue("eventTypeId", out JToken eventTypeIdElement))
				{
					// Check if the value of eventType keys
					if (eventTypeNameElement.Type == JTokenType.String &&
						eventTypeIdElement.Type == JTokenType.Integer)
					{
						// Get the string value of "eventTypeName"
						string eventTypeName = (string)eventTypeNameElement;
						// Get the integer value of "eventTypeId"
						long eventTypeId = (long)eventTypeIdElement;

						// Attempt to create the event object by class name
						if (_eventTypeCache.TryGetValue(eventTypeId, out Type eventType))
						{
							object eventObject = Activator.CreateInstance(eventType);

							// Deserialize the event object from the JSON string
							if (JsonDeserializer.deserializeFromJsonString(utf8ResponseString, eventObject, eventType))
							{
								mikanEvent = (MikanEvent)eventObject;
							}
							else
							{
								_logger.Log(
									MikanLogLevel.Error,
									$"Failed to deserialize event object from JSON string: {utf8ResponseString}");
							}
						}
						else
						{
							_logger.Log(
								MikanLogLevel.Error,
								$"Unknown event type: {eventTypeName} (classId: {eventTypeId})");
						}
					}
					else
					{
						_logger.Log(MikanLogLevel.Error, "eventTypes not of expected types.");
					}
				}
				else
				{
					_logger.Log(MikanLogLevel.Error, "eventType keys not found.");
				}
			}

			return mikanEvent;
		}
	}	
}