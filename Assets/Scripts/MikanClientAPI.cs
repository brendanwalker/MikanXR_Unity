using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace MikanXR.SDK.Unity
{
    /// The ID of a VR Device
    using MikanVRDeviceID = System.Int32;

    /// The ID of a stencil
    using MikanStencilID = System.Int32;

    /// The ID of a spatial anchor
    using MikanSpatialAnchorID = System.Int32;

    public static class SDKConstants
    {
        public const string SDK_VERSION = "1.0.0";
        public const int INVALID_MIKAN_ID = -1;
        public const int MAX_MIKAN_VR_DEVICES = 64;
        public const int MAX_MIKAN_STENCILS = 16;
        public const int MAX_MIKAN_SPATIAL_ANCHORS = 64;
        public const int MAX_MIKAN_ANCHOR_NAME_LEN = 128;
    }

    /// Result enum in response to a client API request
    public enum MikanResult
    {
        Success = 0,
        GeneralError = -1,
        Uninitialized = -2,
        NullParam = -2,
        InitFailed = -3,
        ConnectionFailed = -4,
        AlreadyConnected = -5,
        NoData = -6,
        NotConnected = -7,
        SharedMemoryError = -8,
        UnknownClient = -9,
        UnknownFunction = -10,
        FailedFunctionSend = -11,
        FunctionResponseTimeout = -12,
        MalformedParameters = -13,
        MalformedResponse = -14,
        NoSelectedCamera = -15,
        NoCameraAssignedTracker = -16,
        InvalidDeviceID = -17,
        InvalidStencilID = -18,
    };

    public enum MikanLogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    };

    public enum MikanVideoSourceType
    {
        MONO,
        STEREO
    };

    /// The list of possible camera APIs used by MikanXR
    public enum MikanVideoSourceApi
    {
        INVALID,
        OPENCV_CV,
        WINDOWS_MEDIA_FOUNDATION
    };

    /// The list of possible vr device drivers used by MikanXR Client API
    public enum MikanVRDeviceApi
    {
        INVALID,
        STEAM_VR,
    };

    /// The list of possible vr device types used by MikanXR Client API
    public enum MikanVRDeviceType
    {
        INVALID,
        HMD,
        CONTROLLER,
        TRACKER
    };

    public enum MikanBufferType
    {
        COLOR,
        DEPTH,
    };

    public enum MikanColorBufferType
    {
        NONE,
        RGB24,
        ARGB32,
    };

    public enum MikanDepthBufferType
    {
        NONE,
        DEPTH16,
        DEPTH32,
    };

    [System.Flags]
    public enum MikanClientFeatures : ulong
    {
        NONE = 0L,

        // Render target options
        RenderTarget_RGB24 = 1L << 0,
        RenderTarget_RGBA32 = 1L << 1,
        RenderTarget_DEPTH16 = 1L << 2,
        RenderTarget_DEPTH32 = 1L << 3,
    };

    public enum MikanEventType
    {
        // App Connection Events
        connected,
        disconnected,

        // Video Source Events
        videoSourceOpened,
        videoSourceClosed,
        videoSourceNewFrame,
        videoSourceAttachmentChanged,
        videoSourceIntrinsicsChanged,
        videoSourceModeChanged,

        // VR Device Events
        vrDevicePoseUpdated,
        vrDeviceListUpdated,

        // Spatial Anchor Events
        anchorPoseUpdated,
        anchorListUpdated
    };

    public enum MikanIntrinsicsType
    {
        INVALID,
        MONO,
        STEREO,
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVector3d
    {
        public double x;
        public double y;
        public double z;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVector3f
    {
        public float x;
        public float y;
        public float z;
    };

    /** A 4x4 matrix with float components
        storage is column major order:

        | x0 y0 z0 w0 |
        | x1 y1 z1 w1 |
        | x2 y2 z2 w2 |
        | x3 y3 z3 w3 |
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanMatrix4f
    {
        public float x0;
        public float x1;
        public float x2;
        public float x3;
        public float y0;
        public float y1;
        public float y2;
        public float y3;
        public float z0;
        public float z1;
        public float z2;
        public float z3;
        public float w0;
        public float w1;
        public float w2;
        public float w3;
    };

    /** A 3x3 matrix with double components
        storage is column major order:

        | x0 y0 z0 |
        | x1 y1 z1 |
        | x2 y2 z2 |
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanMatrix3d
    {
        public double x0;
        public double x1;
        public double x2;
        public double y0;
        public double y1;
        public double y2;
        public double z0;
        public double z1;
        public double z2;
    };

    /** A 4x3 matrix with double components
        storage is column major order:

        | x0 y0 z0 |
        | x1 y1 z1 |
        | x2 y2 z2 |
        | x3 y3 z3 |
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanMatrix4x3d
    {
        public double x0;
        public double x1;
        public double x2;
        public double x3;
        public double y0;
        public double y1;
        public double y2;
        public double y3;
        public double z0;
        public double z1;
        public double z2;
        public double z3;
    };

    /** A 4x4 matrix with double components
        storage is column major order:

        | x0 y0 z0 w0 |
        | x1 y1 z1 w1 |
        | x2 y2 z2 w2 |
        | x3 y3 z3 w3 |
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanMatrix4d
    {
        public double x0;
        public double x1;
        public double x2;
        public double x3;
        public double y0;
        public double y1;
        public double y2;
        public double y3;
        public double z0;
        public double z1;
        public double z2;
        public double z3;
        public double w0;
        public double w1;
        public double w2;
        public double w3;
    };

    // A quaternion rotation.
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanQuatd
    {
        public double w;
        public double x;
        public double y;
        public double z;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MikanClientInfo
    {
        public MikanClientFeatures supportedFeatures;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string engineName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string engineVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string applicationName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string applicationVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string xrDeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string graphicsAPI;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string mikanSdkVersion;
    };

    // A float RGB color with [0,1] components.
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanColorRGB
    {
        public float r;
        public float g;
        public float b;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanRenderTargetDescriptor
    {
        public MikanColorBufferType color_buffer_type;
        public MikanColorRGB color_key;
        public MikanDepthBufferType depth_buffer_type;
        public uint width;
        public uint height;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanRenderTargetMemory
    {
        public IntPtr color_buffer;
        public IntPtr depth_buffer;
        public UIntPtr color_buffer_size;
        public UIntPtr depth_buffer_size;
        public uint width;
        public uint height;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanStencilQuad
    {
        public MikanStencilID stencil_id; // filled in on allocation
        public MikanVector3f quad_center;
        public MikanVector3f quad_x_axis;
        public MikanVector3f quad_y_axis;
        public MikanVector3f quad_normal;
        public float quad_width;
        public float quad_height;
        [MarshalAs(UnmanagedType.I1)]
        public bool is_double_sided;
        [MarshalAs(UnmanagedType.I1)]
        public bool is_disabled;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanStencilList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SDKConstants.MAX_MIKAN_STENCILS)]
        public MikanStencilID[] stencil_id_list;
        public int stencil_count;
    };

    /// Radial and tangential lens distortion coefficients computed during lens lens calibration
    /// See the [OpenCV Docs](http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html) for details
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanDistortionCoefficients
    {
        public double k1; ///< Radial Distortion Parameter 1 (r^2 numerator constant)
        public double k2; ///< Radial Distortion Parameter 2 (r^4 numerator constant)
        public double k3; ///< Radial Distortion Parameter 3 (r^6 numerator constant)
        public double k4; ///< Radial Distortion Parameter 4 (r^2 divisor constant)
        public double k5; ///< Radial Distortion Parameter 5 (r^4 divisor constant)
        public double k6; ///< Radial Distortion Parameter 6 (r^6 divisor constant)
        public double p1; ///< Tangential Distortion Parameter 1
        public double p2; ///< Tangential Distortion Parameter 2
    };

    /// Camera intrinsic properties for a monoscopic camera
    /// See the [OpenCV Docs](http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html) for details
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanMonoIntrinsics
    {
        public double pixel_width;  ///< Width of the camera buffer in pixels
        public double pixel_height; ///< Height of the camera buffer in pixels
        public double hfov;         ///< The horizontal field of view camera in degrees
        public double vfov;         ///< The vertical field of view camera in degrees
        public double znear;        ///< The distance of the near clipping plane in cm
        public double zfar;         ///< The distance of the far clipping plane in cm
        public MikanDistortionCoefficients distortion_coefficients;   ///< Lens distortion coefficients
        public MikanMatrix3d camera_matrix;   ///< Intrinsic camera matrix containing focal lengths and principal point
    };

    /// Camera intrinsic properties for a stereoscopic camera
    /// See the [OpenCV Docs](http://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html) for details
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanStereoIntrinsics
    {
        // Keep these in sync with PSVRMonoTrackerIntrinsics
        public double pixel_width;  ///< Width of the camera buffer in pixels
        public double pixel_height; ///< Height of the camera buffer in pixels
        public double hfov;         ///< The horizontal field of view camera in degrees
        public double vfov;         ///< The vertical field of view camera in degrees
        public double znear;        ///< The distance of the near clipping plane in cm
        public double zfar;         ///< The distance of the far clipping plane in cm
        public MikanDistortionCoefficients left_distortion_coefficients; ///< Left lens distortion coefficients
        public MikanMatrix3d left_camera_matrix; ///< Intrinsic matrix for left camera containing focal lengths and principal point
        public MikanDistortionCoefficients right_distortion_coefficients; ///< Right lens distortion coefficients
        public MikanMatrix3d right_camera_matrix; ///< Intrinsic matrix for rotation camera containing focal lengths and principal point
        public MikanMatrix3d left_rectification_rotation; ///< Rotation applied to left camera to rectify the image
        public MikanMatrix3d right_rectification_rotation; ///< Rotation applied to right camera to rectify the image
        public MikanMatrix4x3d left_rectification_projection; ///< Projection applied to left camera to rectify the image
        public MikanMatrix4x3d right_rectification_projection; ///< Projection applied to right camera to rectify the image
        public MikanMatrix3d rotation_between_cameras; ///< Rotation between the left and right cameras
        public MikanVector3d translation_between_cameras; ///< Translation between the left and right camera
        public MikanMatrix3d essential_matrix; ///< Transform relating points in unit coordinate space between cameras
        public MikanMatrix3d fundamental_matrix; ///< Transform relating points in pixel coordinates between cameras
        public MikanMatrix4d reprojection_matrix;  ///< Transform relating pixel x,y + disparity to distance from cameras
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct MikanVideoSourceIntrinsicsPayload
    {
        [FieldOffset(0)] public MikanMonoIntrinsics mono;
        [FieldOffset(0)] public MikanStereoIntrinsics stereo;
    };

    // Bundle containing all intrinsic video source properties
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVideoSourceIntrinsics
    {
        public MikanVideoSourceIntrinsicsPayload intrinsics;
        public MikanIntrinsicsType intrinsics_type;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVideoSourceAttachmentInfo
    {
        public MikanVRDeviceID attached_vr_device_id;
        public MikanMatrix4f vr_device_offset_xform;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MikanVideoSourceMode
    {
        public MikanVideoSourceType video_source_type;
        public MikanVideoSourceApi video_source_api;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string device_path;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string video_mode_name;
        public int resolution_x;
        public int resolution_y;
        public float frame_rate;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVRDeviceList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SDKConstants.MAX_MIKAN_VR_DEVICES)]
        public MikanVRDeviceID[] vr_device_id_list;
        public int vr_device_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MikanVRDeviceInfo
    {

        public MikanVRDeviceApi vr_device_api;
        public MikanVRDeviceType vr_device_type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string device_path;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanSpatialAnchorList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SDKConstants.MAX_MIKAN_SPATIAL_ANCHORS)]
        public MikanSpatialAnchorID[] spatial_anchor_id_list;
        public int spatial_anchor_count;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct MikanSpatialAnchorInfo
    {
        public MikanSpatialAnchorID anchor_id;
        public MikanMatrix4f anchor_xform;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SDKConstants.MAX_MIKAN_ANCHOR_NAME_LEN)]
        public string anchor_name;
    };

    // Message Container
    //------------------
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVideoSourceNewFrameEvent
    {
        public MikanVector3f cameraForward;
        public MikanVector3f cameraUp;
        public MikanVector3f cameraPosition;
        public ulong frame;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanVRDevicePoseUpdateEvent
    {
        public MikanMatrix4f transform;
        public MikanVRDeviceID device_id;
        public ulong frame;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MikanAnchorPoseUpdateEvent
    {
        public MikanMatrix4f transform;
        public MikanSpatialAnchorID anchor_id;
        public ulong frame;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct MikanEventPayload
    {
        [FieldOffset(0)] 
        public MikanVideoSourceNewFrameEvent video_source_new_frame;
        [FieldOffset(0)] 
        public MikanVRDevicePoseUpdateEvent vr_device_pose_updated;
        [FieldOffset(0)] 
        public MikanAnchorPoseUpdateEvent anchor_pose_updated;
    };

    // A container for all MikanXR Client API events    
    [StructLayout(LayoutKind.Sequential)]
    public struct MikanEvent
    {
        public MikanEventPayload event_payload;
        public MikanEventType event_type;
    };

    public class MikanClientAPI
    {

//#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && UNITY_64
        [DllImport("Mikan_CAPI", CharSet = CharSet.Ansi)]
        public static extern MikanResult Mikan_Initialize(
            MikanLogLevel log_level,
            [MarshalAs(UnmanagedType.LPStr)]
        string log_filename);

        [DllImport("Mikan_CAPI")]
        public static extern bool Mikan_GetIsInitialized();

        [DllImport("Mikan_CAPI")]
        public static extern IntPtr Mikan_GetVersionString();
        public static string GetVersionString()
        {
            IntPtr result = Mikan_GetVersionString();
            return Marshal.PtrToStringAnsi(result);
        }

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_Connect([In] MikanClientInfo client_info);

        [DllImport("Mikan_CAPI")]
        public static extern bool Mikan_GetIsConnected();

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_PollNextEvent([Out] out MikanEvent out_event);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetVideoSourceIntrinsics([Out] out MikanVideoSourceIntrinsics out_intrinsics);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetVideoSourceMode([Out] out MikanVideoSourceMode out_info);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetVideoSourceAttachment([Out] out MikanVideoSourceAttachmentInfo out_info);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetVRDeviceList([Out] out MikanVRDeviceList out_vr_device_list);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetVRDeviceInfo(MikanVRDeviceID device_id, [Out] out MikanVRDeviceInfo out_vr_device_info);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_SubscribeToVRDevicePoseUpdates(MikanVRDeviceID device_id);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_UnsubscribeFromVRDevicePoseUpdates(MikanVRDeviceID device_id);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_AllocateRenderTargetBuffers(
            MikanRenderTargetDescriptor descriptor,
            [Out] out MikanRenderTargetMemory out_memory);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_PublishRenderTargetBuffers(ulong frame_index);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_FreeRenderTargetBuffers();

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_AllocateQuadStencil([In, Out] ref MikanStencilQuad stencil);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetStencilList([Out] out MikanStencilList out_stencil_list);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetQuadStencil(MikanStencilID device_id, [Out] out MikanStencilQuad out_stencil);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_UpdateQuadStencil(MikanStencilQuad stencil);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_FreeQuadStencil(MikanStencilID stencil_id);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetSpatialAnchorList([Out] out MikanSpatialAnchorList out_anchor_list);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_GetSpatialAnchorInfo(MikanSpatialAnchorID anchor_id, [Out] out MikanSpatialAnchorInfo out_anchor_info);

        [DllImport("Mikan_CAPI", CharSet = CharSet.Ansi)]
        public static extern MikanResult Mikan_FindSpatialAnchorInfoByName(string anchor_name, [Out] out MikanSpatialAnchorInfo out_anchor_info);

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_Disconnect();

        [DllImport("Mikan_CAPI")]
        public static extern MikanResult Mikan_Shutdown();
//#else
//    //TODO
//#endif
    }
}