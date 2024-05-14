using System;
using System.Collections.Generic;
using UnityEngine;
using MikanXR;

namespace MikanXRPlugin
{
    using MikanSpatialAnchorID = Int32;

    [Serializable]
    public class MikanAnchorInfo
    {
        public MikanSpatialAnchorID AnchorId = -1;
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

    public class MikanScene : MikanBehavior
    {
        [SerializeField]
        private string _sceneOriginAnchorName = "";
        public string SceneOriginAnchorName
        {
            get
            {
                return _sceneOriginAnchorName;
            }
            set
            {
                _sceneOriginAnchorName = value;
                RecomputeMikanToSceneTransform();
            }
        }

        [SerializeField]
        private float _cameraPositionScale = 1.0f;
        public float CameraPositionScale
        {
            get
            {
                return _cameraPositionScale;
            }
            set
            {
                _cameraPositionScale = Math.Max(value, 0.001f);
            }
        }

		private GameObject _sceneCameraObject = null;
		private MikanCamera _sceneCamera = null;
        public MikanCamera SceneCamera => _sceneCamera;

        private Matrix4x4 _mikanToSceneTransform = Matrix4x4.identity;
        public Matrix4x4 MikanToSceneTransform => _mikanToSceneTransform;

        // The table of anchors fetched from Mikan
        private Dictionary<MikanSpatialAnchorID, MikanAnchorInfo> _mikanAnchorInfoMap = new Dictionary<MikanSpatialAnchorID, MikanAnchorInfo>();

        // A list of child MikanAnchor components bound to corresponding MikanAnchorInfo by name
        private List<MikanAnchor> _sceneAnchors = new List<MikanAnchor>();

        public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);


        void OnEnable()
        {
            _mikanToSceneTransform = Matrix4x4.identity;

            // Bind client events
            Client.OnConnected.AddListener(HandleMikanConnected);
            Client.OnDisconnected.AddListener(HandleMikanDisconnected);
			Client.OnRenderBufferCreated.AddListener(HandleRenderBufferCreated);
            Client.OnRenderBufferDisposed.AddListener(HandleRenderBufferDisposed);
			Client.OnAnchorListChanged.AddListener(HandleAnchorListChanged);
            Client.OnAnchorPoseChanged.AddListener(HandleAnchorPoseChanged);
            Client.OnCameraAttachmentChanged.AddListener(HandleCameraAttachmentChanged);
            Client.OnNewFrameReceived.AddListener(HandleNewVideoSourceFrame);

            // Gather all of the scene anchor components attached to the scene
            RebuildSceneAnchorList();

            // Spawn the Mikan camera
            SpawnMikanCamera();

			// If we are already connected to Mikan,
			// tell the scene to fetch anchors, setup scene transform, etc
			// Otherwise wait for MikanManager to tell the scene about the connection
			if (ClientAPI.GetIsConnected())
			{
				HandleMikanConnected();
			}
		}

		void OnDisable()
        {
            // Unbind client events
            Client.OnConnected.RemoveListener(HandleMikanConnected);
            Client.OnDisconnected.RemoveListener(HandleMikanDisconnected);
			Client.OnRenderBufferCreated.RemoveListener(HandleRenderBufferCreated);
            Client.OnRenderBufferDisposed.RemoveListener(HandleRenderBufferDisposed);
			Client.OnAnchorListChanged.RemoveListener(HandleAnchorListChanged);
            Client.OnAnchorPoseChanged.RemoveListener(HandleAnchorPoseChanged);
            Client.OnCameraAttachmentChanged.RemoveListener(HandleCameraAttachmentChanged);
            Client.OnNewFrameReceived.RemoveListener(HandleNewVideoSourceFrame);

            // Despawn the Mikan camera
            DespawnMikanCamera();
        }

		void SpawnMikanCamera()
		{
			if (_sceneCameraObject == null)
			{
				Log(MikanLogLevel.Info, "MikanManager: Spawning Mikan camera");

				_sceneCameraObject = new GameObject(
					"MikanCameraObject",
					new System.Type[] {
						typeof(Camera),
						typeof(MikanCamera)});

				Camera cameraComponent = _sceneCameraObject.GetComponent<Camera>();
				cameraComponent.stereoTargetEye = StereoTargetEyeMask.None;
				cameraComponent.backgroundColor = new Color(0, 0, 0, 0);
				cameraComponent.clearFlags = CameraClearFlags.SolidColor;
				cameraComponent.forceIntoRenderTexture = true;
				cameraComponent.transform.parent = this.transform;

				_sceneCamera = _sceneCameraObject.GetComponent<MikanCamera>();

				// Tell the MikanCamera to look for a sibling Camera component
				_sceneCamera.BindXRCamera();

				// If we are connected to Mikan, 
				// copy the camera intrinsics (fov, pixel dimensions, etc) to the Unity Camera
				// If not, wait for Mikan to connect and we'll copy the settings then
				_sceneCamera.HandleCameraIntrinsicsChanged();

				// If we have a valid render target descriptor (defined video source),
				// create a valid render target from the descriptor
				// If not, wait for Mikan to connect and we'll create it then
				if (Client.IsRenderTargetDescriptorValid)
				{
					_sceneCamera.RecreateRenderTarget(Client.RenderTargetDescriptor);
				}
			}
			else
			{
				Log(MikanLogLevel.Info, "MikanManager: Ignoring camera spawn request. Already spawned.");
			}
		}

		void DespawnMikanCamera()
		{
			if (_sceneCamera != null)
			{
				Log(MikanLogLevel.Info, "MikanManager: Despawn Mikan camera");
				_sceneCamera.DisposeRenderTarget();
                _sceneCamera= null;

				Destroy(_sceneCameraObject);
				_sceneCameraObject = null;
			}
			else
			{
				Log(MikanLogLevel.Info, "MikanManager: Ignoring camera de-spawn request. Already despawned.");
			}
		}

		public MikanAnchorInfo GetMikanAnchorInfoById(MikanSpatialAnchorID anchorId)
        {
            if (_mikanAnchorInfoMap.TryGetValue(anchorId, out MikanAnchorInfo anchorInfo))
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

        protected void HandleMikanConnected()
        {
            HandleCameraIntrinsicsChanged();
            HandleAnchorListChanged();
        }

        protected void HandleMikanDisconnected()
        {
            HandleRenderBufferDisposed();
        }

        protected void HandleRenderBufferCreated()
        {
			if (Client.IsRenderTargetDescriptorValid)
			{
                if (_sceneCamera != null)
                {
					_sceneCamera.RecreateRenderTarget(Client.RenderTargetDescriptor);
				}
                else
                {
                    Log(MikanLogLevel.Warning, "MikanScene: Missing expected scene camera.");
                }
			}
			else
			{
				Log(MikanLogLevel.Error, "MikanScene: Invalid render target descriptor!");
			}
		}

        protected void HandleRenderBufferDisposed()
        {
			if (_sceneCamera != null)
            {
				_sceneCamera.DisposeRenderTarget();
			}
		}

        public async void HandleAnchorListChanged()
        {
            _mikanAnchorInfoMap.Clear();

            MikanResponse listResponse = await ClientAPI.SpatialAnchorAPI.GetSpatialAnchorList();
            if (listResponse.resultCode == MikanResult.Success)
            {
                MikanSpatialAnchorList spatialAnchorList = listResponse as MikanSpatialAnchorList;

                foreach (var anchorID in spatialAnchorList.spatial_anchor_id_list)
                {
                    MikanResponse anchorResponse = await ClientAPI.SpatialAnchorAPI.getSpatialAnchorInfo(anchorID);
                    if (anchorResponse.resultCode == MikanResult.Success)
                    {
                        MikanSpatialAnchorInfo mikanAnchorInfo = anchorResponse as MikanSpatialAnchorInfo;
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
                bool bUpdateAllAnchors = false;
                if (sceneAnchorInfo.AnchorName == _sceneOriginAnchorName)
                {
                    RecomputeMikanToSceneTransform();
                    bUpdateAllAnchors = true;
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

        public async void HandleCameraAttachmentChanged()
        {
            MikanResponse response = await ClientAPI.VideoSourceAPI.GetVideoSourceAttachment();
            if (response.resultCode == MikanResult.Success)
            {
                //var attachInfo = response as MikanVideoSourceAttachmentInfo;

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
            _sceneAnchors = new List<MikanAnchor>(gameObject.GetComponentsInChildren<MikanAnchor>());
        }
    }
}