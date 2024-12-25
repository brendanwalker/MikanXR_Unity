using UnityEngine;

namespace MikanXR
{
    /// The ID of a Spatial Anchor in Mikan
    using MikanSpatialAnchorID = System.Int32;

    public class MikanAnchor : MonoBehaviour
    {
        public MikanScene OwnerScene { get; private set; }

        private MikanSpatialAnchorID _anchorId = -1;
        public MikanSpatialAnchorID AnchorID => _anchorId;

		private string _anchorName = "";
		public string AnchorName => _anchorName;

		private MikanSpaceTransform _mikanSpaceTransform = new MikanSpaceTransform();
        public MikanSpaceTransform MikanSpaceTransform => _mikanSpaceTransform;

        public static MikanAnchor SpawnAnchor(MikanScene ownerScene, MikanSpatialAnchorInfo anchorInfo)
        {
            var anchorPrefab= ownerScene.OwnerManager.OwnerPlugin.anchorPrefab;

            GameObject anchorGO;
            if (anchorPrefab != null)
            {
                anchorGO = Instantiate(anchorPrefab);
            }
            else
            {
				anchorGO = new GameObject();
			}
			anchorGO.name = anchorInfo.anchor_name;
            anchorGO.transform.SetParent(ownerScene.transform, false);

			var anchor = anchorGO.GetComponent<MikanAnchor>();
            if (anchor == null)
            {
				anchor = anchorGO.AddComponent<MikanAnchor>();
			}

            anchor.Setup(ownerScene, anchorInfo);

            return anchor;
		}

        public static void DespawnAnchor(MikanAnchor anchor)
        {
            if (anchor != null)
            {
                anchor.Teardown();
				Destroy(anchor.gameObject);
			}
		}

        private void Setup(MikanScene ownerScene, MikanSpatialAnchorInfo anchorInfo)
        {
            OwnerScene= ownerScene;

            ApplyAnchorInfo(anchorInfo);
        }

        private void Teardown()
        {
		}

        public void ApplyAnchorInfo(MikanSpatialAnchorInfo anchorInfo)
        {
            _anchorId = anchorInfo.anchor_id;
            _anchorName = anchorInfo.anchor_name;

            // Update our transform in Mikan Space
            _mikanSpaceTransform.ApplyMikanTransform(anchorInfo.world_transform);

            // Update the Unity transform
            UpdateSceneTransform();
        }

		public void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			// Update the scene anchor transform from the event
			MikanSpaceTransform.ApplyMikanTransform(AnchorPoseEvent.transform);

			UpdateSceneTransform();
		}

		private void UpdateSceneTransform()
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
