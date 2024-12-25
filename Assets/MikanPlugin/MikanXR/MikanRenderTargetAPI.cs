using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


namespace MikanXR
{
	public class MikanRenderTargetAPI
	{
		private MikanSettings _boundSettings;
		private MikanRequestManager _requestManager;
		private MikanCoreAPI _coreAPI;
		private MikanLogger _logger;

		private System.Type _spoutSenderType = null;
		private MethodInfo _spoutSenderUpdateMethodInfo = null;
		private MethodInfo _spoutSenderDisposeMethodInfo = null;
		private System.Object _colorSenderObject = null;
		private System.Object _depthSenderObject = null;

		private MikanRenderTargetDescriptor _descriptor = null;
		private RenderTexture _colorRenderTexture = null;
		private RenderTexture _packDepthRenderTexture = null;

		private Shader _rgbDepthPackShader = null;
		private Material _depthPackMaterial = null;

		public MikanRenderTargetAPI(MikanRequestManager requestManager)
		{
			_requestManager = requestManager;
			_coreAPI= requestManager.CoreAPI;
			_logger= _coreAPI.Logger;
		}

		public MikanAPIResult Initialize(MikanSettings settings)
		{
			// Use reflection to fetch the KlakSpout API
			if (!FetchSpoutAPI())
			{
				return MikanAPIResult.GeneralError;
			}

			// Missing depth pack shader
			_rgbDepthPackShader = Shader.Find("Custom/RGBAPackDepth");
			if (_rgbDepthPackShader == null)
			{
				_logger.Log(MikanLogLevel.Error, "MikanCoreAPI: Missing Custom/RGBAPackDepth shader");
				return MikanAPIResult.InvalidParam;
			}

			// Create a material used to pack the depth buffer into an RGBA texture
			_depthPackMaterial = new Material(_rgbDepthPackShader);
			_depthPackMaterial.hideFlags = HideFlags.HideAndDontSave;

			// Bind settings events
			BindSettingsEvents(settings);

			return MikanAPIResult.Success;
		}

		private void BindSettingsEvents(MikanSettings settings)
		{
			UnbindSettingsEvents();

			settings.OnSceneScaleChanged.AddListener(HandleSceneScaleChanged);
			_boundSettings = settings;

			HandleSceneScaleChanged(settings.SceneScale);
		}

		private void UnbindSettingsEvents()
		{
			if (_boundSettings != null)
			{
				_boundSettings.OnSceneScaleChanged.RemoveListener(HandleSceneScaleChanged);
				_boundSettings= null;
			}
		}

		private void HandleSceneScaleChanged(float newScale)
		{
			_depthPackMaterial.SetFloat("_SceneScale", newScale);
		}

		public void Shutdown()
		{
			FreeRenderTargetTextures();

			if (_depthPackMaterial != null)
			{
				UnityEngine.Object.Destroy(_depthPackMaterial);
				_depthPackMaterial = null;
			}
		}

		public RenderTexture GetColorRenderTexture()
		{
			return _packDepthRenderTexture;
		}

		public RenderTexture GetPackDepthRenderTexture()
		{
			return _packDepthRenderTexture;
		}

		public MikanResponseFuture TryProcessRequest(MikanRequest request)
		{
			if (request is AllocateRenderTargetTextures)
			{
				return RequestAllocateRenderTargetTextures(request);
			}
			else if (request is WriteColorRenderTargetTexture)
			{
				return RequestWriteColorRenderTargetTexture(request);
			}
			else if (request is WriteDepthRenderTargetTexture)
			{
				return RequestWriteDepthRenderTargetTexture(request);
			}
			else if (request is PublishRenderTargetTextures) 
			{
				return RequestPublishRenderTargetTextures(request);
			}
			else if (request is FreeRenderTargetTextures)
			{
				return RequestFreeRenderTargetTextures(request);
			}

			return new MikanResponseFuture();
		}

		private MikanResponseFuture RequestAllocateRenderTargetTextures(MikanRequest request)
		{
			var allocateRequest = request as AllocateRenderTargetTextures;
			MikanRenderTargetDescriptor descriptor= allocateRequest.descriptor;

			MikanAPIResult result = AllocateRenderTargetTextures(descriptor);
			if (result == MikanAPIResult.Success)
			{
				// Actual descriptor might differ from desired descriptor based on render target writer's capabilities
				MikanRenderTargetDescriptor actualDescriptor;
				result= GetRenderTargetDescriptor(out actualDescriptor);
				if (result == MikanAPIResult.Success)
				{
					return _requestManager.SendRequest(allocateRequest);
				}
			}

			return _requestManager.AddResponseHandler(-1, MikanAPIResult.RequestFailed);
		}

		private MikanResponseFuture RequestWriteColorRenderTargetTexture(MikanRequest request)
		{
			MikanAPIResult result = WriteColorRenderTargetTexture();

			return new MikanResponseFuture(result);
		}

		private MikanResponseFuture RequestWriteDepthRenderTargetTexture(MikanRequest request)
		{
			MikanAPIResult result = WriteDepthRenderTargetTexture();

			return new MikanResponseFuture(result);
		}

		private MikanResponseFuture RequestPublishRenderTargetTextures(MikanRequest request)
		{
			return _requestManager.SendRequest(request);
		}

		private MikanResponseFuture RequestFreeRenderTargetTextures(MikanRequest request)
		{
			// Free any locally allocated resources
			FreeRenderTargetTextures();

			// Tell the server to free the render target resources too
			return _requestManager.SendRequest(request);
		}

		public MikanAPIResult GetRenderTargetDescriptor(out MikanRenderTargetDescriptor out_descriptor)
		{
			out_descriptor = _descriptor;

			return _descriptor != null ? MikanAPIResult.Success : MikanAPIResult.Uninitialized;
		}

		public MikanAPIResult AllocateRenderTargetTextures(MikanRenderTargetDescriptor descriptor)
		{
			if (!_coreAPI.GetIsInitialized())
			{
				return MikanAPIResult.Uninitialized;
			}

			// Verify parameters
			int width = (int)descriptor.width;
			int height = (int)descriptor.height;
			if (width <= 0 || height <= 0)
			{
				_logger.Log(
					MikanLogLevel.Error,
					"  MikanCoreAPI: Unable to create render texture. Texture dimension must be higher than zero.");
				return MikanAPIResult.InvalidParam;
			}

			// Cleanup any existing render target state
			FreeRenderTargetTextures();

			// Initialize the color render textures
			_colorRenderTexture = new RenderTexture(CreateUnityRenderTargetDescriptor(descriptor, true));
			if (!_colorRenderTexture.Create())
			{
				_logger.Log(MikanLogLevel.Error, "MikanCoreAPI: Unable to create render texture.");
				return MikanAPIResult.GeneralError;
			}

			// Initialize the depth render textures
			_packDepthRenderTexture = new RenderTexture(CreateUnityRenderTargetDescriptor(descriptor, false));
			if (!_packDepthRenderTexture.Create())
			{
				_logger.Log(MikanLogLevel.Error, "MikanCoreAPI: Unable to create render texture.");
				return MikanAPIResult.GeneralError;
			}

			string clientUniqueID = _coreAPI.GetClientUniqueID();

			// Create the spout Color sender object
			if (_colorRenderTexture != null)
			{
				_colorSenderObject = CreateSender(clientUniqueID + "_color", _colorRenderTexture);
			}

			// Create the spout Depth sender object
			if (_packDepthRenderTexture != null)
			{
				_depthSenderObject = CreateSender(clientUniqueID + "_depth", _packDepthRenderTexture);
			}

			_descriptor = new MikanRenderTargetDescriptor()
			{
				color_buffer_type = MikanColorBufferType.RGBA32,
				depth_buffer_type = MikanDepthBufferType.PACK_DEPTH_RGBA,
				width = (uint)width,
				height = (uint)height,
				graphicsAPI = descriptor.graphicsAPI
			};

			_logger.Log(MikanLogLevel.Info, $"MikanCoreAPI: Created {width}x{height} render target texture");

			return MikanAPIResult.Success;
		}

		public MikanAPIResult FreeRenderTargetTextures()
		{
			if (!_coreAPI.GetIsInitialized())
			{
				return MikanAPIResult.Uninitialized;
			}

			ReleaseSender(ref _colorSenderObject);
			ReleaseSender(ref _depthSenderObject);

			if (_colorRenderTexture != null && _colorRenderTexture.IsCreated())
			{
				_colorRenderTexture.Release();
			}
			_colorRenderTexture = null;

			if (_packDepthRenderTexture != null && _packDepthRenderTexture.IsCreated())
			{
				_packDepthRenderTexture.Release();
			}
			_packDepthRenderTexture = null;

			_descriptor = null;

			return MikanAPIResult.Success;
		}

		public MikanAPIResult WriteColorRenderTargetTexture()
		{
			if (_colorSenderObject != null)
			{
				_spoutSenderUpdateMethodInfo.Invoke(_colorSenderObject, null);
				return MikanAPIResult.Success;
			}

			return MikanAPIResult.Uninitialized;
		}

		public MikanAPIResult WriteDepthRenderTargetTexture()
		{
			if (_depthPackMaterial != null)
			{
				Graphics.Blit(_colorRenderTexture, _packDepthRenderTexture, _depthPackMaterial);

				if (_depthSenderObject != null)
				{
					_spoutSenderUpdateMethodInfo.Invoke(_depthSenderObject, null);
					return MikanAPIResult.Success;
				}
			}

			return MikanAPIResult.Uninitialized;
		}

		private bool FetchSpoutAPI()
		{
			var spoutRuntimeAssembly = Assembly.Load("Klak.Spout.Runtime");
			if (spoutRuntimeAssembly == null)
			{
				_logger.Log(MikanLogLevel.Error, "Failed to load Klak.Spout.Runtime assembly");
				return false;
			}

			_spoutSenderType = spoutRuntimeAssembly.GetType("Klak.Spout.Sender");
			if (_spoutSenderType == null)
			{
				_logger.Log(MikanLogLevel.Error, "Failed to load Klak.Spout.Sender type");
				return false;
			}

			_spoutSenderUpdateMethodInfo = _spoutSenderType.GetMethod("Update");
			if (_spoutSenderUpdateMethodInfo == null)
			{
				_logger.Log(MikanLogLevel.Error, "Failed to find Klak.Spout.Sender.Update method");
				return false;
			}

			_spoutSenderDisposeMethodInfo = _spoutSenderType.GetMethod("Dispose");
			if (_spoutSenderDisposeMethodInfo == null)
			{
				_logger.Log(MikanLogLevel.Error, "Failed to find Klak.Spout.Sender.Dispose method");
				return false;
			}

			return true;
		}

		private RenderTextureDescriptor CreateUnityRenderTargetDescriptor(
			MikanRenderTargetDescriptor RTDesc,
			bool isColor)
		{
			RenderTextureDescriptor rtDesc = new RenderTextureDescriptor()
			{
				width = (int)RTDesc.width,
				height = (int)RTDesc.height,
				colorFormat = RenderTextureFormat.ARGB32,
				depthBufferBits = isColor ? 32 : 0,
				msaaSamples = 1,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				enableRandomWrite = false,
				memoryless = RenderTextureMemoryless.None,
				useMipMap = false,
				autoGenerateMips = false,
				bindMS = false,
				useDynamicScale = false,
				shadowSamplingMode = ShadowSamplingMode.None,
				vrUsage = VRTextureUsage.None,
				graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
			};

			return rtDesc;
		}

		private System.Object CreateSender(string senderName, Texture renderTexture)
		{
			return Activator.CreateInstance(
				   _spoutSenderType,
				   BindingFlags.Instance
				   | BindingFlags.Public
				   | BindingFlags.NonPublic,
				   null,
				   new System.Object[] { senderName, renderTexture }, // or your actual constructor arguments
				   null
				);
		}

		private void ReleaseSender(ref System.Object senderObject)
		{
			if (senderObject != null)
			{
				if (_spoutSenderDisposeMethodInfo != null)
				{
					_spoutSenderDisposeMethodInfo.Invoke(senderObject, null);
				}

				senderObject = null;
			}
		}
	}
}