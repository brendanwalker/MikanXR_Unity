using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;

namespace MikanXR
{
	public class MikanClient : MonoBehaviour
	{
		public MikanManager OwnerManager
		{
			get; private set;
		}
		private MikanLogger _logger = null;

		private MikanAPI _clientApi = null;
		public MikanAPI ClientAPI => _clientApi;

		private string _mikanAddress = "ws://127.0.0.1";
		public string MikanAddress
		{
			get
			{
				return _mikanAddress;
			}
			set
			{
				_mikanAddress = value;
			}
		}

		private string _mikanPort = "8080";
		public string MikanPort
		{
			get
			{
				return _mikanPort;
			}
			set
			{
				_mikanPort = value;
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
		public UnityEvent OnQuadStencilListChanged = new UnityEvent();
		public UnityEvent OnBoxStencilListChanged = new UnityEvent();
		public UnityEvent OnModelStencilListChanged = new UnityEvent();
		public UnityEvent<MikanStencilPoseUpdateEvent> OnStencilPoseChanged = new UnityEvent<MikanStencilPoseUpdateEvent>();
		public UnityEvent<MikanVideoSourceNewFrameEvent> OnNewFrameReceived = new UnityEvent<MikanVideoSourceNewFrameEvent>();
		public UnityEvent OnCameraIntrinsicsChanged = new UnityEvent();
		public UnityEvent OnCameraAttachmentChanged = new UnityEvent();
		public UnityEvent<string> OnScriptMessage = new UnityEvent<string>();

		MikanRenderTargetDescriptor _renderTargetDescriptor;
		public MikanRenderTargetDescriptor RenderTargetDescriptor => _renderTargetDescriptor;
		public bool IsRenderTargetDescriptorValid =>
			_renderTargetDescriptor != null &&
			_renderTargetDescriptor.width > 0 &&
			_renderTargetDescriptor.height > 0;

		public bool Setup(MikanManager ownerManager)
		{
			OwnerManager = ownerManager;
			_clientApi = new MikanAPI(this);
			_logger = _clientApi.CoreAPI.Logger;

			// Attempt to initialize the Mikan API (load MikanCore.dll)
			if (_clientApi.Initialize(MikanLogLevel.Info) == MikanAPIResult.Success)
			{
				_apiInitialized = true;
			}
			else
			{
				_logger.Log(MikanLogLevel.Error, "Failed to initialize MikanAPI");
				return false;
			}

			// Determine which graphics API is being used by Unity
			MikanClientGraphicsApi unityGraphicsAPI;
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
				_logger.Log(MikanLogLevel.Error, $"Unsupported graphics API: {SystemInfo.graphicsDeviceType}");
				return false;
			}

			// Fill out the client info struct and send it to Mikan
			_clientInfo = _clientApi.AllocateClientInfo();
			_clientInfo.engineName = "Unity";
			_clientInfo.engineVersion = Application.unityVersion;
			_clientInfo.applicationName = Application.productName;
			_clientInfo.applicationVersion = Application.version;
			_clientInfo.graphicsAPI = unityGraphicsAPI;
			_clientInfo.supportsRGB24 = SystemInfo.SupportsTextureFormat(TextureFormat.RGB24);
			_clientInfo.supportsRGBA32 = SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32);
			_clientInfo.supportsBGRA32 = SystemInfo.SupportsTextureFormat(TextureFormat.BGRA32);
			_clientInfo.supportsDepth = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);

			return true;
		}

		public void TearDown()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient DisposeClient Called");

			if (_apiInitialized)
			{
				FreeRenderBuffers();

				_clientApi.Shutdown();
				_logger.Log(MikanLogLevel.Info, $"  MikanClient Shutdown Mikan API");

				_apiInitialized = false;
			}
			else
			{
				_logger.Log(MikanLogLevel.Warning, $"  MikanClient Ignoring ShutdownMikan - Already shutdown.");
			}
		}

		public void Update()
		{
			if (_clientApi.GetIsConnected())
			{
				while (_clientApi.FetchNextEvent(out MikanEvent mikanEvent) == MikanAPIResult.Success)
				{
					HandleMikanEvent(mikanEvent);
				}
			}
			else
			{
				if (_mikanReconnectTimeout <= 0f)
				{
					_logger.Log(MikanLogLevel.Info, $"MikanClient: Attempting connect to {_mikanAddress}:{_mikanPort}");

					MikanAPIResult result = _clientApi.Connect(_mikanAddress, _mikanPort);

					if (result != MikanAPIResult.Success || !_clientApi.GetIsConnected())
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
			_logger.Log(MikanLogLevel.Info, $"MikanClient FreeRenderBuffers Called");

			_clientApi.RenderTargetAPI.FreeRenderTargetTextures();

			// Tell any dependent systems that the render buffers have been disposed
			if (OnRenderBufferDisposed != null)
			{
				OnRenderBufferDisposed.Invoke();
			}
		}

		void ReallocateRenderBuffers()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient ReallocateRenderBuffers Called");

			// Clean up any previously allocated render targets
			FreeRenderBuffers();

			// Fetch the video source information from Mikan
			var response = _clientApi.SendRequest(new GetVideoSourceIntrinsics()).FetchResponse();
			if (response.resultCode == MikanAPIResult.Success)
			{
				var intrinsicsResponse = response as MikanVideoSourceIntrinsicsResponse;
				MikanVideoSourceIntrinsics intrinsics = intrinsicsResponse.intrinsics;
				var monoIntrinsics = intrinsics.intrinsics_ptr.Instance as MikanMonoIntrinsics;

				_renderTargetDescriptor = new MikanRenderTargetDescriptor();
				_renderTargetDescriptor.width = (uint)monoIntrinsics.pixel_width;
				_renderTargetDescriptor.height = (uint)monoIntrinsics.pixel_height;
				_renderTargetDescriptor.color_buffer_type = MikanColorBufferType.RGBA32;
				_renderTargetDescriptor.depth_buffer_type = MikanDepthBufferType.PACK_DEPTH_RGBA;
				_renderTargetDescriptor.graphicsAPI = _clientInfo.graphicsAPI;

				// Allocate any behind the scenes render target textures
				var allocateRequest = new AllocateRenderTargetTextures()
				{
					descriptor= _renderTargetDescriptor,
				};
				var allocateResponse = _clientApi.SendRequest(allocateRequest).FetchResponse();
				if (allocateResponse.resultCode == MikanAPIResult.Success)
				{
					_logger.Log(MikanLogLevel.Info, "  MikanClient Allocated render target buffers");
				}
				else
				{
					_logger.Log(MikanLogLevel.Error, "  MikanClient Failed to allocate shared memory");
				}

				// Tell any dependent systems that the render buffers have been created
				if (OnRenderBufferCreated != null)
				{
					OnRenderBufferCreated.Invoke();
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Error, "MikanClient Failed to get video source mode");
			}
		}

		void HandleMikanEvent(MikanEvent inEvent)
		{
			_logger.Log(MikanLogLevel.Debug, $"MikanClient: Event {inEvent.eventTypeName}");

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
				_logger.Log(MikanLogLevel.Info, "MikanAnchorListUpdateEvent event received");
				HandleAnchorListChanged();
			}
			else if (inEvent is MikanStencilPoseUpdateEvent)
			{
				HandleStencilPoseChanged(inEvent as MikanStencilPoseUpdateEvent);
			}
			else if (inEvent is MikanQuadStencilListUpdateEvent)
			{
				_logger.Log(MikanLogLevel.Info, "MikanQuadStencilListUpdateEvent event received");
				HandleQuadStencilListChanged();
			}
			else if (inEvent is MikanBoxStencilListUpdateEvent)
			{
				_logger.Log(MikanLogLevel.Info, "MikanBoxStencilListUpdateEvent event received");
				HandleBoxStencilListChanged();
			}
			else if (inEvent is MikanModelStencilListUpdateEvent)
			{
				_logger.Log(MikanLogLevel.Info, "MikanModelStencilListUpdateEvent event received");
				HandleModelStencilListChanged();
			}
			else if (inEvent is MikanScriptMessagePostedEvent)
			{
				HandleScriptMessage(inEvent as MikanScriptMessagePostedEvent);
			}
		}

		void HandleMikanConnected()
		{
			_logger.Log(MikanLogLevel.Info, "MikanClient Connected!");

			// Send the client info to Mikan upon connection
			var initClientRequest = new InitClientRequest()
			{
				clientInfo = _clientInfo,
			};
			_clientApi.SendRequest(initClientRequest).AwaitResponse();

			ReallocateRenderBuffers();

			if (OnConnected != null)
			{
				OnConnected.Invoke();
			}
		}

		void HandleMikanDisconnected()
		{
			_logger.Log(MikanLogLevel.Warning, "MikanClient Disconnected!");
			FreeRenderBuffers();

			if (OnDisconnected != null)
			{
				OnDisconnected.Invoke();
			}
		}

		void HandleAnchorListChanged()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleAnchorListChanged called.");

			if (OnAnchorListChanged != null)
			{
				OnAnchorListChanged.Invoke();
			}
		}

		void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleAnchorPoseChanged called for Anchor ID {AnchorPoseEvent.anchor_id}.");

			if (OnAnchorPoseChanged != null)
			{
				OnAnchorPoseChanged.Invoke(AnchorPoseEvent);
			}
		}

		void HandleQuadStencilListChanged()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleQuadStencilListChanged called.");

			if (OnQuadStencilListChanged != null)
			{
				OnQuadStencilListChanged.Invoke();
			}
		}

		void HandleBoxStencilListChanged()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleBoxStencilListChanged called.");

			if (OnBoxStencilListChanged != null)
			{
				OnBoxStencilListChanged.Invoke();
			}
		}

		void HandleModelStencilListChanged()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleModelStencilListChanged called.");

			if (OnModelStencilListChanged != null)
			{
				OnModelStencilListChanged.Invoke();
			}
		}

		void HandleStencilPoseChanged(MikanStencilPoseUpdateEvent StencilPoseEvent)
		{
			_logger.Log(MikanLogLevel.Info, 
				$"MikanClient HandleStencilPoseChanged called for Stencil ID {StencilPoseEvent.stencil_id}.");

			if (OnStencilPoseChanged != null)
			{
				OnStencilPoseChanged.Invoke(StencilPoseEvent);
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
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleCameraIntrinsicsChanged called.");

			if (OnCameraIntrinsicsChanged != null)
			{
				OnCameraIntrinsicsChanged.Invoke();
			}
		}

		void HandleCameraAttachmentChanged()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleCameraAttachmentChanged called.");

			if (OnCameraAttachmentChanged != null)
			{
				OnCameraAttachmentChanged.Invoke();
			}
		}

		void HandleScriptMessage(MikanScriptMessagePostedEvent MessageEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient HandleScriptMessage called (message={MessageEvent.message}).");

			OnScriptMessage?.Invoke(MessageEvent.message);
		}

		public void SendMikanMessage(string MessageString)
		{
			_logger.Log(MikanLogLevel.Info, $"MikanClient SendMikanMessage called (message={MessageString}).");

			var MessageRequest = new SendScriptMessage();
			MessageRequest.message = new MikanScriptMessageInfo()
			{
				content = MessageString,
			};
			ClientAPI.SendRequest(MessageRequest);
		}
	}
}
