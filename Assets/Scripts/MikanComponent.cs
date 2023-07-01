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

namespace Mikan
{
    using MikanSpatialAnchorID = System.Int32;

    [System.Serializable]
    public class MikanPoseUpdateEvent : UnityEvent<MikanTransform> { }

    [HelpURL("https://github.com/MikanXR/MikanXR_Unity")]
    [AddComponentMenu("MikanXR/Mikan")]
    public class MikanComponent : MonoBehaviour
    {
        private static MikanComponent _instance = null;

        private MikanClientInfo _clientInfo;
        private MikanRenderTargetMemory _renderTargetMemory;
        private MikanStencilQuad _stencilQuad;
        private Transform _originSpatialAnchorXform;
        private RenderTexture _renderTexture;

        //private Texture2D _externalTexture;
        private AsyncGPUReadbackRequest _readbackRequest = new AsyncGPUReadbackRequest();

        private bool _enabled = false;
        private bool _apiInitialized = false;
        private float _mikanReconnectTimeout = 0.0f;
        private ulong _lastReceivedVideoSourceFrame = 0;
        private ulong _lastRenderedFrame = 0;
        private Quaternion _lastCameraRotation = new Quaternion();
        private Vector3 _lastCameraPosition= new Vector3();
        private float _sceneScale= 1.0f;

        public UnityEvent OnConnectEvent;
        public UnityEvent OnDisconnectEvent;
        public event Action<string> OnMessageEvent;

        private Dictionary<MikanSpatialAnchorID, MikanPoseUpdateEvent> _anchorPoseEvents =
            new Dictionary<MikanSpatialAnchorID, MikanPoseUpdateEvent>();

        public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        public static MikanComponent Instance
        {
            get { return _instance; }
        }

        [Tooltip("Camera prefab for customized rendering.")]
        [SerializeField]
        Camera _MRCamera = null;

        /// <summary>
        /// Camera prefab for customized rendering.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public Camera MRCamera
        {
            get { return _MRCamera; }
            set { _MRCamera = value; }
        }

        private void Awake()
        {
            _instance = this;
        }

        void OnEnable()
        {
            if (!_enabled)
            {
                _enabled = true;
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

                MikanResult result = MikanClient.Mikan_Initialize(MikanLogLevel.Info, OnMikanLog);
                if (result == MikanResult.Success)
                {
                    _apiInitialized = true;
                }
            }
        }

        void OnMikanLog(int log_level, string log_message)
        {
            MikanLogLevel mikanLogLevel = (MikanLogLevel)log_level;
            switch (mikanLogLevel)
            {
                case MikanLogLevel.Trace:
                case MikanLogLevel.Debug:
                case MikanLogLevel.Info:
                    Debug.Log(log_message);
                    break;
                case MikanLogLevel.Warning:
                    Debug.LogWarning(log_message);
                    break;
                case MikanLogLevel.Error:
                    Debug.LogError(log_message);
                    break;
                case MikanLogLevel.Fatal:
                    Debug.LogAssertion(log_message);
                    break;
            }
        }

        void OnDisable()
        {
            if (_enabled)
            {
                if (_apiInitialized)
                {
                    if (!_readbackRequest.done)
                    {
                        _readbackRequest.WaitForCompletion();
                    }

                    freeFrameBuffer();
                    MikanClient.Mikan_Shutdown();
                    _apiInitialized = false;
                }

                _enabled = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (MikanClient.Mikan_GetIsConnected())
            {
                MikanEvent mikanEvent = new MikanEvent();
                while (MikanClient.Mikan_PollNextEvent(mikanEvent) == MikanResult.Success)
                {
                    switch (mikanEvent.event_type)
                    {
                        case MikanEventType.connected:
                            reallocateRenderBuffers();
                            //setupStencils();
                            updateCameraProjectionMatrix();
                            OnConnectEvent.Invoke();
                            break;
                        case MikanEventType.disconnected:
                            OnDisconnectEvent.Invoke();
                            break;
                        case MikanEventType.videoSourceOpened:
                            reallocateRenderBuffers();
                            updateCameraProjectionMatrix();
                            break;
                        case MikanEventType.videoSourceClosed:
                            break;
                        case MikanEventType.videoSourceNewFrame:
                            processNewVideoSourceFrame(
                                mikanEvent.event_payload.video_source_new_frame
                            );
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
                        case MikanEventType.scriptMessagePosted:
                            processScriptMessage(mikanEvent.event_payload.script_message_posted);
                            break;
                    }
                }
            }
            else
            {
                if (_mikanReconnectTimeout <= 0.0f)
                {
                    if (MikanClient.Mikan_Connect(_clientInfo) == MikanResult.Success)
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

        void setupOriginSpatialAnchor()
        {
            // Skip if stencils are already created
            MikanStencilList stencilList = new MikanStencilList();
            MikanClient.Mikan_GetStencilList(stencilList);
            if (stencilList.stencil_count > 0)
                return;

            if (_originSpatialAnchorXform != null)
            {
                // Get the origin spatial anchor to build the stencil scene around
                MikanSpatialAnchorInfo originSpatialAnchor = new MikanSpatialAnchorInfo();
                if (
                    MikanClient.Mikan_FindSpatialAnchorInfoByName("origin", originSpatialAnchor)
                    == MikanResult.Success
                )
                {
                    _originSpatialAnchorXform.localPosition = MikanMath.MikanVector3fToVector3(
                        originSpatialAnchor.world_transform.position
                    );
                    _originSpatialAnchorXform.localRotation = MikanMath.MikanQuatfToQuaternion(
                        originSpatialAnchor.world_transform.rotation
                    );
                    _originSpatialAnchorXform.localScale = MikanMath.MikanVector3fToVector3(
                        originSpatialAnchor.world_transform.scale
                    );
                }
            }
        }

        void processScriptMessage(MikanScriptMessageInfo scriptMessage)
        {
            OnMessageEvent?.Invoke(scriptMessage.content);
        }

        void processNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
        {
            if (newFrameEvent.frame == _lastReceivedVideoSourceFrame)
                return;

            // Apply the camera pose received
            setCameraPose(
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraForward),
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraUp),
                MikanMath.MikanVector3fToVector3(newFrameEvent.cameraPosition)
            );

            // Render out a new frame
            render(newFrameEvent.frame);

            // Remember the frame index of the last frame we published
            _lastReceivedVideoSourceFrame = newFrameEvent.frame;
        }

        public float getSceneScale()
        {
            return _sceneScale;
        }

        public void setSceneScale(float newScale)
        {
            _sceneScale= newScale;
            updateCameraTransform();
        }

        void setCameraPose(Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraPosition)
        {
            _lastCameraPosition= cameraPosition;
            _lastCameraRotation= Quaternion.LookRotation(cameraForward, cameraUp);
            updateCameraTransform();
        }

        void updateCameraTransform()
        {
            if (_MRCamera != null)
            {
                _MRCamera.transform.localRotation = _lastCameraRotation;
                _MRCamera.transform.localPosition = _lastCameraPosition * _sceneScale;            
            }
        }

        void reallocateRenderBuffers()
        {
            freeFrameBuffer();

            MikanClient.Mikan_FreeRenderTargetBuffers();
            _renderTargetMemory = new MikanRenderTargetMemory();

            MikanVideoSourceMode mode = new MikanVideoSourceMode();
            if (MikanClient.Mikan_GetVideoSourceMode(mode) == MikanResult.Success)
            {
                MikanRenderTargetDescriptor desc = new MikanRenderTargetDescriptor();
                desc.width = (uint)mode.resolution_x;
                desc.height = (uint)mode.resolution_y;
                desc.color_key = new MikanColorRGB()
                {
                    r = BackgroundColorKey.r,
                    g = BackgroundColorKey.g,
                    b = BackgroundColorKey.b
                };
                desc.color_buffer_type = MikanColorBufferType.RGBA32;
                desc.depth_buffer_type = MikanDepthBufferType.NODEPTH;
                desc.graphicsAPI = _clientInfo.graphicsAPI;

                MikanClient.Mikan_AllocateRenderTargetBuffers(desc, _renderTargetMemory);
                createFrameBuffer(_renderTargetMemory, mode.resolution_x, mode.resolution_y);
            }
        }

        bool createFrameBuffer(MikanRenderTargetMemory renderTargetMemory, int width, int height)
        {
            bool bSuccess = true;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError(
                    "Mikan: Unable to create render texture. Texture dimension must be higher than zero."
                );
                return false;
            }

            //_externalTexture = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, renderTargetMemory.color_texture_pointer);

            int depthBufferPrecision = 0;
            _renderTexture = new RenderTexture(
                width,
                height,
                depthBufferPrecision,
                RenderTextureFormat.ARGB32
            )
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
            MikanVideoSourceIntrinsics videoSourceIntrinsics = new MikanVideoSourceIntrinsics();
            if (
                MikanClient.Mikan_GetVideoSourceIntrinsics(videoSourceIntrinsics)
                == MikanResult.Success
            )
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

        public void addAnchorPoseListener(
            MikanSpatialAnchorID anchor_id,
            UnityAction<MikanTransform> call
        )
        {
            MikanPoseUpdateEvent anchorEvent;

            if (!_anchorPoseEvents.TryGetValue(anchor_id, out anchorEvent))
            {
                anchorEvent = new MikanPoseUpdateEvent();
                _anchorPoseEvents.Add(anchor_id, anchorEvent);
            }

            anchorEvent.AddListener(call);
        }

        public void removeAnchorPoseListener(
            MikanSpatialAnchorID anchor_id,
            UnityAction<MikanTransform> call
        )
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

            if (
                _clientInfo.graphicsAPI == MikanClientGraphicsApi.Direct3D11
                || _clientInfo.graphicsAPI == MikanClientGraphicsApi.OpenGL
            )
            {
                IntPtr textureNativePtr = _renderTexture.GetNativeTexturePtr();

                // Fast interprocess shared texture transfer
                MikanClient.Mikan_PublishRenderTargetTexture(textureNativePtr, frame_index);
            }
            if (_clientInfo.graphicsAPI == MikanClientGraphicsApi.UNKNOWN)
            {
                if (_renderTargetMemory.color_buffer != IntPtr.Zero)
                {
                    // Slow texture read-back / shared CPU memory transfer
                    _readbackRequest = AsyncGPUReadback.Request(
                        _renderTexture,
                        0,
                        ReadbackCompleted
                    );
                }
            }
        }

        void ReadbackCompleted(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                NativeArray<byte> buffer = request.GetData<byte>();

                if (
                    buffer.Length > 0
                    && _renderTargetMemory.color_buffer != IntPtr.Zero
                    && _renderTargetMemory.color_buffer_size == buffer.Length
                )
                {
                    unsafe
                    {
                        void* dest = _renderTargetMemory.color_buffer.ToPointer();
                        void* source = NativeArrayUnsafeUtility.GetUnsafePtr(buffer);
                        long size = buffer.Length;

                        UnsafeUtility.MemCpy(dest, source, size);
                    }

                    // Publish the new video frame back to Mikan
                    MikanClient.Mikan_PublishRenderTargetBuffers(_lastRenderedFrame);
                }
            }
        }
    }
}
