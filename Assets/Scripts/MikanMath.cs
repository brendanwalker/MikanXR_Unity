using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikanXR.SDK.Unity
{

    public static class MikanMath 
	{
		// Mikan(OpenGL) -> Unity coordinate xform = (x, y, -z)
		// https://medium.com/comerge/what-are-the-coordinates-225f1ec0dd78

		public static Matrix4x4 MikanMatrix4fToMatrix4x4(MikanMatrix4f xform)
		{
			// Negate the z-component of each column to convert from Mikan to Unity Coordinate system
			return new Matrix4x4(
				new Vector4(xform.x0, xform.x1, xform.x2, xform.x3), // Column 0
				new Vector4(xform.y0, xform.y1, xform.y2, xform.y3), // Column 1
				new Vector4(-xform.z0, -xform.z1, -xform.z2, -xform.z3), // Column 2
				new Vector4(xform.w0, xform.w1, xform.w2, xform.w3)); // Column 3
		}

		public static MikanVector3f Vector3ToMikanVector3f(Vector3 v)
		{
			// Negate z-component to convert from Mikan to Unity Coordinate system
			return new MikanVector3f() {x = v.x, y = v.y, z = -v.z};
		}
	}
}