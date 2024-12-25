// This file is auto generated. DO NO EDIT.
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public class AllocateRenderTargetTextures : MikanRequest
	{
		public static new readonly long classId= -3277355074821076887;

		public MikanRenderTargetDescriptor descriptor;
	};

	public class FreeRenderTargetTextures : MikanRequest
	{
		public static new readonly long classId= -1540001020382784652;

	};

	public class PublishRenderTargetTextures : MikanRequest
	{
		public static new readonly long classId= 1665533327486038831;

		public long frameIndex;
	};

	public class WriteColorRenderTargetTexture : MikanRequest
	{
		public static new readonly long classId= -1794938483344910945;

		public IntPtr apiColorTexturePtr;
	};

	public class WriteDepthRenderTargetTexture : MikanRequest
	{
		public static new readonly long classId= -6513232297008704103;

		public IntPtr apiDepthTexturePtr;
		public float zNear;
		public float zFar;
	};

}
