// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public enum MikanVRDeviceApi
	{
		INVALID= 0,
		STEAM_VR= 1,
	};

	public enum MikanVRDeviceType
	{
		INVALID= 0,
		HMD= 1,
		CONTROLLER= 2,
		TRACKER= 3,
	};

	public class MikanVRDeviceInfo
	{
		public static readonly long classId= 2293209149503382551;

		public MikanVRDeviceApi vr_device_api;
		public MikanVRDeviceType vr_device_type;
		public string device_path;
	};

}
