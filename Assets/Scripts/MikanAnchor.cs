using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MikanXR.SDK.Unity
{
    /// The ID of a VR Device
    using MikanSpatialAnchorID = System.Int32;

    public class MikanAnchor : MonoBehaviour
    {
        private MikanSpatialAnchorID _anchorId = SDKConstants.INVALID_MIKAN_ID;

        public string AnchorName;

        // Start is called before the first frame update
        void Start()
        {
            if (MikanClient.Instance != null)
            {
                MikanClient.Instance.ConnectEvent.AddListener(OnMikanConnected);
                MikanClient.Instance.addAnchorPoseListener(_anchorId, AnchorPoseChanged);
            }
        }

        private void OnDestroy()
        {
            if (MikanClient.Instance != null)
            {
                MikanClient.Instance.removeAnchorPoseListener(_anchorId, AnchorPoseChanged);
            }
        }

        void OnMikanConnected()
        {
            FindAnchorInfo();
        }    

        void FindAnchorInfo()
        {
            if (MikanClientAPI.Mikan_GetIsConnected())
            {
                MikanSpatialAnchorInfo anchorInfo;
                if (MikanClientAPI.Mikan_FindSpatialAnchorInfoByName(AnchorName, out anchorInfo) == MikanResult.Success)
                {
                    _anchorId = anchorInfo.anchor_id;
                    AnchorPoseChanged(anchorInfo.anchor_xform);
                }
            }
        }

        void AnchorPoseChanged(MikanMatrix4f xform)
        {
            MikanMath.SetTransformFromMatrix(this.transform, ref xform);
        }
    }
}
