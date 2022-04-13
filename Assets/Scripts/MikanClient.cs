using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Runtime.InteropServices;
using System.IO;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace MikanXR.SDK.Unity
{
    [HelpURL("https://github.com/MikanXR/MikanXR_Unity")]
    [AddComponentMenu("MikanXR/Mikan")]
    public class MikanClient : MonoBehaviour
    {
        private MikanClientInfo _clientInfo;
        private MikanRenderTargetMemory _renderTargetMemory;
        private MikanStencilQuad _stencilQuad;
        private Matrix4x4 _originSpatialAnchorXform = Matrix4x4.identity;
        private RenderTexture _renderTexture;
        private AsyncGPUReadbackRequest _readbackRequest = new AsyncGPUReadbackRequest();

        private bool _enabled = false;
        private bool _apiInitialized = false;
        private float _mikanReconnectTimeout = 0.0f;
        private ulong _lastReceivedVideoSourceFrame = 0;
        private ulong _lastRenderedFrame = 0;

        public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);

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

        void OnEnable()
        {
            _enabled = true;
            _apiInitialized = false;
            _clientInfo = new MikanClientInfo()
            {
                supportedFeatures = MikanClientFeatures.RenderTarget_ARGB32,
                engineName = "unity",
                engineVersion = Application.unityVersion,
                applicationName = Application.productName,
                applicationVersion = Application.version,
#if UNITY_2017_2_OR_NEWER
                xrDeviceName = XRSettings.loadedDeviceName,
#endif
                graphicsAPI = SystemInfo.graphicsDeviceType.ToString(),
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
                MikanClientAPI.Mikan_FreeRenderTargetBuffers();

                MikanClientAPI.Mikan_Disconnect();

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
                        break;
                    case MikanEventType.disconnected:
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
            setCameraPose(MikanMath.MikanMatrix4fToMatrix4x4(newFrameEvent.transform));

            // Render out a new frame
            render(newFrameEvent.frame);

            // Remember the frame index of the last frame we published
            _lastReceivedVideoSourceFrame = newFrameEvent.frame;
        }

        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }

        void setCameraPose(Matrix4x4 mat)
        {
            if (_MRCamera == null)
                return;

            if (!mat.ValidTRS())
                return;

            // Decompose Matrix4x4 into a quaternion and an position
            _MRCamera.transform.localRotation = Quaternion.LookRotation(mat.GetColumn(2), mat.GetColumn(1));
            //_MRCamera.transform.localRotation = QuaternionFromMatrix(mat);
            //float w = Mathf.Sqrt(1.0f + mat.m00 + mat.m11 + mat.m22) / 2.0f;
            //_MRCamera.transform.localRotation =new Quaternion(
            //        (mat.m21 - mat.m12) / (4.0f * w),
            //        (mat.m02 - mat.m20) / (4.0f * w),
            //        (mat.m10 - mat.m01) / (4.0f * w),
            //        w);
            _MRCamera.transform.localPosition = mat.GetColumn(3);
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
                desc.color_buffer_type = MikanColorBufferType.ARGB32;
                desc.depth_buffer_type = MikanDepthBufferType.NONE;

                MikanClientAPI.Mikan_AllocateRenderTargetBuffers(desc, out _renderTargetMemory);
                createFrameBuffer(mode.resolution_x, mode.resolution_y);
            }
        }

        bool createFrameBuffer(int width, int height)
        {
            bool bSuccess = true;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError("Mikan: Unable to create render texture. Texture dimension must be higher than zero.");
                return false;
            }

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
            if (_renderTexture == null) return;
            if (_renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }
            _renderTexture = null;
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

        void render(ulong frame_index)
        {
            _lastRenderedFrame = frame_index;
            _MRCamera.targetTexture = _renderTexture;
            _MRCamera.Render();
            _MRCamera.targetTexture = null;

            if (_renderTargetMemory.color_buffer != IntPtr.Zero)
            {
                _readbackRequest= AsyncGPUReadback.Request(_renderTexture, 0, ReadbackCompleted);
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