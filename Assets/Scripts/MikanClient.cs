using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.Experimental.Rendering;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace MikanXR.SDK.Unity
{
    using MikanSpatialAnchorID = System.Int32;

    [System.Serializable]
    public class MikanPoseUpdateEvent : UnityEvent<MikanMatrix4f>
    {
    }

    [HelpURL("https://github.com/MikanXR/MikanXR_Unity")]
    [AddComponentMenu("MikanXR/Mikan")]
    public class MikanClient : MonoBehaviour
    {
        private static MikanClient _instance = null;

        private MikanClientInfo _clientInfo;
        private MikanRenderTargetMemory _renderTargetMemory;
        private MikanStencilQuad _stencilQuad;
        private Matrix4x4 _originSpatialAnchorXform = Matrix4x4.identity;
        private RenderTexture _renderTexture;
        //private Texture2D _externalTexture;
        private AsyncGPUReadbackRequest _readbackRequest = new AsyncGPUReadbackRequest();

        private bool _enabled = false;
        private bool _apiInitialized = false;
        private float _mikanReconnectTimeout = 0.0f;
        private ulong _lastReceivedVideoSourceFrame = 0;
        private ulong _lastRenderedFrame = 0;

        public UnityEvent _connectEvent = new UnityEvent();
        public UnityEvent ConnectEvent
        {
            get { return _connectEvent; } 
        }

        public UnityEvent _disconnectEvent = new UnityEvent();
        public UnityEvent DisconnectEvent
        {
            get { return _disconnectEvent; }
        }

        private Dictionary<MikanSpatialAnchorID, MikanPoseUpdateEvent> _anchorPoseEvents = new Dictionary<MikanSpatialAnchorID, MikanPoseUpdateEvent>();

        public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        public static MikanClient Instance
        {
            get
            {
                return _instance;
            }
        }

        [Tooltip("Camera prefab for customized rendering.")]
        [SerializeField] Camera _MRCamera = null;
        /// <summary>
        /// Camera prefab for customized rendering.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Camera MRCamera
        {
            get
            {
                return _MRCamera;
            }
            set
            {
                _MRCamera = value;
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        void OnEnable()
        {
            _enabled = true;
            _apiInitialized = false;

            MikanClientGraphicsAPI graphicsAPI = MikanClientGraphicsAPI.UNKNOWN;
            switch(SystemInfo.graphicsDeviceType)
            {
                case GraphicsDeviceType.Direct3D11:
                    graphicsAPI = MikanClientGraphicsAPI.Direct3D11;
                    break;
                case GraphicsDeviceType.OpenGLCore:
                case GraphicsDeviceType.OpenGLES2:
                case GraphicsDeviceType.OpenGLES3:
                    graphicsAPI = MikanClientGraphicsAPI.OpenGL;
                    break;
            }

            _clientInfo = new MikanClientInfo()
            {
                supportedFeatures = MikanClientFeatures.RenderTarget_RGBA32,
                engineName = "unity",
                engineVersion = Application.unityVersion,
                applicationName = Application.productName,
                applicationVersion = Application.version,
#if UNITY_2017_2_OR_NEWER
                xrDeviceName = XRSettings.loadedDeviceName,
#endif
                graphicsAPI = graphicsAPI,
                mikanSdkVersion = SDKConstants.SDK_VERSION,
            };

            MikanResult result= MikanClientAPI.Mikan_Initialize(MikanLogLevel.Info, "UnityClient.log");
            if (result == MikanResult.Success)
            {
                _apiInitialized = true;
            }
        }

        void OnDisable()
        {
            _enabled = false;

            if (_apiInitialized)
            {
                if (!_readbackRequest.done)
                {
                    _readbackRequest.WaitForCompletion();
                }

                freeFrameBuffer();
                MikanClientAPI.Mikan_Shutdown();
                _apiInitialized = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (MikanClientAPI.Mikan_GetIsConnected())
            {
                MikanEvent mikanEvent;
                while (MikanClientAPI.Mikan_PollNextEvent(out mikanEvent) == MikanResult.Success)
                {
                    switch(mikanEvent.event_type)
                    {
                    case MikanEventType.connected:
                        reallocateRenderBuffers();
                        //setupStencils();
                        updateCameraProjectionMatrix();
                        _connectEvent.Invoke();
                        break;
                    case MikanEventType.disconnected:
                        _disconnectEvent.Invoke();
                        break;
                    case MikanEventType.videoSourceOpened:
                        reallocateRenderBuffers();
                        updateCameraProjectionMatrix();
                        break;
                    case MikanEventType.videoSourceClosed:
                        break;
                    case MikanEventType.videoSourceNewFrame:
                        processNewVideoSourceFrame(mikanEvent.event_payload.video_source_new_frame);
    					break;
				    case MikanEventType.videoSourceModeChanged:
				    case MikanEventType.videoSourceIntrinsicsChanged:
					   reallocateRenderBuffers();
					   updateCameraProjectionMatrix();
					   break;
				    case MikanEventType.videoSourceAttachmentChanged:
					   break;
				    case MikanEventType.vrDevicePoseUpdated:
					   break;
				    case MikanEventType.anchorPoseUpdated:
                       updateAnchorPose(mikanEvent.event_payload.anchor_pose_updated);
					   break;
				    case MikanEventType.anchorListUpdated:
					   break;
                    }
                }
            }
            else
            {
                if (_mikanReconnectTimeout <= 0.0f)
                {
                    if (MikanClientAPI.Mikan_Connect(_clientInfo) == MikanResult.Success)
                    {
                        _lastReceivedVideoSourceFrame = 0;
                    }
                    else
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

        void setupStencils()
        {
            // Skip if stencils are already created
            MikanStencilList stencilList;
            MikanClientAPI.Mikan_GetStencilList(out stencilList);
            if (stencilList.stencil_count > 0)
                return;

            // Get the origin spatial anchor to build the stencil scene around
            MikanSpatialAnchorInfo originSpatialAnchor;
            if (MikanClientAPI.Mikan_FindSpatialAnchorInfoByName("origin", out originSpatialAnchor) == MikanResult.Success)
            {
                _originSpatialAnchorXform = MikanMath.MikanMatrix4fToMatrix4x4(originSpatialAnchor.anchor_xform);
            }
            else
            {
                _originSpatialAnchorXform = Matrix4x4.identity;
            }

            // Create a stencil in front of the origin
            {
                Vector4 col0 = _originSpatialAnchorXform.GetColumn(0);
                Vector4 col1 = _originSpatialAnchorXform.GetColumn(1);
                Vector4 col2 = _originSpatialAnchorXform.GetColumn(2);
                Vector4 col3 = _originSpatialAnchorXform.GetColumn(3);

                Vector3 quad_x_axis = new Vector3(col0.x, col0.y, col0.z);
                Vector3 quad_y_axis = new Vector3(col1.x, col1.y, col1.z);
                Vector3 quad_normal = new Vector3(col2.x, col2.y, col2.z);
                Vector3 quad_center = new Vector3(col3.x, col3.y, col3.z) + quad_normal * 0.4f + quad_y_axis * 0.3f;

                _stencilQuad = new MikanStencilQuad();
                _stencilQuad.stencil_id = SDKConstants.INVALID_MIKAN_ID; // filled in on allocation
                _stencilQuad.quad_center = MikanMath.Vector3ToMikanVector3f(quad_center);
                _stencilQuad.quad_x_axis = MikanMath.Vector3ToMikanVector3f(quad_x_axis);
                _stencilQuad.quad_y_axis = MikanMath.Vector3ToMikanVector3f(quad_y_axis);
                _stencilQuad.quad_normal = MikanMath.Vector3ToMikanVector3f(quad_normal);
                _stencilQuad.quad_width = 0.25f;
                _stencilQuad.quad_height = 0.25f;
                _stencilQuad.is_double_sided = true;
                _stencilQuad.is_disabled = false;
                MikanClientAPI.Mikan_AllocateQuadStencil(ref _stencilQuad);
            }
        }

        void processNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
	    {
		    if (newFrameEvent.frame == _lastReceivedVideoSourceFrame)
		    	return;

            // Apply the camera pose received
            setCameraPose(
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraForward),
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraUp),
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraPosition));

            // Render out a new frame
            render(newFrameEvent.frame);

            // Remember the frame index of the last frame we published
            _lastReceivedVideoSourceFrame = newFrameEvent.frame;
        }

        void setCameraPose(Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraPosition)
        {
            if (_MRCamera == null)
                return;

            // Decompose Matrix4x4 into a quaternion and an position
            _MRCamera.transform.localRotation = Quaternion.LookRotation(cameraForward, cameraUp);
            _MRCamera.transform.localPosition = cameraPosition;
        }

        void reallocateRenderBuffers()
        {
            freeFrameBuffer();

            MikanClientAPI.Mikan_FreeRenderTargetBuffers();
            _renderTargetMemory = new MikanRenderTargetMemory();

            MikanVideoSourceMode mode;
            if (MikanClientAPI.Mikan_GetVideoSourceMode(out mode) == MikanResult.Success)
            {
                MikanRenderTargetDescriptor desc;
                desc.width = (uint)mode.resolution_x;
                desc.height = (uint)mode.resolution_y;
                desc.color_key = new MikanColorRGB() { 
                    r= BackgroundColorKey.r, 
                    g= BackgroundColorKey.g, 
                    b= BackgroundColorKey.b
                };
                desc.color_buffer_type = MikanColorBufferType.RGBA32;
                desc.depth_buffer_type = MikanDepthBufferType.NONE;
                desc.graphicsAPI = _clientInfo.graphicsAPI;

                MikanClientAPI.Mikan_AllocateRenderTargetBuffers(desc, out _renderTargetMemory);
                createFrameBuffer(_renderTargetMemory, mode.resolution_x, mode.resolution_y);
            }
        }

        bool createFrameBuffer(MikanRenderTargetMemory renderTargetMemory, int width, int height)
        {
            bool bSuccess = true;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("Mikan: Unable to create render texture. Texture dimension must be higher than zero.");
                return false;
            }

            //_externalTexture = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, renderTargetMemory.color_texture_pointer);

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
                Debug.LogError("LIV: Unable to create render texture.");
                return false;
            }


            return bSuccess;
        }

        void freeFrameBuffer()
        {
            if (_renderTexture != null && _renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }
            _renderTexture = null;
            //_externalTexture = null;
        }

        void updateCameraProjectionMatrix()
        {
            MikanVideoSourceIntrinsics videoSourceIntrinsics;
            if (MikanClientAPI.Mikan_GetVideoSourceIntrinsics(out videoSourceIntrinsics) == MikanResult.Success)
            {
                MikanMonoIntrinsics monoIntrinsics = videoSourceIntrinsics.intrinsics.mono;
                float videoSourcePixelWidth = (float)monoIntrinsics.pixel_width;
                float videoSourcePixelHeight = (float)monoIntrinsics.pixel_height;

                MRCamera.fieldOfView = (float)monoIntrinsics.vfov;
                MRCamera.aspect = videoSourcePixelWidth / videoSourcePixelHeight;
                MRCamera.nearClipPlane = (float)monoIntrinsics.znear;
                MRCamera.farClipPlane = (float)monoIntrinsics.zfar;
            }
        }

        void updateAnchorPose(MikanAnchorPoseUpdateEvent anchorPoseEvent)
        {
            MikanPoseUpdateEvent anchorEvent;

            if (_anchorPoseEvents.TryGetValue(anchorPoseEvent.anchor_id, out anchorEvent))
            {
                anchorEvent.Invoke(anchorPoseEvent.transform);
            }
        }

        public void addAnchorPoseListener(MikanSpatialAnchorID anchor_id, UnityAction<MikanMatrix4f> call)
        {
            MikanPoseUpdateEvent anchorEvent;

            if (!_anchorPoseEvents.TryGetValue(anchor_id, out anchorEvent))
            {
                anchorEvent = new MikanPoseUpdateEvent();
                _anchorPoseEvents.Add(anchor_id, anchorEvent);
            }

            anchorEvent.AddListener(call);
        }

        public void removeAnchorPoseListener(MikanSpatialAnchorID anchor_id, UnityAction<MikanMatrix4f> call)
        {
            MikanPoseUpdateEvent anchorEvent;

            if (_anchorPoseEvents.TryGetValue(anchor_id, out anchorEvent))
            {
                anchorEvent.RemoveListener(call);

                if (anchorEvent.GetPersistentEventCount() == 0)
                {
                    _anchorPoseEvents.Remove(anchor_id);
                }
            }
        }

        void render(ulong frame_index)
        {
            _lastRenderedFrame = frame_index;
            _MRCamera.targetTexture = _renderTexture;
            _MRCamera.Render();
            _MRCamera.targetTexture = null;

            if (_clientInfo.graphicsAPI == MikanClientGraphicsAPI.Direct3D11 ||
                _clientInfo.graphicsAPI == MikanClientGraphicsAPI.OpenGL)
            {
                IntPtr textureNativePtr = _renderTexture.GetNativeTexturePtr();

                // Fast interprocess shared texture transfer
                MikanClientAPI.Mikan_PublishRenderTargetTexture(textureNativePtr, frame_index);
            }
            if (_clientInfo.graphicsAPI == MikanClientGraphicsAPI.UNKNOWN)
            {
                if (_renderTargetMemory.color_buffer != IntPtr.Zero)
                {
                    // Slow texture read-back / shared CPU memory transfer
                    _readbackRequest = AsyncGPUReadback.Request(_renderTexture, 0, ReadbackCompleted);
                }
            }
        }

        void ReadbackCompleted(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                NativeArray<byte> buffer = request.GetData<byte>();

                if (buffer.Length > 0 &&
                    _renderTargetMemory.color_buffer != IntPtr.Zero &&
                    _renderTargetMemory.color_buffer_size.ToUInt32() == buffer.Length)
                {
                    unsafe
                    {
                        void* dest = _renderTargetMemory.color_buffer.ToPointer();
                        void* source = NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
                        long size = buffer.Length;

                        UnsafeUtility.MemCpy(dest, source, size);
                    }

                    // Publish the new video frame back to Mikan
                    MikanClientAPI.Mikan_PublishRenderTargetBuffers(_lastRenderedFrame);
                }
            }
        }
    }
}