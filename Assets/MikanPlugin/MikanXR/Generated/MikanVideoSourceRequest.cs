// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class GetVideoSourceAttachment : MikanRequest
	{
		public static new readonly long classId= 2729677767526720868;

	};

	public class GetVideoSourceIntrinsics : MikanRequest
	{
		public static new readonly long classId= 5111634741046016445;

	};

	public class GetVideoSourceMode : MikanRequest
	{
		public static new readonly long classId= -5470313175782314738;

	};

	public class MikanVideoSourceAttachmentInfoResponse : MikanResponse
	{
		public static new readonly long classId= -7842986529463861117;

		public int attached_vr_device_id;
		public MikanMatrix4f vr_device_offset_xform;
	};

	public class MikanVideoSourceIntrinsicsResponse : MikanResponse
	{
		public static new readonly long classId= 5018187312099351234;

		public MikanVideoSourceIntrinsics intrinsics;
	};

	public class MikanVideoSourceModeResponse : MikanResponse
	{
		public static new readonly long classId= -1059487460754321771;

		public MikanVideoSourceType video_source_type;
		public MikanVideoSourceApi video_source_api;
		public string device_path;
		public string video_mode_name;
		public int resolution_x;
		public int resolution_y;
		public float frame_rate;
	};

}
