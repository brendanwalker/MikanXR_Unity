using System;
using MikanXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace MikanXRPlugin
{
    public class MikanClient : MonoBehaviour
    {
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

		private float _mikanReconnectTimeout = 0.0f;

		private MikanClientInfo _clientInfo = null;
        public MikanClientInfo ClientInfo => _clientInfo;

		private bool _apiInitialized = false;
        public bool IsApiInitialized => _apiInitialized;

		public UnityEvent OnConnected = new UnityEvent();
        public UnityEvent OnDisconnected = new UnityEvent();
        public UnityEvent OnRenderBufferCreated = new UnityEvent();
        public UnityEvent OnRenderBufferDisposed = new UnityEvent();
        public UnityEvent OnAnchorListChanged = new UnityEvent();
        public UnityEvent<MikanAnchorPoseUpdateEvent> OnAnchorPoseChanged = new UnityEvent<MikanAnchorPoseUpdateEvent>();
        public UnityEvent<MikanVideoSourceNewFrameEvent> OnNewFrameReceived = new UnityEvent<MikanVideoSourceNewFrameEvent>();
        public UnityEvent OnCameraIntrinsicsChanged = new UnityEvent();
        public UnityEvent OnCameraAttachmentChanged = new UnityEvent();
        public UnityEvent<string> OnScriptMessage = new UnityEvent<string>();

        MikanRenderTargetDescriptor _renderTargetDescriptor;
        public MikanRenderTargetDescriptor RenderTargetDescriptor => _renderTargetDescriptor;
        public bool IsRenderTargetDescriptorValid => _renderTargetDescriptor.width > 0 && _renderTargetDescriptor.height > 0;

        public bool InitClient()
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

        public void DisposeClient()
        {
            Log(MikanLogLevel.Info, $"MikanManager: DisposeClient Called");

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

        void FreeRenderBuffers()
        {
            Log(MikanLogLevel.Info, $"MikanManager: FreeRenderBuffers Called");

            ClientAPI.FreeRenderTargetTextures();

			// Tell any dependent systems that the render buffers have been disposed
			if (OnRenderBufferDisposed != null)
			{
				OnRenderBufferDisposed.Invoke();
			}
		}

        async void ReallocateRenderBuffers()
        {
            Log(MikanLogLevel.Info, $"MikanManager: ReallocateRenderBuffers Called");

            // Clean up any previously allocated render targets
            FreeRenderBuffers();

            // Fetch the video source information from Mikan
            var response = await ClientAPI.VideoSourceAPI.GetVideoSourceIntrinsics();
            if (response.resultCode == MikanResult.Success)
            {
                var intrinsics = response as MikanVideoSourceIntrinsics;

                _renderTargetDescriptor = new MikanRenderTargetDescriptor();
                _renderTargetDescriptor.width = (uint)intrinsics.mono.pixel_width;
                _renderTargetDescriptor.height = (uint)intrinsics.mono.pixel_height;
                _renderTargetDescriptor.color_buffer_type = MikanColorBufferType.RGBA32;
                _renderTargetDescriptor.depth_buffer_type = MikanDepthBufferType.PACK_DEPTH_RGBA;
                _renderTargetDescriptor.graphicsAPI = _clientInfo.graphicsAPI;

                // Allocate any behind the scenes render target textures
                var allocateResponse = await ClientAPI.AllocateRenderTargetTextures(ref _renderTargetDescriptor);
                if (allocateResponse.resultCode == MikanResult.Success)
                {
                    Log(MikanLogLevel.Info, "  MikanManager: Allocated render target buffers");
                }
                else
                {
                    Log(MikanLogLevel.Error, "  MikanManager: Failed to allocate shared memory");
                }

                // Tell any dependent systems that the render buffers have been created
                if (OnRenderBufferCreated != null)
                {
                    OnRenderBufferCreated.Invoke();
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

            if (OnConnected != null)
            {
                OnConnected.Invoke();
            }
        }

        void HandleMikanDisconnected()
        {
            Log(MikanLogLevel.Warning, "MikanManager: Disconnected!");
            FreeRenderBuffers();

            if (OnDisconnected != null)
            {
                OnDisconnected.Invoke();
            }
        }

        void HandleAnchorListChanged()
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorListChanged called.");

            if (OnAnchorListChanged != null)
            {
                OnAnchorListChanged.Invoke();
            }
        }

        void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleAnchorPoseChanged called for Anchor ID {AnchorPoseEvent.anchor_id}.");

            if (OnAnchorPoseChanged != null)
            {
                OnAnchorPoseChanged.Invoke(AnchorPoseEvent);
            }
        }

        void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
        {
            if (OnNewFrameReceived != null)
            {
                OnNewFrameReceived.Invoke(newFrameEvent);
            }
        }

        void HandleCameraIntrinsicsChanged()
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleCameraIntrinsicsChanged called.");

            if (OnCameraIntrinsicsChanged != null)
            {
                OnCameraIntrinsicsChanged.Invoke();
            }
        }

        void HandleCameraAttachmentChanged()
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleCameraAttachmentChanged called.");

            if (OnCameraAttachmentChanged != null)
            {
				OnCameraAttachmentChanged.Invoke();
			}
        }

        void HandleScriptMessage(MikanScriptMessagePostedEvent MessageEvent)
        {
            Log(MikanLogLevel.Info, $"MikanManager: HandleScriptMessage called (message={MessageEvent.message}).");

            OnScriptMessage?.Invoke(MessageEvent.message);
        }

        public void SendMikanMessage(string MessageString)
        {
            Log(MikanLogLevel.Info, $"MikanManager: SendMikanMessage called (message={MessageString}).");

            ClientAPI.ScriptAPI.SendScriptMessage(MessageString);
        }

		protected void Log(MikanLogLevel logLevel, string message)
		{
			MikanManager.Instance?.Log(logLevel, message);
		}
	}
}
