// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanVideoSourceAttachmentChangedEvent : MikanEvent
	{
		public static new readonly long classId= -1389242778064515596;

	};

	public class MikanVideoSourceClosedEvent : MikanEvent
	{
		public static new readonly long classId= -5465858161967922385;

	};

	public class MikanVideoSourceIntrinsicsChangedEvent : MikanEvent
	{
		public static new readonly long classId= -4388222824097463291;

	};

	public class MikanVideoSourceModeChangedEvent : MikanEvent
	{
		public static new readonly long classId= 2985204106626338658;

	};

	public class MikanVideoSourceNewFrameEvent : MikanEvent
	{
		public static new readonly long classId= 1235622184979266446;

		public MikanVector3f cameraForward;
		public MikanVector3f cameraUp;
		public MikanVector3f cameraPosition;
		public long frame;
	};

	public class MikanVideoSourceOpenedEvent : MikanEvent
	{
		public static new readonly long classId= 5034499706629648632;

	};

}
