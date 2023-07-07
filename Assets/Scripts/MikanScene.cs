using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using MikanXR;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#endif

namespace Mikan
{
    using MikanSpatialAnchorID = System.Int32;

    [System.Serializable]
    public class MikanAnchorInfo
    {
        public MikanSpatialAnchorID AnchorId= MikanClient.INVALID_MIKAN_ID;
		public string AnchorName = "";
		public Vector3 MikanSpacePosition = new Vector3(0, 0, 0);
        public Quaternion MikanSpaceOrientation = new Quaternion();
        public Vector3 MikanSpaceScale = new Vector3(1, 1, 1);
		public Matrix4x4 MikanSpaceTransform = Matrix4x4.identity;

        public void ApplyMikanTransform(MikanTransform mikanTransform)
        {
			MikanSpacePosition = MikanMath.MikanVector3fToVector3(mikanTransform.position);
			MikanSpaceOrientation = MikanMath.MikanQuatfToQuaternion(mikanTransform.rotation);
			MikanSpaceScale = MikanMath.MikanScaleVector3fToVector3(mikanTransform.scale);
			MikanSpaceTransform =
				Matrix4x4.TRS(
					MikanSpacePosition,
					MikanSpaceOrientation,
					MikanSpaceScale);
		}
    }

    public class MikanScene : MonoBehaviour
    {
		[SerializeField]
		private string _sceneOriginAnchorName= "";
		public string SceneOriginAnchorName
		{
			get { return _sceneOriginAnchorName; }
			set { 
				_sceneOriginAnchorName = value;
				RecomputeMikanToSceneTransform();
			}
		}

		[SerializeField]
		private float _cameraPositionScale = 1.0f;
		public float CameraPositionScale
		{
			get { 
				return _cameraPositionScale; 
			}
			set {
				_cameraPositionScale = Math.Max(value, 0.001f);
			}
		}

		[SerializeField]
		private MikanCamera _sceneCamera = null;
		public MikanCamera SceneCamera
		{
			get {
				return _sceneCamera;
			}
			set {
				_sceneCamera = value;
			}
		}

		private Matrix4x4 _mikanToSceneTransform= Matrix4x4.identity;
        public Matrix4x4 MikanToSceneTransform
        {
            get { return _mikanToSceneTransform; } 
        }

		// The table of anchors fetched from Mikan
		private Dictionary<MikanSpatialAnchorID, MikanAnchorInfo> _mikanAnchorInfoMap = new Dictionary<MikanSpatialAnchorID, MikanAnchorInfo>();

        // A list of child MikanAnchor components bound to corresponding MikanAnchorInfo by name
		private List<MikanAnchor> _sceneAnchors = new List<MikanAnchor>();

        public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);


		void OnEnable()
		{
			_mikanToSceneTransform= Matrix4x4.identity;

			// Find the first attached mikan scene
			BindSceneCamera();

			// Gather all of the scene anchor components attached to the scene
			RebuildSceneAnchorList();

			// Register ourselves with the MikanManager
			MikanManager.Instance.BindMikanScene(this);
		}

		void OnDisable()
		{
			MikanManager.Instance.UnbindMikanScene(this);
		}

		public MikanAnchorInfo GetMikanAnchorInfoById(MikanSpatialAnchorID anchorId)
		{
			MikanAnchorInfo anchorInfo = null;
			if (_mikanAnchorInfoMap.TryGetValue(anchorId, out anchorInfo))
			{
				return anchorInfo;
			}

			return null;
		}

		public MikanAnchorInfo GetMikanAnchorInfoByName(string anchorName)
		{
			foreach (MikanAnchorInfo anchorInfo in _mikanAnchorInfoMap.Values)
			{
				if (anchorInfo.AnchorName == anchorName)
				{
					return anchorInfo;
				}
			}

			return null;
		}

        public void HandleMikanConnected()
        {
			HandleCameraIntrinsicsChanged();
			HandleAnchorListChanged();
		}

		public void HandleMikanDisconnected()
		{

		}

        public void HandleAnchorListChanged()
        {
			_mikanAnchorInfoMap.Clear();

			MikanSpatialAnchorList spatialAnchorList = new MikanSpatialAnchorList();
			if (MikanClient.Mikan_GetSpatialAnchorList(spatialAnchorList) == MikanResult.Success)
			{
				for (int index = 0; index < spatialAnchorList.spatial_anchor_count; ++index)
				{
					MikanSpatialAnchorID anchorID = spatialAnchorList.spatial_anchor_id_list[index];

					MikanSpatialAnchorInfo mikanAnchorInfo = new MikanSpatialAnchorInfo();
					if (MikanClient.Mikan_GetSpatialAnchorInfo(anchorID, mikanAnchorInfo) == MikanResult.Success)
					{
						MikanAnchorInfo sceneAnchorInfo = new MikanAnchorInfo();

						// Copy over the anchor id
						sceneAnchorInfo.AnchorId = anchorID;

						// Copy over the anchor name
						sceneAnchorInfo.AnchorName = mikanAnchorInfo.anchor_name;

						// Get the transform of the anchor in Mikan Space
                        sceneAnchorInfo.ApplyMikanTransform(mikanAnchorInfo.world_transform);

						_mikanAnchorInfoMap.Add(anchorID, sceneAnchorInfo);
					}
				}
			}

			// We can now recompute the mikan->scene transform now that the anchors are up to date
			RecomputeMikanToSceneTransform();

			// Finally, update scene transform on all registered scene anchors
			foreach (MikanAnchor mikanAnchor in _sceneAnchors)
			{
				mikanAnchor.FindAnchorInfo();
			}
		}

		public void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
        {
            MikanAnchorInfo sceneAnchorInfo = GetMikanAnchorInfoById(AnchorPoseEvent.anchor_id);

            if (sceneAnchorInfo != null)
            {
                // Update the scene anchor transform from the event
                sceneAnchorInfo.ApplyMikanTransform(AnchorPoseEvent.transform);

				// If the anchor we are using as the scene origin changed,
				// we need to recompute the MikanToSceneTransform
				bool bUpdateAllAnchors= false;
				if (sceneAnchorInfo.AnchorName == _sceneOriginAnchorName)
				{
					RecomputeMikanToSceneTransform();
					bUpdateAllAnchors= true;
				}

                // Update all transforms associated anchor components
                foreach (MikanAnchor anchor in _sceneAnchors)
                {
					if (bUpdateAllAnchors || anchor.AnchorID == AnchorPoseEvent.anchor_id)
					{
                    	anchor.UpdateSceneTransform();
					}
                }
            }
        }

		public void HandleCameraIntrinsicsChanged()
		{
			if (_sceneCamera != null)
			{
				_sceneCamera.HandleCameraIntrinsicsChanged();
			}
		}

		public void HandleCameraAttachmentChanged()
		{
			MikanVideoSourceAttachmentInfo attachInfo = new MikanVideoSourceAttachmentInfo();
			if (MikanClient.Mikan_GetVideoSourceAttachment(attachInfo) == MikanResult.Success)
			{
				RecomputeMikanToSceneTransform();
			}
		}

		public void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
		{
			if (_sceneCamera != null)
			{
				// Compute the camera transform in Mikan Space (but in Unity coordinate system)
				Vector3 unityCameraPosition = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraPosition);
				Vector3 unityCameraForward = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraForward);
				Vector3 unityCameraUp = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraUp);
				Quaternion unityCameraRotation = Quaternion.LookRotation(unityCameraForward, unityCameraUp);
				Matrix4x4 unityCameraTransform = Matrix4x4.TRS(unityCameraPosition, unityCameraRotation, Vector3.one);

				// Compute the Scene space camera transform
				Matrix4x4 sceneCameraTransform = MikanToSceneTransform * unityCameraTransform;

				// Update the scene camera transform
				_sceneCamera.transform.localPosition = 
					MikanMath.ExtractTranslationFromMatrix(sceneCameraTransform) * _cameraPositionScale;
				_sceneCamera.transform.localRotation = 
					MikanMath.ExtractRotationFromMatrix(sceneCameraTransform);
				// For now we don't want to bother with modifying camera scale?
				//_sceneCamera.transform.localScale = MikanMath.ExtractScaleFromMatrix(sceneCameraTransform);

				// Render out a new frame
				_sceneCamera.CaptureFrame(newFrameEvent.frame);
			}
		}

		public float GetSceneScale()
		{
			return _cameraPositionScale;
		}

		public void SetSceneScale(float newScale)
		{
			_cameraPositionScale = Math.Max(newScale, 0.001f);
		}

		private void RecomputeMikanToSceneTransform()
		{
			// Get the scene origin anchor, if any given
			MikanAnchorInfo originAnchorInfo = GetMikanAnchorInfoByName(SceneOriginAnchorName);

			if (originAnchorInfo != null)
			{
				// Undo the origin anchor transform, then apply camera scale
				_mikanToSceneTransform = originAnchorInfo.MikanSpaceTransform.inverse;
			}
			else
			{
				// Just apply the camera scale
				_mikanToSceneTransform = Matrix4x4.identity;
			}
		}

		public void RebuildSceneAnchorList()
		{
			_sceneAnchors= new List<MikanAnchor>(gameObject.GetComponentsInChildren<MikanAnchor>());
		}

		public void BindSceneCamera()
		{
			_sceneCamera = gameObject.GetComponentInChildren<MikanCamera>();
		}
    }
}