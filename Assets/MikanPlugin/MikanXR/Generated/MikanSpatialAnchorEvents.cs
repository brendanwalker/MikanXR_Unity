// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanAnchorListUpdateEvent : MikanEvent
	{
		public static new readonly long classId= 4263170011815929379;

	};

	public class MikanAnchorNameUpdateEvent : MikanEvent
	{
		public static new readonly long classId= -9070327848687977506;

		public int anchor_id;
		public string anchor_name;
	};

	public class MikanAnchorPoseUpdateEvent : MikanEvent
	{
		public static new readonly long classId= 8819677263159765076;

		public MikanTransform transform;
		public int anchor_id;
	};

}
