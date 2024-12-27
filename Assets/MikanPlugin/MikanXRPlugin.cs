using UnityEngine;

namespace MikanXR
{
	public class MikanXRPlugin : MonoBehaviour
	{
		public Material modelStencilMaterial;

		//public GameObject windowPrefab;
		public GameObject anchorPrefab;
		public GameObject stencilPrefab;

		private bool _isMikanInitialized = false;

		public float DefaultSceneScale = 1.0f;

		private MikanSettings _settings = null;
		public MikanSettings Settings => _settings;

		private MikanManager _mikanManager = null;
		public MikanManager Manager => _mikanManager;

		private MikanClient _mikanClient = null;
		public MikanClient Client => _mikanClient;

		private MikanLogger _logger = null;
		public MikanLogger Logger => _logger;

		public void Awake()
		{
			Log(MikanLogLevel.Info, "MikanXRPlugin - Awake Called");

			// Load the settings
			InitSettings();

			// Don't destroy this object on scene changes
			DontDestroyOnLoad(this);

			// Setup MikanManager, Client and Logger
			SetupMikan();
		}

		protected virtual void InitSettings()
		{
			_settings = new MikanSettings();
			if (!_settings.LoadSettings())
			{
				_settings.SceneScale = DefaultSceneScale;
			}
		}

		public void OnDestroy()
		{
			Log(MikanLogLevel.Info, "MikanXRPlugin - OnDestroy Called");
			Log(MikanLogLevel.Info, StackTraceUtility.ExtractStackTrace());

			TearDownMikan();
		}

		public void OnApplicationQuit()
		{
			Log(MikanLogLevel.Info, "MikanXRPlugin - OnApplicationQuit Called");

			TearDownMikan();
		}

		private void SetupMikan()
		{
			Log(MikanLogLevel.Info, "MikanXRPlugin - SetupMikan Called");

			if (!_isMikanInitialized)
			{
				// Create the MikanManager
				_mikanManager = gameObject.AddComponent<MikanManager>();
				_mikanManager.Setup(this);

				// Cache the MikanClient that the MikanManager created
				_mikanClient = _mikanManager.MikanClient;

				// Register the logger callback
				_logger = _mikanClient.ClientAPI.CoreAPI.Logger;
				_logger.AddLogCallback(Log);

				_isMikanInitialized = true;
			}
		}

		private void TearDownMikan()
		{
			Log(MikanLogLevel.Info, "MikanXRPlugin - TearDownMikan Called");

			if (_isMikanInitialized)
			{
				if (_mikanManager != null)
				{
					_mikanManager.TearDown();
				}

				if (_logger != null)
				{
					_logger.RemoveLogCallback(Log);
					_logger = null;
				}

				_mikanClient = null;
				_isMikanInitialized = false;
			}
		}

		public void Log(MikanLogLevel mikanLogLevel, string log_message)
		{
			switch (mikanLogLevel)
			{
			case MikanLogLevel.Trace:
				AddLogMessage($"Trace | {log_message}");
				break;
			case MikanLogLevel.Debug:
				AddLogMessage($"DEBUG | {log_message}");
				break;
			case MikanLogLevel.Info:
				AddLogMessage($"INFO | {log_message}");
				break;
			case MikanLogLevel.Warning:
				AddLogMessage($"WARNING | {log_message}");
				break;
			case MikanLogLevel.Error:
				AddLogMessage($"ERROR | {log_message}");
				break;
			case MikanLogLevel.Fatal:
				AddLogMessage($"FATAL | {log_message}");
				break;
			}
		}

		private void AddLogMessage(string message)
		{
			Debug.Log(message);
		}
	}
}
