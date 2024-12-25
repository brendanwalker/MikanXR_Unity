// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class MikanStencilBoxInfo
	{
		public static readonly long classId= -7257776522534565484;

		public int stencil_id;
		public int parent_anchor_id;
		public MikanTransform relative_transform;
		public float box_x_size;
		public float box_y_size;
		public float box_z_size;
		public bool is_disabled;
		public string stencil_name;
	};

	public class MikanStencilModelInfo
	{
		public static readonly long classId= -8053689253500337528;

		public int stencil_id;
		public int parent_anchor_id;
		public MikanTransform relative_transform;
		public bool is_disabled;
		public string stencil_name;
	};

	public class MikanStencilModelRenderGeometry
	{
		public static readonly long classId= 6822885306325183796;

		public List<MikanTriagulatedMesh> meshes;
	};

	public class MikanStencilQuadInfo
	{
		public static readonly long classId= -5584171141820643036;

		public int stencil_id;
		public int parent_anchor_id;
		public MikanTransform relative_transform;
		public float quad_width;
		public float quad_height;
		public bool is_double_sided;
		public bool is_disabled;
		public string stencil_name;
	};

	public class MikanTriagulatedMesh
	{
		public static readonly long classId= -1925804809077911022;

		public List<MikanVector3f> vertices;
		public List<MikanVector3f> normals;
		public List<MikanVector2f> texels;
		public List<int> indices;
	};

}
