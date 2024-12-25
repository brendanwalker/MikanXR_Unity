// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public enum MikanIntrinsicsType
	{
		INVALID= 0,
		MONO_CAMERA_INTRINSICS= 1,
		STEREO_CAMERA_INTRINSICS= 2,
	};

	public enum MikanVideoSourceApi
	{
		INVALID= 0,
		OPEN_CV= 1,
		WINDOWS_MEDIA_FOUNDATION= 2,
	};

	public enum MikanVideoSourceType
	{
		MONO= 0,
		STEREO= 1,
	};

	public class MikanCameraIntrinsics
	{
		public static readonly long classId= -1980224418616951122;

		public double pixel_width;
		public double pixel_height;
		public double hfov;
		public double vfov;
		public double znear;
		public double zfar;
	};

	public class MikanDistortionCoefficients
	{
		public static readonly long classId= -2596555002374434624;

		public double k1;
		public double k2;
		public double k3;
		public double k4;
		public double k5;
		public double k6;
		public double p1;
		public double p2;
	};

	public class MikanMonoIntrinsics : MikanCameraIntrinsics
	{
		public static new readonly long classId= 4896055255137140914;

		public MikanDistortionCoefficients distortion_coefficients;
		public MikanMatrix3d camera_matrix;
	};

	public class MikanStereoIntrinsics : MikanCameraIntrinsics
	{
		public static new readonly long classId= -261934067861644075;

		public MikanDistortionCoefficients left_distortion_coefficients;
		public MikanMatrix3d left_camera_matrix;
		public MikanDistortionCoefficients right_distortion_coefficients;
		public MikanMatrix3d right_camera_matrix;
		public MikanMatrix3d left_rectification_rotation;
		public MikanMatrix3d right_rectification_rotation;
		public MikanMatrix4x3d left_rectification_projection;
		public MikanMatrix4x3d right_rectification_projection;
		public MikanMatrix3d rotation_between_cameras;
		public MikanVector3d translation_between_cameras;
		public MikanMatrix3d essential_matrix;
		public MikanMatrix3d fundamental_matrix;
		public MikanMatrix4d reprojection_matrix;
	};

	public class MikanVideoSourceIntrinsics
	{
		public static readonly long classId= -5073913459979558727;

		public SerializableObject<MikanCameraIntrinsics> intrinsics_ptr;
		public MikanIntrinsicsType intrinsics_type;
	};

}
