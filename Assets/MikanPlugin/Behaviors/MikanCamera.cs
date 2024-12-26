using UnityEngine;

namespace MikanXR
{
	public class MikanCamera : MonoBehaviour
	{
		public MikanScene OwnerScene { get; private set; }
		private MikanLogger _logger = null;
		private MikanClient _client = null;

		private Camera _xrCamera = null;

		private RenderTexture _colorRenderTexture = null;
		private RenderTexture _packDepthRenderTexture = null;

		private LineRenderer _frustumRenderer = null;
		private float _hfovRadians = 90f * Mathf.Deg2Rad;
		private float _vfovRadians = 90f * Mathf.Deg2Rad;
		private float _zNear = 0.1f;
		private float _zFar = 100f;

		private long _lastRenderedFrame = 0;
		private MikanClientGraphicsApi _mikanClientGraphicsApi = MikanClientGraphicsApi.UNKNOWN;

		public static MikanCamera SpawnCamera(MikanScene ownerScene)
		{
			// Create the camera game object
			var cameraObject = new GameObject(
				"MikanCameraObject",
				new System.Type[] {
						typeof(Camera),
						typeof(MikanCamera),
						typeof(LineRenderer)});

			// Set the parent of the camera object to the scene
			// All camera transforms coming from Mikan will relative to the scene transform
			cameraObject.transform.parent = ownerScene.transform;

			// Setup the MikanCamera component after all the components are added
			var sceneCamera = cameraObject.GetComponent<MikanCamera>();
			sceneCamera.Setup(ownerScene);

			return sceneCamera;
		}

		public static void DespawnCamera(MikanCamera camera)
		{
			if (camera != null)
			{
				camera.TearDown();
				Destroy(camera.gameObject);
			}
		}

		private void Setup(MikanScene ownerScene)
		{
			OwnerScene = ownerScene;
			_client = ownerScene.OwnerManager.MikanClient;
			_logger = _client.ClientAPI.CoreAPI.Logger;

			// Setup the sibling Unity Camera component for XR rendering
			SetupXRCamera();

			// If we are connected to Mikan, copy the camera intrinsics to the Unity Camera
			// If not, wait for Mikan to connect and we'll copy the settings then
			if (_client.ClientAPI.GetIsConnected())
			{
				HandleCameraIntrinsicsChanged();
			}
			else
			{
				// Generate default geo for the frustum line renderer if not yet connected
				RebuildFrustumGeometry();
			}

			// If we have a valid render target descriptor (defined video source),
			// create a valid render target from the descriptor
			// If not, wait for Mikan to connect and we'll create it then
			if (_client.IsRenderTargetDescriptorValid)
			{
				RecreateRenderTarget(_client.RenderTargetDescriptor);
			}
		}

		private void TearDown()
		{
			_logger.Log(MikanLogLevel.Info, "MikanCamera OnDestroy called");
			DisposeRenderTarget();
		}

		public void Start()
		{
			_logger.Log(MikanLogLevel.Info, "MikanCamera Start called");


			if (_xrCamera == null)
			{
				SetupXRCamera();
			}
		}

		private void RebuildFrustumGeometry()
		{
			if (_frustumRenderer == null)
			{
				_frustumRenderer = gameObject.GetComponent<LineRenderer>();
			}

			if (_frustumRenderer != null)
			{
				_frustumRenderer.useWorldSpace = false;
				_frustumRenderer.startWidth = 0.05f;
				_frustumRenderer.endWidth = 0.05f;
				_frustumRenderer.startColor = Color.yellow;
				_frustumRenderer.endColor = Color.yellow;
				_frustumRenderer.material = new Material(Shader.Find("Unlit/Color"));
				_frustumRenderer.material.color = Color.yellow;
				_frustumRenderer.gameObject.layer = LayerMask.NameToLayer("UI");

				float HRatio = Mathf.Tan(_hfovRadians / 2f);
				float VRatio = Mathf.Tan(_vfovRadians / 2f);

				Vector3 cameraRight = Vector3.right;
				Vector3 cameraUp = Vector3.up;
				Vector3 cameraForward = Vector3.forward;
				Vector3 cameraOrigin = Vector3.zero;

				Vector3 nearX = cameraRight * _zNear * HRatio;
				Vector3 farX = cameraRight * _zFar * HRatio;

				Vector3 nearY = cameraUp * _zNear * VRatio;
				Vector3 farY = cameraUp * _zFar * VRatio;

				Vector3 nearZ = cameraForward * _zNear;
				Vector3 farZ = cameraForward * _zFar;

				Vector3 nearCenter = cameraOrigin + nearZ;
				Vector3 near0 = cameraOrigin + nearX + nearY + nearZ;
				Vector3 near1 = cameraOrigin - nearX + nearY + nearZ;
				Vector3 near2 = cameraOrigin - nearX - nearY + nearZ;
				Vector3 near3 = cameraOrigin + nearX - nearY + nearZ;

				Vector3 far0 = cameraOrigin + farX + farY + farZ;
				Vector3 far1 = cameraOrigin - farX + farY + farZ;
				Vector3 far2 = cameraOrigin - farX - farY + farZ;
				Vector3 far3 = cameraOrigin + farX - farY + farZ;

				_frustumRenderer.positionCount = 26;

				// Far Cone
				_frustumRenderer.SetPosition(0, cameraOrigin);
				_frustumRenderer.SetPosition(1, far0);
				_frustumRenderer.SetPosition(2, far1);

				_frustumRenderer.SetPosition(3, cameraOrigin);
				_frustumRenderer.SetPosition(4, far1);
				_frustumRenderer.SetPosition(5, far2);

				_frustumRenderer.SetPosition(6, cameraOrigin);
				_frustumRenderer.SetPosition(7, far2);
				_frustumRenderer.SetPosition(8, far3);

				_frustumRenderer.SetPosition(9, cameraOrigin);
				_frustumRenderer.SetPosition(10, far3);
				_frustumRenderer.SetPosition(11, far0);

				// Near Cone
				_frustumRenderer.SetPosition(12, cameraOrigin);
				_frustumRenderer.SetPosition(13, near0);
				_frustumRenderer.SetPosition(14, near1);

				_frustumRenderer.SetPosition(15, cameraOrigin);
				_frustumRenderer.SetPosition(16, near1);
				_frustumRenderer.SetPosition(17, near2);

				_frustumRenderer.SetPosition(18, cameraOrigin);
				_frustumRenderer.SetPosition(19, near2);
				_frustumRenderer.SetPosition(20, near3);

				_frustumRenderer.SetPosition(21, cameraOrigin);
				_frustumRenderer.SetPosition(22, near3);
				_frustumRenderer.SetPosition(23, near0);

				// Forward Direction
				_frustumRenderer.SetPosition(24, cameraOrigin);
				_frustumRenderer.SetPosition(25, nearCenter);
			}
		}

		public void SetupXRCamera()
		{
			_logger.Log(MikanLogLevel.Info, "MikanCamera BindXRCamera called");

			_xrCamera = gameObject.GetComponent<Camera>();
			if (_xrCamera != null)
			{
				_logger.Log(MikanLogLevel.Info, "  Found XRCamera");

				// Setup the unity camera component used to render the XR scene
				_xrCamera.stereoTargetEye = StereoTargetEyeMask.None;
				_xrCamera.backgroundColor = new Color(0, 0, 0, 0);
				_xrCamera.clearFlags = CameraClearFlags.SolidColor;
				_xrCamera.forceIntoRenderTexture = true;

				// Disable the camera so that we can drive manual rendering
				_xrCamera.enabled = false;

				// Don't render anything on the UI layer
				_xrCamera.cullingMask = _xrCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
			}
			else
			{
				_logger.Log(MikanLogLevel.Warning, "  Failed to find sibling XRCamera");
			}
		}

		public void HandleCameraIntrinsicsChanged()
		{
			_logger.Log(MikanLogLevel.Info, "MikanCamera HandleCameraIntrinsicsChanged called");
			if (_client.ClientAPI.GetIsConnected())
			{
				var response = _client.ClientAPI.SendRequest(new GetVideoSourceIntrinsics()).FetchResponse();
				if (response.resultCode == MikanAPIResult.Success)
				{
					var intrinsicsResponse = response as MikanVideoSourceIntrinsicsResponse;
					MikanVideoSourceIntrinsics intrinsics = intrinsicsResponse.intrinsics;
					var monoIntrinsics = intrinsics.intrinsics_ptr.Instance as MikanMonoIntrinsics;
					float videoSourcePixelWidth = (float)monoIntrinsics.pixel_width;
					float videoSourcePixelHeight = (float)monoIntrinsics.pixel_height;

					if (_xrCamera != null)
					{
						_xrCamera.fieldOfView = (float)monoIntrinsics.vfov;
						_xrCamera.aspect = videoSourcePixelWidth / videoSourcePixelHeight;
						_xrCamera.nearClipPlane = (float)monoIntrinsics.znear;
						_xrCamera.farClipPlane = (float)monoIntrinsics.zfar;
						_xrCamera.depthTextureMode = DepthTextureMode.Depth;
						_xrCamera.clearFlags = CameraClearFlags.SolidColor;
						_xrCamera.backgroundColor = Color.clear;

						// Update frustum geometry
						_hfovRadians = (float)monoIntrinsics.hfov * Mathf.Deg2Rad;
						_vfovRadians = (float)monoIntrinsics.vfov * Mathf.Deg2Rad;
						_zNear = (float)monoIntrinsics.znear;
						_zFar = (float)monoIntrinsics.zfar;
						RebuildFrustumGeometry();

						_logger.Log(
							MikanLogLevel.Info,
							$"MikanClient: Updated camera params: fov:{_xrCamera.fieldOfView}, aspect:{_xrCamera.aspect}, near:{_xrCamera.nearClipPlane}, far:{_xrCamera.farClipPlane}");
					}
					else
					{
						_logger.Log(MikanLogLevel.Warning, "  No valid XRCamera found to apply intrinsics to.");
					}
				}
				else
				{
					_logger.Log(MikanLogLevel.Error, "  Failed to fetch camera intrinsics!");
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "  Ignoring HandleCameraIntrinsicsChanged - Mikan Disconnected.");
			}
		}

		public bool RecreateRenderTarget(MikanRenderTargetDescriptor RTDesc)
		{
			bool bSuccess = true;
			int width = (int)RTDesc.width;
			int height = (int)RTDesc.height;

			_logger.Log(MikanLogLevel.Info, "MikanCamera RecreateRenderTarget called");

			if (width <= 0 || height <= 0)
			{
				_logger.Log(
					MikanLogLevel.Error, 
					"  MikanCamera: Unable to create render texture. Texture dimension must be higher than zero.");
				return false;
			}

			var createRequest = new AllocateRenderTargetTextures()
			{
				descriptor = RTDesc
			};
			var response = _client.ClientAPI.SendRequest(createRequest).FetchResponse();
			if (response.resultCode != MikanAPIResult.Success)
			{
				_logger.Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture.");
				return false;
			}

			_mikanClientGraphicsApi = RTDesc.graphicsAPI;

			_colorRenderTexture = _client.ClientAPI.RenderTargetAPI.GetColorRenderTexture();
			_packDepthRenderTexture = _client.ClientAPI.RenderTargetAPI.GetPackDepthRenderTexture();

			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = _colorRenderTexture;
			}
			else
			{
				_logger.Log(MikanLogLevel.Warning, "  No valid XRCamera found to assign render target to.");
			}

			_logger.Log(MikanLogLevel.Info, $"  MikanCamera: Created {width}x{height} render target texture");

			return bSuccess;
		}

		public void DisposeRenderTarget()
		{
			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = null;
			}

			_client.ClientAPI.SendRequest(new WriteColorRenderTargetTexture()).AwaitResponse();
			_colorRenderTexture = null;
			_packDepthRenderTexture = null;
		}

		public void CaptureFrame(long inFrameIndex)
		{
			if (_xrCamera != null && _colorRenderTexture != null && _packDepthRenderTexture != null)
			{
				_xrCamera.Render();
				_lastRenderedFrame = inFrameIndex;

				var writeColorRequest = new WriteColorRenderTargetTexture()
				{
					apiColorTexturePtr = _colorRenderTexture.GetNativeTexturePtr()
				};
				_client.ClientAPI.SendRequest(writeColorRequest).AwaitResponse();

				var writeDepthRequest = new WriteDepthRenderTargetTexture()
				{
					apiDepthTexturePtr = _packDepthRenderTexture.GetNativeTexturePtr(),
					zNear = _xrCamera.nearClipPlane,
					zFar = _xrCamera.farClipPlane
				};
				_client.ClientAPI.SendRequest(writeColorRequest).AwaitResponse();

				if (_mikanClientGraphicsApi == MikanClientGraphicsApi.Direct3D11 ||
					_mikanClientGraphicsApi == MikanClientGraphicsApi.OpenGL)
				{
					// Fast interprocess shared texture transfer
					var publishFrameRequest = new PublishRenderTargetTextures
					{
						frameIndex = inFrameIndex
					};
					_client.ClientAPI.SendRequest(publishFrameRequest).AwaitResponse();
				}
			}
		}
	}
}
