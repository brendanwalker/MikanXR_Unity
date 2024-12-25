// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanVRDeviceListUpdateEvent : MikanEvent
	{
		public static new readonly long classId= -2074884757236690202;

	};

	public class MikanVRDevicePoseUpdateEvent : MikanEvent
	{
		public static new readonly long classId= 3423063131481365449;

		public MikanMatrix4f transform;
		public int device_id;
		public long frame;
	};

}
