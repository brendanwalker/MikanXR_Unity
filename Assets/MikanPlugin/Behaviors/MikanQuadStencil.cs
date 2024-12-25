using MikanXR;
using UnityEngine;

namespace MikanXR
{
	public class MikanQuadStencil : MikanStencil
	{
		public static MikanQuadStencil SpawnStencil(MikanScene ownerScene, MikanStencilQuadInfo modelStencilInfo)
		{
			var stencilPrefab = ownerScene.OwnerManager.OwnerPlugin.stencilPrefab;

			GameObject stencilGO;
			if (stencilPrefab != null)
			{
				stencilGO = Instantiate(stencilPrefab);
			}
			else
			{
				stencilGO = new GameObject();
			}
			stencilGO.name = modelStencilInfo.stencil_name;
			stencilGO.transform.SetParent(ownerScene.transform, false);

			var stencil = stencilGO.GetComponent<MikanQuadStencil>();
			if (stencil == null)
			{
				stencil = stencilGO.AddComponent<MikanQuadStencil>();
			}

			stencil.Setup(
				ownerScene,
				modelStencilInfo.stencil_id,
				modelStencilInfo.stencil_name,
				modelStencilInfo.relative_transform);

			return stencil;
		}

		public void ApplyStencilInfo(MikanStencilQuadInfo mikanStencilQuad)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilQuad.relative_transform);

			UpdateSceneTransform();
		}
	}
}
