using System;
using MikanXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace Mikan
{
	public class MikanManager : MonoBehaviour
	{
		public static MikanManager Instance
		{
			get
			{
				return _instance;
			}
		}

		public UnityEvent OnConnectEvent;
		public UnityEvent OnDisconnectEvent;
		public event Action<string> OnMessageEvent;

		void Awake()
		{			
			// For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
			//   and destroy any that are created while one already exists.
			if (_instance != null)
			{
				MikanManager.Log(MikanLogLevel.Warning, $"MikanManager: Instance of {GetType().Name} already exists, destroying.");
				GameObject.DestroyImmediate(this);
				return;
			}

			// If there is a unity logger scipt attached, use that by default
			MikanLogger_Unity defaultLogger= this.gameObject.GetComponent<MikanLogger_Unity>();
			if (defaultLogger != null)
			{
				SetLogHandler(defaultLogger);
			}

			GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
			_instance = this;
			MikanManager.Log(MikanLogLevel.Debug, $"MikanManager: {name}: Awake()");
		}

		void OnEnable()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: OnEnable Called");		
			InitializeMikan();
		}

		void OnDisable()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: OnDisable Called");
			ShutdownMikan();
		}

		void OnApplicationQuit()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: OnApplicationQuit Called");
			ShutdownMikan();
		}

		void InitializeMikan()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: InitializeMikan Called");		

			if (_apiInitialized)
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: Ignoring InitializeMikan. Already initialized.");		
				return;
			}

			MikanClientGraphicsApi graphicsAPI = MikanClientGraphicsApi.UNKNOWN;
			switch (SystemInfo.graphicsDeviceType)
			{
			case GraphicsDeviceType.Direct3D11:
				graphicsAPI = MikanClientGraphicsApi.Direct3D11;
				break;
			case GraphicsDeviceType.OpenGLCore:
			case GraphicsDeviceType.OpenGLES2:
			case GraphicsDeviceType.OpenGLES3:
				graphicsAPI = MikanClientGraphicsApi.OpenGL;
				break;
			}

			_clientInfo = new MikanClientInfo()
			{
				supportedFeatures = (ulong)MikanClientFeatures.RenderTarget_RGBA32,
				engineName = "unity",
				engineVersion = Application.unityVersion,
				applicationName = Application.productName,
				applicationVersion = Application.version,
#if UNITY_2017_2_OR_NEWER
				xrDeviceName = XRSettings.loadedDeviceName,
#endif
				graphicsAPI = graphicsAPI,
				mikanSdkVersion = MikanClient.MIKAN_CLIENT_VERSION_STRING,
			};

			MikanResult result = MikanClient.Mikan_Initialize(MikanLogLevel.Info, MikanManager.CAPILogCallback);
			if (result == MikanResult.Success)
			{
				MikanManager.Log(MikanLogLevel.Info, $"  MikanManager: Successfully initialized Mikan");		
				_apiInitialized = true;
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Error, $"  MikanManager: Failed to initialize Mikan: Error {result}");		
			}
		}

		void ShutdownMikan()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: ShutdownMikan Called");

			if (_apiInitialized)
			{
				FreeRenderBuffers();

				MikanClient.Mikan_Shutdown();
				MikanManager.Log(MikanLogLevel.Info, $"  MikanManager: Shutdown Mikan API");

				_apiInitialized = false;
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: Ignoring ShutdownMikan - Already shutdown.");
			}
		}

		void Update()
		{
			if (MikanClient.Mikan_GetIsConnected())
			{
				MikanEvent mikanEvent = new MikanEvent();
				while (MikanClient.Mikan_PollNextEvent(mikanEvent) == MikanResult.Success)
				{
					HandleMikanEvent(mikanEvent);
				}
			}
			else
			{
				if (_mikanReconnectTimeout <= 0.0f)
				{
					MikanResult result= MikanClient.Mikan_Connect(_clientInfo);
					if (result != MikanResult.Success && 
						result != MikanResult.AlreadyConnected)
					{
						// Reset reconnect attempt timer
						_mikanReconnectTimeout = 1.0f;
						MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: Failed to connect to Mikan. Retry in {_mikanReconnectTimeout} seconds.");
					}
					else
					{
						MikanManager.Log(MikanLogLevel.Info, $"  MikanManager: Successfully connected to Mikan");
					}
				}
				else
				{
					_mikanReconnectTimeout -= Time.deltaTime;
				}
			}
		}

		private IMikanLogger _logHandler= null;
		public void SetLogHandler(IMikanLogger logHandler)
		{
			_logHandler = logHandler;
		}

		private static void CAPILogCallback(int log_level, string log_message)
		{
			MikanManager.Log((MikanLogLevel)log_level, log_message);
		}

		public static void Log(MikanLogLevel log_level, string log_message)
		{
			if (_instance != null && _instance._logHandler != null)
			{
				_instance._logHandler.Log(log_level, log_message);
			}
		}

		void FreeRenderBuffers()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: FreeRenderBuffers Called");

			if (_mikanScene != null)
			{
				MikanCamera mikanCamera = _mikanScene.SceneCamera;
				if (mikanCamera != null)
				{
					mikanCamera.DisposeRenderTarget();
				}
				else
				{
					MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: No camera bound");
				}
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: FreeRenderBuffers - No scene bound");
			}

			MikanClient.Mikan_FreeRenderTargetBuffers();
			_renderTargetMemory= new MikanRenderTargetMemory();
			_renderTargetDescriptor = null;
		}

		MikanRenderTargetDescriptor _renderTargetDescriptor= null;
		public MikanRenderTargetDescriptor RenderTargetDescriptor{
			get { return _renderTargetDescriptor; }
		}

		void ReallocateRenderBuffers()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: ReallocateRenderBuffers Called");

			// Clean up any previously allocated render targets
			FreeRenderBuffers();

			// Fetch the video source information from Mikan
			MikanVideoSourceMode mode = new MikanVideoSourceMode();
			if (MikanClient.Mikan_GetVideoSourceMode(mode) == MikanResult.Success)
			{
				_renderTargetDescriptor = new MikanRenderTargetDescriptor();
				_renderTargetDescriptor.width = (uint)mode.resolution_x;
				_renderTargetDescriptor.height = (uint)mode.resolution_y;
				_renderTargetDescriptor.color_key = new MikanColorRGB()
				{
					r = 0,
					g = 0,
					b = 0
				};
				_renderTargetDescriptor.color_buffer_type = MikanColorBufferType.RGBA32;
				_renderTargetDescriptor.depth_buffer_type = MikanDepthBufferType.NODEPTH;
				_renderTargetDescriptor.graphicsAPI = _clientInfo.graphicsAPI;

				// Allocate any behind the scenes shared memory
				if (MikanClient.Mikan_AllocateRenderTargetBuffers(_renderTargetDescriptor, _renderTargetMemory) == MikanResult.Success)
				{
					MikanManager.Log(MikanLogLevel.Info, "  MikanManager: Allocated render target buffers");
				}
				else
				{
					MikanManager.Log(MikanLogLevel.Error, "  MikanManager: Failed to allocate shared memory");
				}

				// Tell the active scene camera to recreate a matching render target
				if (_mikanScene != null)
				{
					MikanCamera _mikanCamera = _mikanScene.SceneCamera;

					if (_mikanCamera != null)
					{
						_mikanCamera.RecreateRenderTarget(_renderTargetDescriptor);
					}
					else
					{
						MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: ReallocateRenderBuffers - No camera bound");
					}					
				}
				else
				{
					MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: ReallocateRenderBuffers - No scene bound");
				}				
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Error, "MikanManager: Failed to get video source mode");
			}
		}

		void HandleMikanEvent(MikanEvent inEvent)
		{
			switch (inEvent.event_type)
			{
			// App Connection Events
			case MikanEventType.connected:
				HandleMikanConnected();
				break;
			case MikanEventType.disconnected:
				HandleMikanDisconnected();
				break;

			// Video Source Events
			case MikanEventType.videoSourceOpened:
				ReallocateRenderBuffers();
				HandleCameraIntrinsicsChanged();
				break;
			case MikanEventType.videoSourceClosed:
				break;
			case MikanEventType.videoSourceNewFrame:
				HandleNewVideoSourceFrame(inEvent.event_payload.video_source_new_frame);
				break;
			case MikanEventType.videoSourceAttachmentChanged:
				HandleCameraAttachmentChanged();
				break;
			case MikanEventType.videoSourceModeChanged:
			case MikanEventType.videoSourceIntrinsicsChanged:
				ReallocateRenderBuffers();
				HandleCameraIntrinsicsChanged();
				break;
			
			// VR Device Events
			case MikanEventType.vrDevicePoseUpdated:
				break;
			case MikanEventType.vrDeviceListUpdated:
				break;

			// Spatial Anchor Events
			case MikanEventType.anchorPoseUpdated:
				HandleAnchorPoseChanged(inEvent.event_payload.anchor_pose_updated);
				break;
			case MikanEventType.anchorListUpdated:
				HandleAnchorListChanged();
				break;

			// Script Events
			case MikanEventType.scriptMessagePosted:
				HandleScriptMessage(inEvent.event_payload.script_message_posted);
				break;
			}
		}

		void HandleMikanConnected()
		{
			MikanManager.Log(MikanLogLevel.Info, "MikanManager: Connected!");
			ReallocateRenderBuffers();

			if (_mikanScene != null)
			{
				_mikanScene.HandleMikanConnected();
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: No camera bound");
			}			

			if (OnConnectEvent != null)
			{
				OnConnectEvent.Invoke();
			}
		}

		void HandleMikanDisconnected()
		{
			MikanManager.Log(MikanLogLevel.Warning, "MikanManager: Disconnected!");
			FreeRenderBuffers();

			if (_mikanScene != null)
			{
				_mikanScene.HandleMikanDisconnected();
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleMikanDisconnected - No scene bound");
			}

			if (OnDisconnectEvent != null)
			{
				OnDisconnectEvent.Invoke();
			}
		}

		void HandleAnchorListChanged()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorListChanged called.");
			if (_mikanScene != null)
			{
				_mikanScene.HandleAnchorListChanged();
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleAnchorListChanged - No scene bound");
			}			
		}

		void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorPoseChanged called for Anchor ID {AnchorPoseEvent.anchor_id}.");
			if (_mikanScene != null)
			{
				_mikanScene.HandleAnchorPoseChanged(AnchorPoseEvent);
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleAnchorPoseChanged - No scene bound");
			}			
		}

		void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleNewVideoSourceFrame(newFrameEvent);
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleNewVideoSourceFrame - No scene bound");
			}			
		}

		void HandleCameraIntrinsicsChanged()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: HandleCameraIntrinsicsChanged called.");
			if (_mikanScene != null)
			{
				_mikanScene.HandleCameraIntrinsicsChanged();
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleCameraIntrinsicsChanged - No scene bound");
			}			
		}

		void HandleCameraAttachmentChanged()
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: HandleCameraAttachmentChanged called.");

			if (_mikanScene != null)
			{
				_mikanScene.HandleCameraAttachmentChanged();
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"  MikanManager: HandleCameraAttachmentChanged - No scene bound");
			}			
		}

		void HandleScriptMessage(MikanScriptMessageInfo MessageEvent)
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: HandleScriptMessage called (message={MessageEvent.content}).");

			OnMessageEvent?.Invoke(MessageEvent.content);
		}

		public void SendMikanMessage(string MessageString)
		{
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: SendMikanMessage called (message={MessageString}).");

			MikanScriptMessageInfo MessageEvent = new MikanScriptMessageInfo();
			MessageEvent.content = MessageString;

			MikanClient.Mikan_SendScriptMessage(MessageEvent);
		}

		public void BindMikanScene(MikanScene InScene)
		{
			_mikanScene= InScene;
			MikanManager.Log(MikanLogLevel.Info, $"MikanManager: Binding Mikan Scene");
		}

		public void UnbindMikanScene(MikanScene InScene)
		{
			if (_mikanScene == InScene)
			{
				MikanManager.Log(MikanLogLevel.Info, $"MikanManager: Unbinding Mikan Scene");
				_mikanScene = InScene;
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, $"MikanManager: Trying to unbind incorrect scene.");
			}
		}

		public MikanScene CurrentMikanScene
		{
			get {
				return _mikanScene;
			}
		}

		private static MikanManager _instance = null;

		private MikanClientInfo _clientInfo;
		private MikanRenderTargetMemory _renderTargetMemory;
		private bool _apiInitialized = false;
		private float _mikanReconnectTimeout = 0.0f;
		private MikanScene _mikanScene= null;
	}
}
