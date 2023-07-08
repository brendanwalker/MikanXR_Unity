using System;
using Mikan;
using UnityEngine;

namespace MikanXR
{
	public class MikanCamera : MonoBehaviour
	{
		private Camera _xrCamera= null;
		private RenderTexture _renderTexture= null;
		private ulong _lastRenderedFrame = 0;
		private MikanClientGraphicsApi _mikanClientGraphicsApi= MikanClientGraphicsApi.UNKNOWN;

		void Start()
		{
			MikanManager.Log(MikanLogLevel.Info, "MikanCamera Start called");

			if (_xrCamera == null)
			{
				BindXRCamera();
			}
		}

		void OnDestroy()
		{
			MikanManager.Log(MikanLogLevel.Info, "MikanCamera OnDestroy called");
			DisposeRenderTarget();
		}

		public void BindXRCamera()
		{
			MikanManager.Log(MikanLogLevel.Info, "MikanCamera BindXRCamera called");

			_xrCamera = gameObject.GetComponent<Camera>();
			if (_xrCamera != null)
			{
				MikanManager.Log(MikanLogLevel.Info, "  Found XRCamera");
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, "  Failed to find sibling XRCamera");
			}			
		}

		public void HandleCameraIntrinsicsChanged()
		{
			MikanManager.Log(MikanLogLevel.Info, "MikanCamera HandleCameraIntrinsicsChanged called");
			if (MikanClient.Mikan_GetIsConnected())
			{
				MikanVideoSourceIntrinsics videoSourceIntrinsics = new MikanVideoSourceIntrinsics();
				if (MikanClient.Mikan_GetVideoSourceIntrinsics(videoSourceIntrinsics) == MikanResult.Success)
				{
					MikanMonoIntrinsics monoIntrinsics = videoSourceIntrinsics.intrinsics.mono;
					float videoSourcePixelWidth = (float)monoIntrinsics.pixel_width;
					float videoSourcePixelHeight = (float)monoIntrinsics.pixel_height;

					if (_xrCamera != null)
					{
						_xrCamera.fieldOfView = (float)monoIntrinsics.vfov;
						_xrCamera.aspect = videoSourcePixelWidth / videoSourcePixelHeight;
						_xrCamera.nearClipPlane = (float)monoIntrinsics.znear;
						_xrCamera.farClipPlane = (float)monoIntrinsics.zfar;

						MikanManager.Log(
							MikanLogLevel.Info, 
							$"MikanClient: Updated camera params: fov:{_xrCamera.fieldOfView}, aspect:{_xrCamera.aspect}, near:{_xrCamera.nearClipPlane}, far:{_xrCamera.farClipPlane}");
					}
					else
					{
						MikanManager.Log(MikanLogLevel.Warning, "  No valid XRCamera found to apply intrinsics to.");	
					}
				}
				else
				{
					MikanManager.Log(MikanLogLevel.Error, "  Failed to fetch camera intrinsics!");
				}
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Info, "  Ignoring HandleCameraIntrinsicsChanged - Mikan Disconnected.");
			}
		}

		public bool RecreateRenderTarget(MikanRenderTargetDescriptor RTDesc)
		{
			bool bSuccess = true;
			int width= (int)RTDesc.width;
			int height= (int)RTDesc.height;

			MikanManager.Log(MikanLogLevel.Info, "MikanCamera RecreateRenderTarget called");

			if (width <= 0 || height <= 0)
			{
				MikanManager.Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture. Texture dimension must be higher than zero.");
				return false;
			}

			_mikanClientGraphicsApi= RTDesc.graphicsAPI;

			int depthBufferPrecision = 0;
			_renderTexture = new RenderTexture(width, height, depthBufferPrecision, RenderTextureFormat.ARGB32)
			{
				antiAliasing = 1,
				wrapMode = TextureWrapMode.Clamp,
				useMipMap = false,
				anisoLevel = 0
			};

			if (!_renderTexture.Create())
			{
				MikanManager.Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture.");
				return false;
			}

			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = _renderTexture;
			}
			else
			{
				MikanManager.Log(MikanLogLevel.Warning, "  No valid XRCamera found to assign render target to.");	
			}

			MikanManager.Log(MikanLogLevel.Info, $"  MikanCamera: Created {width}x{height} render target texture");

			return bSuccess;
		}

		public void DisposeRenderTarget()
		{
			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = null;
			}

			if (_renderTexture != null && _renderTexture.IsCreated())
			{
				_renderTexture.Release();
			}
			_renderTexture = null;
		}

		public void CaptureFrame(ulong frame_index)
		{
			_lastRenderedFrame = frame_index;

			if (_xrCamera != null)
			{
				_xrCamera.Render();
			}

			if (_mikanClientGraphicsApi == MikanClientGraphicsApi.Direct3D11 ||
				_mikanClientGraphicsApi == MikanClientGraphicsApi.OpenGL)
			{
				IntPtr textureNativePtr = _renderTexture.GetNativeTexturePtr();

				// Fast interprocess shared texture transfer
				MikanClient.Mikan_PublishRenderTargetTexture(textureNativePtr, frame_index);
			}
		}
	}
}
