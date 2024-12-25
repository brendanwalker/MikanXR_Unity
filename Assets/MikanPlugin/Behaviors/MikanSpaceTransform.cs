using MikanXR;
using UnityEngine;

namespace MikanXR
{
	public class MikanSpaceTransform
	{
		public Vector3 MikanSpacePosition = new Vector3(0, 0, 0);
		public Quaternion MikanSpaceOrientation = new Quaternion();
		public Vector3 MikanSpaceScale = new Vector3(1, 1, 1);
		public Matrix4x4 MikanSpaceTRSMat = Matrix4x4.identity;

		public void ApplyMikanTransform(MikanTransform mikanTransform)
		{
			MikanSpacePosition = MikanMath.MikanVector3fToVector3(mikanTransform.position);
			MikanSpaceOrientation = MikanMath.MikanQuatfToQuaternion(mikanTransform.rotation);
			MikanSpaceScale = MikanMath.MikanScaleVector3fToVector3(mikanTransform.scale);
			MikanSpaceTRSMat =
				Matrix4x4.TRS(
					MikanSpacePosition,
					MikanSpaceOrientation,
					MikanSpaceScale);
		}
	}
}
