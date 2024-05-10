using System;
using MikanXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace MikanXRPlugin
{
    public class MikanManager : MonoBehaviour
    {
        public static MikanManager Instance => _instance;
        private static MikanManager _instance = null;

        public MikanAPI ClientAPI => _clientApi;
        private MikanAPI _clientApi = new MikanAPI();

        private string _mikanAddress = "ws://127.0.0.1";
        public string MikanAddress
        {
			get { return _mikanAddress; }
			set { 
                _mikanAddress = value; 
                if (ClientAPI.GetIsConnected())
                {
                    _clientApi.Disconnect();
                }
            }
		}

        private string _mikanPort = "8080";
        public string MikanPort
		{
            get { return _mikanPort; }
            set
            {
				_mikanPort = value; 
				if (ClientAPI.GetIsConnected())
                {
					_clientApi.Disconnect();
				}
			}
        }

        public UnityEvent OnConnectEvent;
        public UnityEvent OnDisconnectEvent;
        public event Action<string> OnMessageEvent;

        MikanRenderTargetDescriptor _renderTargetDescriptor;
        public MikanRenderTargetDescriptor RenderTargetDescriptor => _renderTargetDescriptor;
        public bool IsRenderTargetDescriptorValid => _renderTargetDescriptor.width > 0 && _renderTargetDescriptor.height > 0;

        void Awake()
        {
            // For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
            //   and destroy any that are created while one already exists.
            if (_instance != null)
            {
                Log(MikanLogLevel.Warning, $"MikanManager: Instance of {GetType().Name} already exists, destroying.");
                DestroyImmediate(this);
                return;
            }

            // If there is a unity logger scipt attached, use that by default
            MikanLogger_Unity defaultLogger = gameObject.GetComponent<MikanLogger_Unity>();
            if (defaultLogger != null)
            {
                SetLogHandler(defaultLogger);
            }

            DontDestroyOnLoad(this); // Don't destroy this object on scene changes
            _instance = this;
            Log(MikanLogLevel.Debug, $"MikanManager: {name}: Awake()");

            _renderTargetDescriptor = new MikanRenderTargetDescriptor();
            _renderTargetDescriptor.graphicsAPI = MikanClientGraphicsApi.UNKNOWN;
            _renderTargetDescriptor.color_buffer_type = MikanColorBufferType.NOCOLOR;
            _renderTargetDescriptor.depth_buffer_type = MikanDepthBufferType.NODEPTH;
            _renderTargetDescriptor.width = 0;
            _renderTargetDescriptor.height = 0;
        }

        void OnEnable()
        {
            Log(MikanLogLevel.Info, $"MikanManager: OnEnable Called");
            InitializeMikan();
        }

        void OnDisable()
        {
            Log(MikanLogLevel.Info, $"MikanManager: OnDisable Called");
            ShutdownMikan();
        }

        void OnApplicationQuit()
        {
            Log(MikanLogLevel.Info, $"MikanManager: OnApplicationQuit Called");
            ShutdownMikan();
        }

        bool InitializeMikan()
        {
            // Set the logging callback first that so that we can can see init errors
            ClientAPI.SetLogCallback(Log);

            // Attempt to initialize the Mikan API (load MikanCore.dll)
            if (ClientAPI.Initialize(MikanLogLevel.Info) == MikanResult.Success)
            {
				_apiInitialized = true;
			}
            else
			{
                Log(MikanLogLevel.Error, "Failed to initialize MikanAPI");
                return false;
            }

            // Determine which graphics API is being used by Unity
            MikanClientGraphicsApi unityGraphicsAPI = MikanClientGraphicsApi.UNKNOWN;
            switch (SystemInfo.graphicsDeviceType)
            {
                case GraphicsDeviceType.Direct3D11:
                    unityGraphicsAPI = MikanClientGraphicsApi.Direct3D11;
                    break;
                case GraphicsDeviceType.Direct3D12:
                    unityGraphicsAPI = MikanClientGraphicsApi.Direct3D12;
                    break;
                case GraphicsDeviceType.OpenGLCore:
                case GraphicsDeviceType.OpenGLES2:
                case GraphicsDeviceType.OpenGLES3:
                    unityGraphicsAPI = MikanClientGraphicsApi.OpenGL;
                    break;
                default:
                    Log(MikanLogLevel.Error, $"Unsupported graphics API: {SystemInfo.graphicsDeviceType}");
                    return false;
            }

            // Fill out the client info struct and send it to Mikan
            _clientInfo= new MikanClientInfo()
			{
				engineName = "Unity",
				engineVersion = Application.unityVersion,
				applicationName = Application.productName,
				applicationVersion = Application.version,
				graphicsAPI = unityGraphicsAPI,
				supportsRGB24 = SystemInfo.SupportsTextureFormat(TextureFormat.RGB24),
				supportsRGBA32 = SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32),
				supportsBGRA32 = SystemInfo.SupportsTextureFormat(TextureFormat.BGRA32),
				supportsDepth = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)
			};

			if (ClientAPI.SetClientInfo(_clientInfo) != MikanResult.Success)
            {
                Log(MikanLogLevel.Error, "Failed to set Mikan client info");
                return false;
            }

            return true;
        }

        void ShutdownMikan()
        {
            Log(MikanLogLevel.Info, $"MikanManager: ShutdownMikan Called");

            if (_apiInitialized)
            {
                FreeRenderBuffers();

                ClientAPI.Shutdown();
                Log(MikanLogLevel.Info, $"  MikanManager: Shutdown Mikan API");

                _apiInitialized = false;
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: Ignoring ShutdownMikan - Already shutdown.");
            }
        }

        void Update()
        {
            if (ClientAPI.GetIsConnected())
            {
                while (ClientAPI.FetchNextEvent(out MikanEvent mikanEvent) == MikanResult.Success)
                {
                    HandleMikanEvent(mikanEvent);
                }
            }
            else
            {
				if (_mikanReconnectTimeout <= 0f)
				{
                    MikanResult result = ClientAPI.Connect(_mikanAddress, _mikanPort);

					if (result != MikanResult.Success || !ClientAPI.GetIsConnected())
					{
						// Timeout before trying to reconnect
						_mikanReconnectTimeout = 1f;
					}
				}
				else
				{
					_mikanReconnectTimeout -= Time.deltaTime;
				}
			}
        }

        private IMikanLogger _logHandler = null;
        public void SetLogHandler(IMikanLogger logHandler)
        {
            _logHandler = logHandler;
        }

        public void Log(MikanLogLevel log_level, string log_message)
        {
            if (_instance != null && _instance._logHandler != null)
            {
                _instance._logHandler.Log(log_level, log_message);
            }
        }

        void FreeRenderBuffers()
        {
            Log(MikanLogLevel.Info, $"MikanManager: FreeRenderBuffers Called");

            if (_mikanScene != null)
            {
                MikanCamera mikanCamera = _mikanScene.SceneCamera;
                if (mikanCamera != null)
                {
                    mikanCamera.DisposeRenderTarget();
                }
                else
                {
                    Log(MikanLogLevel.Warning, $"  MikanManager: No camera bound");
                }
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: FreeRenderBuffers - No scene bound");
            }

            ClientAPI.FreeRenderTargetTextures();
        }

        async void ReallocateRenderBuffers()
        {
            var client = Instance.ClientAPI;

            Log(MikanLogLevel.Info, $"MikanManager: ReallocateRenderBuffers Called");

            // Clean up any previously allocated render targets
            FreeRenderBuffers();

            // Fetch the video source information from Mikan
            var response = await client.VideoSourceAPI.GetVideoSourceIntrinsics();
            if (response.resultCode == MikanResult.Success)
            {
                var intrinsics = response as MikanVideoSourceIntrinsics;

                _renderTargetDescriptor = new MikanRenderTargetDescriptor();
                _renderTargetDescriptor.width = (uint)intrinsics.mono.pixel_width;
                _renderTargetDescriptor.height = (uint)intrinsics.mono.pixel_height;
                _renderTargetDescriptor.color_buffer_type = MikanColorBufferType.RGBA32;
                _renderTargetDescriptor.depth_buffer_type = MikanDepthBufferType.PACK_DEPTH_RGBA;
                _renderTargetDescriptor.graphicsAPI = _clientInfo.graphicsAPI;

                // Allocate any behind the scenes shared memory
                var allocateResponse = await client.AllocateRenderTargetTextures(ref _renderTargetDescriptor);
                if (allocateResponse.resultCode == MikanResult.Success)
                {
                    Log(MikanLogLevel.Info, "  MikanManager: Allocated render target buffers");
                }
                else
                {
                    Log(MikanLogLevel.Error, "  MikanManager: Failed to allocate shared memory");
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
                        Log(MikanLogLevel.Warning, $"  MikanManager: ReallocateRenderBuffers - No camera bound");
                    }
                }
                else
                {
                    Log(MikanLogLevel.Warning, $"  MikanManager: ReallocateRenderBuffers - No scene bound");
                }
            }
            else
            {
                Log(MikanLogLevel.Error, "MikanManager: Failed to get video source mode");
            }
        }

        void HandleMikanEvent(MikanEvent inEvent)
        {
            if (inEvent is MikanConnectedEvent)
            {
                HandleMikanConnected();
            }
            else if (inEvent is MikanDisconnectedEvent)
            {
                HandleMikanDisconnected();
            }
            else if (inEvent is MikanVideoSourceOpenedEvent)
            {
                ReallocateRenderBuffers();
                HandleCameraIntrinsicsChanged();
            }
            else if (inEvent is MikanVideoSourceClosedEvent)
            {

            }
            else if (inEvent is MikanVideoSourceNewFrameEvent)
            {
                var newFrameEvent = inEvent as MikanVideoSourceNewFrameEvent;

                HandleNewVideoSourceFrame(newFrameEvent);
            }
            else if (inEvent is MikanVideoSourceAttachmentChangedEvent)
            {
                HandleCameraAttachmentChanged();
            }
            else if (inEvent is MikanVideoSourceIntrinsicsChangedEvent ||
                    inEvent is MikanVideoSourceModeChangedEvent)
            {
                ReallocateRenderBuffers();
                HandleCameraIntrinsicsChanged();
            }
            else if (inEvent is MikanVRDevicePoseUpdateEvent)
            {

            }
            else if (inEvent is MikanVRDeviceListUpdateEvent)
            {

            }
            else if (inEvent is MikanAnchorPoseUpdateEvent)
            {
                HandleAnchorPoseChanged(inEvent as MikanAnchorPoseUpdateEvent);
            }
            else if (inEvent is MikanAnchorListUpdateEvent)            
            {
                Log(MikanLogLevel.Info, "MikanAnchorListUpdateEvent event received");
                HandleAnchorListChanged();
            }
            else if (inEvent is MikanScriptMessagePostedEvent)
            {
                HandleScriptMessage(inEvent as MikanScriptMessagePostedEvent);
            }
        }

        void HandleMikanConnected()
        {
            Log(MikanLogLevel.Info, "MikanManager: Connected!");
            ReallocateRenderBuffers();

            if (_mikanScene != null)
            {
                _mikanScene.HandleMikanConnected();
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: No camera bound");
            }

            if (OnConnectEvent != null)
            {
                OnConnectEvent.Invoke();
            }
        }

        void HandleMikanDisconnected()
        {
            Log(MikanLogLevel.Warning, "MikanManager: Disconnected!");
            FreeRenderBuffers();

            if (_mikanScene != null)
            {
                _mikanScene.HandleMikanDisconnected();
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: HandleMikanDisconnected - No scene bound");
            }

            if (OnDisconnectEvent != null)
            {
                OnDisconnectEvent.Invoke();
            }
        }

        void HandleAnchorListChanged()
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorListChanged called.");
            if (_mikanScene != null)
            {
                _mikanScene.HandleAnchorListChanged();
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: HandleAnchorListChanged - No scene bound");
            }
        }

        void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorPoseChanged called for Anchor ID {AnchorPoseEvent.anchor_id}.");
            if (_mikanScene != null)
            {
                _mikanScene.HandleAnchorPoseChanged(AnchorPoseEvent);
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: HandleAnchorPoseChanged - No scene bound");
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
            Log(MikanLogLevel.Info, $"MikanManager: HandleCameraIntrinsicsChanged called.");
            if (_mikanScene != null)
            {
                _mikanScene.HandleCameraIntrinsicsChanged();
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: HandleCameraIntrinsicsChanged - No scene bound");
            }
        }

        void HandleCameraAttachmentChanged()
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleCameraAttachmentChanged called.");

            if (_mikanScene != null)
            {
                _mikanScene.HandleCameraAttachmentChanged();
            }
            else
            {
                Log(MikanLogLevel.Warning, $"  MikanManager: HandleCameraAttachmentChanged - No scene bound");
            }
        }

        void HandleScriptMessage(MikanScriptMessagePostedEvent MessageEvent)
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleScriptMessage called (message={MessageEvent.message}).");

            OnMessageEvent?.Invoke(MessageEvent.message);
        }

        public void SendMikanMessage(string MessageString)
        {
            Log(MikanLogLevel.Info, $"MikanManager: SendMikanMessage called (message={MessageString}).");

            ClientAPI.ScriptAPI.SendScriptMessage(MessageString);
        }

        public void BindMikanScene(MikanScene InScene)
        {
            _mikanScene = InScene;
            Log(MikanLogLevel.Info, $"MikanManager: Binding Mikan Scene");
        }

        public void UnbindMikanScene(MikanScene InScene)
        {
            if (_mikanScene == InScene)
            {
                Log(MikanLogLevel.Info, $"MikanManager: Unbinding Mikan Scene");
                _mikanScene = InScene;
            }
            else
            {
                Log(MikanLogLevel.Warning, $"MikanManager: Trying to unbind incorrect scene.");
            }
        }

        public MikanScene CurrentMikanScene
        {
            get
            {
                return _mikanScene;
            }
        }

        public object XRSettings
        {
            get;
            private set;
        }

        private MikanClientInfo _clientInfo;
        private bool _apiInitialized = false;
        private float _mikanReconnectTimeout = 0.0f;
        private MikanScene _mikanScene = null;
    }
}
