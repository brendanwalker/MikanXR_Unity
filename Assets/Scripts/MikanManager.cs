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
			InitializeMikan();
		}

		void OnDisable()
		{
			ShutdownMikan();
		}

		void OnApplicationQuit()
		{
			ShutdownMikan();
		}

		void InitializeMikan()
		{
			_apiInitialized = false;

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
				_apiInitialized = true;
			}
		}

		void ShutdownMikan()
		{
			if (_apiInitialized)
			{
				FreeRenderBuffers();
				MikanClient.Mikan_Shutdown();
				_apiInitialized = false;
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
			if (_mikanScene != null)
			{
				MikanCamera mikanCamera = _mikanScene.SceneCamera;
				if (mikanCamera != null)
				{
					mikanCamera.DisposeRenderTarget();
				}
			}

			MikanClient.Mikan_FreeRenderTargetBuffers();
			_renderTargetMemory= new MikanRenderTargetMemory();
		}

		void ReallocateRenderBuffers()
		{
			// Clean up any previously allocated render targets
			FreeRenderBuffers();

			// Fetch the video source information from Mikan
			MikanVideoSourceMode mode = new MikanVideoSourceMode();
			if (MikanClient.Mikan_GetVideoSourceMode(mode) == MikanResult.Success)
			{
				MikanRenderTargetDescriptor RTDesc = new MikanRenderTargetDescriptor();
				RTDesc.width = (uint)mode.resolution_x;
				RTDesc.height = (uint)mode.resolution_y;
				RTDesc.color_key = new MikanColorRGB()
				{
					r = 0,
					g = 0,
					b = 0
				};
				RTDesc.color_buffer_type = MikanColorBufferType.RGBA32;
				RTDesc.depth_buffer_type = MikanDepthBufferType.NODEPTH;
				RTDesc.graphicsAPI = _clientInfo.graphicsAPI;

				// Allocate any behind the scenes shared memory
				if (MikanClient.Mikan_AllocateRenderTargetBuffers(RTDesc, _renderTargetMemory) != MikanResult.Success)
				{
					MikanManager.Log(MikanLogLevel.Error, "MikanClient: Failed to allocate shared memory");
				}

				// Tell the active scene camera to recreate a matching render target
				if (_mikanScene != null)
				{
					MikanCamera _mikanCamera = _mikanScene.SceneCamera;

					if (_mikanCamera != null)
					{
						_mikanCamera.RecreateRenderTarget(RTDesc);
					}
				}
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Error, "MikanClient: Failed to get video source mode");
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
			MikanManager.Log(MikanLogLevel.Warning, "Mikan: Connected!");
			ReallocateRenderBuffers();

			if (_mikanScene != null)
			{
				_mikanScene.HandleMikanConnected();
			}

			OnConnectEvent.Invoke();
		}

		void HandleMikanDisconnected()
		{
			MikanManager.Log(MikanLogLevel.Warning, "Mikan: Disconnected!");
			FreeRenderBuffers();

			if (_mikanScene != null)
			{
				_mikanScene.HandleMikanDisconnected();
			}

			OnDisconnectEvent.Invoke();
		}

		void HandleAnchorListChanged()
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleAnchorListChanged();
			}
		}

		void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleAnchorPoseChanged(AnchorPoseEvent);
			}
		}

		void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleNewVideoSourceFrame(newFrameEvent);
			}
		}

		void HandleCameraIntrinsicsChanged()
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleCameraIntrinsicsChanged();
			}
		}

		void HandleCameraAttachmentChanged()
		{
			if (_mikanScene != null)
			{
				_mikanScene.HandleCameraAttachmentChanged();
			}
		}

		void HandleScriptMessage(MikanScriptMessageInfo MessageEvent)
		{
			OnMessageEvent?.Invoke(MessageEvent.content);
		}

		public void SendMikanMessage(string MessageString)
		{
			MikanScriptMessageInfo MessageEvent = new MikanScriptMessageInfo();
			MessageEvent.content = MessageString;

			MikanClient.Mikan_SendScriptMessage(MessageEvent);
		}

		public void BindMikanScene(MikanScene InScene)
		{
			_mikanScene= InScene;
		}

		public void UnbindMikanScene(MikanScene InScene)
		{
			if (_mikanScene == InScene)
			{
				_mikanScene = InScene;
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
