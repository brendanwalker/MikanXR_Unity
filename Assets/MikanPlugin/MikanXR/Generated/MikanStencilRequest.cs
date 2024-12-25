// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class GetBoxStencil : MikanRequest
	{
		public static new readonly long classId= -7977627892346889050;

		public int stencilId;
	};

	public class GetBoxStencilList : MikanRequest
	{
		public static new readonly long classId= 497666200403468338;

	};

	public class GetModelStencil : MikanRequest
	{
		public static new readonly long classId= 2139398247341621214;

		public int stencilId;
	};

	public class GetModelStencilList : MikanRequest
	{
		public static new readonly long classId= -6996149616550292566;

	};

	public class GetModelStencilRenderGeometry : MikanRequest
	{
		public static new readonly long classId= 7106057332746101286;

		public int stencilId;
	};

	public class GetQuadStencil : MikanRequest
	{
		public static new readonly long classId= 1551069120606568234;

		public int stencilId;
	};

	public class GetQuadStencilList : MikanRequest
	{
		public static new readonly long classId= 1562088813869505254;

	};

	public class MikanStencilBoxInfoResponse : MikanResponse
	{
		public static new readonly long classId= 8416540735772150643;

		public MikanStencilBoxInfo box_info;
	};

	public class MikanStencilListResponse : MikanResponse
	{
		public static new readonly long classId= -1151731472597403252;

		public List<int> stencil_id_list;
	};

	public class MikanStencilModelInfoResponse : MikanResponse
	{
		public static new readonly long classId= -7953197851410486201;

		public MikanStencilModelInfo model_info;
	};

	public class MikanStencilModelRenderGeometryResponse : MikanResponse
	{
		public static new readonly long classId= 6128619420232158675;

		public MikanStencilModelRenderGeometry render_geometry;
	};

	public class MikanStencilQuadInfoResponse : MikanResponse
	{
		public static new readonly long classId= 5745901639841426435;

		public MikanStencilQuadInfo quad_info;
	};

}
