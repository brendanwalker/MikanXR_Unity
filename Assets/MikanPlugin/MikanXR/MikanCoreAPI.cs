using System;

namespace MikanXR
{
	public class MikanCoreAPI : IDisposable
	{
		private bool _isInitialized = false;
		private string _clientUniqueID = "";

		private MikanLogger _logger;
		public MikanLogger Logger => _logger;

		private MikanMessageClient _messageClient;
		public MikanMessageClient MessageClient => _messageClient;

		public MikanCoreAPI()
		{
			_clientUniqueID = Guid.NewGuid().ToString();
			_logger = new MikanLogger();
			_messageClient = new MikanMessageClient(_logger);
		}

		public void Dispose()
		{
			Shutdown();

			_messageClient.Dispose();
			_logger.Dispose();
		}

		public MikanAPIResult Initialize(MikanLogLevel minLogLevel)
		{
			if (_isInitialized)
			{
				return MikanAPIResult.Success;
			}

			LoggerSettings loggerSettings = new LoggerSettings()
			{
				min_log_level = minLogLevel,
				log_filename = "MikanCore.log",
				enable_console = true,
			};
			_logger.Init(loggerSettings);

			MikanAPIResult result = _messageClient.Initialize();
			if (result != MikanAPIResult.Success)
			{
				return result;
			}

			_isInitialized = true;

			return MikanAPIResult.Success;
		}

		public void Shutdown()
		{
			_messageClient.Disconnect();
			_isInitialized = false;
		}

		public int GetClientAPIVersion()
		{
			return Constants.MIKAN_CLIENT_API_VERSION;
		}

		public string GetClientUniqueID()
		{
			return _clientUniqueID;
		}

		public bool GetIsInitialized()
		{
			return _isInitialized;
		}

		public MikanAPIResult Connect(string host = "", string port = "")
		{
			return _messageClient.Connect(host, port);
		}

		public bool GetIsConnected()
		{
			return _messageClient.IsConnected;
		}

		public MikanAPIResult Disconnect(ushort code, string reason)
		{
			if (GetIsConnected())
			{
				_messageClient.Disconnect(code, reason);
				return MikanAPIResult.Success;
			}

			return MikanAPIResult.NotConnected;
		}

		public MikanAPIResult FetchNextEvent(out string message)
		{
			return _messageClient.FetchNextEvent(out message);
		}

		public MikanAPIResult SendRequestJSON(
			string jsonString)
		{
			if (_messageClient.IsConnected)
			{
				_messageClient.SendRequest(jsonString);

				return MikanAPIResult.Success;
			}
			else
			{
				return MikanAPIResult.NotConnected;
			}
		}
	}
}
