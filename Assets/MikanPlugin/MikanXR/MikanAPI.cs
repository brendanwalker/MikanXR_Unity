using System;

namespace MikanXR
{
	public static class Constants
	{
		public const int MIKAN_CLIENT_API_VERSION = 0;
		public const int INVALID_MIKAN_ID = -1;
	}

	public class MikanAPI : IDisposable
	{
		public delegate void MikanLogCallback(
			MikanLogLevel log_level,
			string log_message);

		private MikanClient _ownerClient= null;

		private	MikanCoreAPI _coreAPI;
		public MikanCoreAPI CoreAPI => _coreAPI;

		private MikanRenderTargetAPI _renderTargetAPI;
		public MikanRenderTargetAPI RenderTargetAPI => _renderTargetAPI;

		private MikanRequestManager _requestManager;
		private MikanEventManager _eventManager;

		// -- API Lifecycle ----

		public MikanAPI(MikanClient ownerClient)
		{
			_ownerClient= ownerClient;
			_coreAPI = new MikanCoreAPI();
			_requestManager = new MikanRequestManager(_coreAPI);
			_eventManager = new MikanEventManager(_coreAPI);
			_renderTargetAPI = new MikanRenderTargetAPI(_requestManager);
		}

		public void Dispose()
		{			
			Shutdown();
			_requestManager= null;
			_eventManager= null;
			_renderTargetAPI= null;
		}

		public MikanAPIResult Initialize(MikanLogLevel minLogLevel)
		{
			MikanAPIResult result = _coreAPI.Initialize(minLogLevel);
			if (result != MikanAPIResult.Success)
			{
				return result;
			}

			result = _requestManager.Initialize();
			if (result != MikanAPIResult.Success)
			{
				return result;
			}

			_eventManager.Initialize();
			_renderTargetAPI.Initialize(_ownerClient.OwnerManager.OwnerPlugin.Settings);

			return MikanAPIResult.Success;
		}

		public bool GetIsInitialized()
		{
			return _coreAPI.GetIsInitialized();
		}

		public MikanAPIResult Shutdown()
		{
			_renderTargetAPI.Shutdown();
			_coreAPI.Shutdown();

			return MikanAPIResult.Success;
		}

		// -- Client Info ----

		public int GetClientAPIVersion()
		{
			return _coreAPI.GetClientAPIVersion();
		}

		public string GetClientUniqueID()
		{
			return  _coreAPI.GetClientUniqueID();
		}

		public MikanClientInfo AllocateClientInfo()
		{
			MikanClientInfo clientInfo = new MikanClientInfo();

			// Stamp the request with the client API version and client id
			clientInfo.clientId = GetClientUniqueID();

			return clientInfo;
		}

		// -- Client Info ----

		public MikanAPIResult Connect(string host="", string port="")
		{
			return _coreAPI.Connect(host, port);
		}

		public bool GetIsConnected()
		{
			return _coreAPI.GetIsConnected();
		}

		public MikanAPIResult Disconnect()
		{
			return _coreAPI.Disconnect(0, "");
		}

		// -- Messaging ----
		
		public MikanResponseFuture SendRequest(MikanRequest request)
		{
			MikanResponseFuture response = _renderTargetAPI.TryProcessRequest(request);
			if (!response.IsValid())
			{
				response= _requestManager.SendRequest(request);
			}

			return response;
		}

		public MikanAPIResult FetchNextEvent(out MikanEvent outEvent)
		{
			return _eventManager.FetchNextEvent(out outEvent);
		}
	}
}