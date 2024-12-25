using UnityEngine;

namespace MikanXR
{
	public class MikanBoxStencil : MikanStencil
	{
		public static MikanBoxStencil SpawnStencil(MikanScene ownerScene, MikanStencilBoxInfo modelStencilInfo)
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

			var stencil = stencilGO.GetComponent<MikanBoxStencil>();
			if (stencil == null)
			{
				stencil = stencilGO.AddComponent<MikanBoxStencil>();
			}

			stencil.Setup(
				ownerScene,
				modelStencilInfo.stencil_id,
				modelStencilInfo.stencil_name,
				modelStencilInfo.relative_transform);

			return stencil;
		}

		public void ApplyBoxStencilInfo(MikanStencilBoxInfo mikanStencilBox)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilBox.relative_transform);

			UpdateSceneTransform();
		}
	}
}
