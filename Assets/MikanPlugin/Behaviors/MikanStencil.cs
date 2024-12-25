using MikanXR;
using UnityEngine;

namespace MikanXR
{
	/// The ID of a Stencil in Mikan
	using MikanStencilID = System.Int32;

	public class MikanStencil : MonoBehaviour
	{
		public MikanScene OwnerScene
		{
			get; private set;
		}

		protected MikanStencilID _stencilId = -1;
		public MikanStencilID StencilID => _stencilId;

		protected string _stencilName = "";
		public string StencilName => _stencilName;

		protected MikanSpaceTransform _mikanSpaceTransform = new MikanSpaceTransform();
		public MikanSpaceTransform MikanSpaceTransform => _mikanSpaceTransform;

		public static void DespawnAnchor(MikanStencil stencil)
		{
			if (stencil != null)
			{
				stencil.Teardown();
				Destroy(stencil.gameObject);
			}
		}

		protected virtual void Setup(
			MikanScene ownerScene,
			MikanStencilID stencilId,
			string stencilName,
			MikanTransform relativeTransform)
		{
			OwnerScene = ownerScene;

			_stencilId = stencilId;
			_stencilName = stencilName;

			// Update our transform in Mikan Space
			_mikanSpaceTransform.ApplyMikanTransform(relativeTransform);

			// Update the Unity transform
			UpdateSceneTransform();
		}

		protected virtual void Teardown()
		{
		}

		public void HandleStencilPoseChanged(MikanStencilPoseUpdateEvent stencilPoseEvent)
		{
			// Update the stencil transform from the event
			MikanSpaceTransform.ApplyMikanTransform(stencilPoseEvent.transform);

			UpdateSceneTransform();
		}

		protected void UpdateSceneTransform()
		{
			// Get the anchor transform in Mikan Space
			Matrix4x4 SceneSpaceTransform = _mikanSpaceTransform.MikanSpaceTRSMat;

			// Update the relative transform of the anchor
			transform.localPosition = MikanMath.ExtractTranslationFromMatrix(SceneSpaceTransform);
			transform.localRotation = MikanMath.ExtractRotationFromMatrix(SceneSpaceTransform);
			transform.localScale = MikanMath.ExtractScaleFromMatrix(SceneSpaceTransform);
		}
	}
}
