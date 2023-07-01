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

        public string AnchorName;

        // Start is called before the first frame update
        void Start()
        {
            if (MikanComponent.Instance != null)
            {
                MikanComponent.Instance.OnConnectEvent.AddListener(OnMikanConnected);
                MikanComponent.Instance.addAnchorPoseListener(_anchorId, AnchorPoseChanged);
            }
        }

        private void OnDestroy()
        {
            if (MikanComponent.Instance != null)
            {
                MikanComponent.Instance.removeAnchorPoseListener(_anchorId, AnchorPoseChanged);
            }
        }

        void OnMikanConnected()
        {
            FindAnchorInfo();
        }    

        void FindAnchorInfo()
        {
            if (MikanClient.Mikan_GetIsConnected())
            {
                MikanSpatialAnchorInfo anchorInfo = new MikanSpatialAnchorInfo();
                if (MikanClient.Mikan_FindSpatialAnchorInfoByName(AnchorName, anchorInfo) == MikanResult.Success)
                {
                    _anchorId = anchorInfo.anchor_id;
                    AnchorPoseChanged(anchorInfo.world_transform);
                }
            }
        }

        void AnchorPoseChanged(MikanTransform xform)
        {
            transform.localPosition= MikanMath.MikanVector3fToVector3(xform.position);
            transform.localRotation= MikanMath.MikanQuatfToQuaternion(xform.rotation);
            transform.localScale= MikanMath.MikanVector3fToVector3(xform.scale);
        }
    }
}
