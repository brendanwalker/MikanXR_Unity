﻿using UnityEngine;

namespace MikanXRPlugin
{
    /// The ID of a VR Device
    using MikanSpatialAnchorID = System.Int32;

    public class MikanAnchor : MikanBehavior
    {
        private MikanSpatialAnchorID _anchorId = -1;
        public MikanSpatialAnchorID AnchorID => _anchorId;

        [SerializeField]
        private string _anchorName = "";
        public string AnchorName
        {
            get
            {
                return _anchorName;
            }
            set
            {
                _anchorName = value;
            }
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
                    _anchorId = mikanAnchorInfo.AnchorId;

                    // Update our scene transform now that we have an assigned anchor
                    UpdateSceneTransform();
                }
            }
        }

        public void UpdateSceneTransform()
        {
            if (AnchorID != -1)
            {
                MikanScene ownerScene = GetParentScene();

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
