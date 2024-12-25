// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public enum MikanAPIResult
	{
		Success= 0,
		GeneralError= 1,
		Uninitialized= 2,
		NullParam= 3,
		InvalidParam= 4,
		RequestFailed= 5,
		NotConnected= 6,
		AlreadyConnected= 7,
		SocketError= 8,
		Timeout= 9,
		Canceled= 10,
		NoData= 11,
		BufferTooSmall= 12,
		UnknownClient= 13,
		UnknownFunction= 14,
		MalformedParameters= 15,
		MalformedResponse= 16,
		NoVideoSource= 100,
		NoVideoSourceAssignedTracker= 101,
		InvalidDeviceId= 102,
		InvalidStencilID= 103,
		InvalidAnchorID= 104,
	};

	public class MikanEvent
	{
		public static readonly long classId= 8521159033538382795;

		public long eventTypeId;
		public string eventTypeName;
	};

	public class MikanRequest
	{
		public static readonly long classId= 1095719431187359814;

		public long requestTypeId;
		public string requestTypeName;
		public int requestId;
	};

	public class MikanResponse
	{
		public static readonly long classId= 7094118849615581562;

		public long responseTypeId;
		public string responseTypeName;
		public int requestId;
		public MikanAPIResult resultCode;
	};

}
