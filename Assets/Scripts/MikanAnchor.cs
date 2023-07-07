using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mikan
{
    /// The ID of a VR Device
    using MikanSpatialAnchorID = System.Int32;

    public class MikanAnchor : MonoBehaviour
    {
        private MikanSpatialAnchorID _anchorId = MikanClient.INVALID_MIKAN_ID;
        public MikanSpatialAnchorID AnchorID { 
            get { return _anchorId; } 
        }
        
        [SerializeField]
        private string _anchorName= "";
        public string AnchorName { 
            get { return _anchorName; } 
            set { _anchorName = value; }
        }

        public MikanScene GetParentScene() 
        {
            return gameObject.GetComponentInParent<MikanScene>();
        }

        public void FindAnchorInfo()
        {
            MikanScene scene = GetParentScene();

            if (scene != null)
            {
                MikanAnchorInfo mikanAnchorInfo = scene.GetMikanAnchorInfoByName(_anchorName);

                if (mikanAnchorInfo != null)
                {
                    _anchorId= mikanAnchorInfo.AnchorId;

                    // Update our scene transform now that we have an assigned anchor
                    UpdateSceneTransform();
                }
            }
        }

        public void UpdateSceneTransform()
        {
            if (AnchorID != MikanClient.INVALID_MIKAN_ID)
            {
                MikanScene ownerScene= GetParentScene();

                if (ownerScene != null)
                {
                    MikanAnchorInfo mikanAnchorInfo = ownerScene.GetMikanAnchorInfoById(AnchorID);

                    if (mikanAnchorInfo != null)
                    {
                        // Get the anchor transform in Mikan Space
                        Matrix4x4 MikanSpaceTransform = mikanAnchorInfo.MikanSpaceTransform;

                        // Get the conversion from the scene to go from Mikan to Scene space
                        Matrix4x4 MikanToSceneXform = ownerScene.MikanToSceneTransform;

                        // Compute the scene space transform
                        Matrix4x4 SceneSpaceTransform = MikanToSceneXform * MikanSpaceTransform;

                        // Update the relative transform of the anchor
						transform.localPosition = MikanMath.ExtractTranslationFromMatrix(SceneSpaceTransform);
						transform.localRotation = MikanMath.ExtractRotationFromMatrix(SceneSpaceTransform);
						transform.localScale = MikanMath.ExtractScaleFromMatrix(SceneSpaceTransform);
					}
                }
            }
        }
    }
}
