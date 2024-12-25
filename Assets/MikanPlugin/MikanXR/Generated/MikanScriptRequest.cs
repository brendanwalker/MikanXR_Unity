// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class GetVRDeviceInfo : MikanRequest
	{
		public static new readonly long classId= -3800049373124041613;

		public int deviceId;
	};

	public class GetVRDeviceList : MikanRequest
	{
		public static new readonly long classId= 6797511981020466783;

	};

	public class MikanVRDeviceInfoResponse : MikanResponse
	{
		public static new readonly long classId= -2644634697764695900;

		public MikanVRDeviceInfo vr_device_info;
	};

	public class MikanVRDeviceListResponse : MikanResponse
	{
		public static new readonly long classId= -504651662541717832;

		public List<int> vr_device_id_list;
	};

	public class SendScriptMessage : MikanRequest
	{
		public static new readonly long classId= -3006836539234531471;

		public MikanScriptMessageInfo message;
	};

	public class SubscribeToVRDevicePoseUpdates : MikanRequest
	{
		public static new readonly long classId= -6156509566154826557;

		public int deviceId;
	};

	public class UnsubscribeFromVRDevicePoseUpdates : MikanRequest
	{
		public static new readonly long classId= 796067758472639459;

		public int deviceId;
	};

}
