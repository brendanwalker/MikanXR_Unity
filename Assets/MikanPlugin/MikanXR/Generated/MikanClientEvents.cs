// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanConnectedEvent : MikanEvent
	{
		public static new readonly long classId= -8563579496677618876;

		public MikanClientAPIVersion serverVersion;
		public MikanClientAPIVersion minClientVersion;
		public bool isClientCompatible;
	};

	public class MikanDisconnectedEvent : MikanEvent
	{
		public static new readonly long classId= -4899718033844115118;

		public MikanDisconnectCode code;
		public string reason;
	};

}
