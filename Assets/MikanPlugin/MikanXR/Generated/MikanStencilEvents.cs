// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanBoxStencilListUpdateEvent : MikanEvent
	{
		public static new readonly long classId= -2219490838934601883;

	};

	public class MikanModelStencilListUpdateEvent : MikanEvent
	{
		public static new readonly long classId= 5443530884967588781;

	};

	public class MikanQuadStencilListUpdateEvent : MikanEvent
	{
		public static new readonly long classId= -1043716818970806071;

	};

	public class MikanStencilNameUpdateEvent : MikanEvent
	{
		public static new readonly long classId= 5445856050236674031;

		public int stencil_id;
		public string stencil_name;
	};

	public class MikanStencilPoseUpdateEvent : MikanEvent
	{
		public static new readonly long classId= -6555276302420518083;

		public MikanTransform transform;
		public int stencil_id;
	};

}
