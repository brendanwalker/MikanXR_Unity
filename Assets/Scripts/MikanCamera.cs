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
			_xrCamera = gameObject.GetComponent<Camera>();
		}

		void OnDestroy()
		{
			DisposeRenderTarget();
		}

		public void HandleCameraIntrinsicsChanged()
		{
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
				}
			}
		}

		public bool RecreateRenderTarget(MikanRenderTargetDescriptor RTDesc)
		{
			bool bSuccess = true;
			int width= (int)RTDesc.width;
			int height= (int)RTDesc.height;

			if (width <= 0 || height <= 0)
			{
				MikanManager.Log(MikanLogLevel.Error, "MikanClient: Unable to create render texture. Texture dimension must be higher than zero.");
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
				MikanManager.Log(MikanLogLevel.Error, "MikanClient: Unable to create render texture.");
				return false;
			}

			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = _renderTexture;
			}

			MikanManager.Log(MikanLogLevel.Info, $"MikanClient: Created {width}x{height} render target texture");

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
