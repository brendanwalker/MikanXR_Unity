using System;
using System.Collections.Generic;
using UnityEngine;

namespace MikanXR
{
	using MikanSpatialAnchorID = Int32;
	using MikanStencilID = Int32;

	public class MikanScene : MonoBehaviour
	{
		public MikanManager OwnerManager { get; private set; }
		private MikanClient _client = null;
		private MikanLogger _logger = null;

		private MikanCamera _sceneCamera = null;
		public MikanCamera SceneCamera => _sceneCamera;

		// The table of anchors currently active in the scene
		private Dictionary<MikanSpatialAnchorID, MikanAnchor> _sceneAnchors
			= new Dictionary<MikanSpatialAnchorID, MikanAnchor>();

		// Tables of each kind of stencil currently active in the scene
		private Dictionary<MikanStencilID, MikanQuadStencil> _sceneQuadStencils
			= new Dictionary<MikanStencilID, MikanQuadStencil>();
		private Dictionary<MikanStencilID, MikanBoxStencil> _sceneBoxStencils
			= new Dictionary<MikanStencilID, MikanBoxStencil>();
		private Dictionary<MikanStencilID, MikanModelStencil> _sceneModelStencils
			= new Dictionary<MikanStencilID, MikanModelStencil>();

		public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);

		private static readonly float ManipulatorSize = 0.1f;

		public static MikanScene SpawnScene(MikanManager ownerManager)
		{
			var mikanSceneObject = new GameObject(
				"MikanSceneObject",
				new System.Type[] {
					typeof(MikanScene),
					typeof(BoxCollider)
				});

			MikanScene sceneComponent = mikanSceneObject.GetComponent<MikanScene>();
			sceneComponent.Setup(ownerManager);

			return sceneComponent;
		}

		public void DespawnScene(MikanScene mikanScene)
		{
			Teardown();
			Destroy(mikanScene.gameObject);
		}

		private void Setup(MikanManager ownerManager)
		{
			// Cache references to common mikan objects
			OwnerManager = ownerManager;
			_client = ownerManager.MikanClient;
			_logger = _client.ClientAPI.CoreAPI.Logger;

			// Make sure all scene objects are on the UI layer
			// so they don't render in the XR camera
			gameObject.layer = LayerMask.NameToLayer("UI");

			// Bind client events
			BindClientEvents(_client);

			// Bind settings events
			BindSettingsEvents(ownerManager.OwnerPlugin.Settings);

			// Setup the scene manipulator collision
			SetupSceneManipulatorCollision();

			// Spawn the Mikan camera
			SpawnMikanCamera();

			// If we are already connected to Mikan,
			// tell the scene to fetch anchors, setup scene transform, etc
			// Otherwise wait for MikanManager to tell the scene about the connection
			if (_client.ClientAPI.GetIsConnected())
			{
				HandleMikanConnected();
			}
		}

		private void Teardown()
		{
			// Unbind settings events
			UnbindSettingsEvents(OwnerManager.OwnerPlugin.Settings);

			// Unbind client events
			UnbindClientEvents(_client);

			// Despawn the Mikan camera
			DespawnMikanCamera();

			// Forget cached references
			_logger = null;
			_client = null;
			OwnerManager = null;
		}

		private void BindClientEvents(MikanClient client)
		{
			client.OnConnected.AddListener(HandleMikanConnected);
			client.OnDisconnected.AddListener(HandleMikanDisconnected);
			client.OnRenderBufferCreated.AddListener(HandleRenderBufferCreated);
			client.OnRenderBufferDisposed.AddListener(HandleRenderBufferDisposed);
			client.OnAnchorListChanged.AddListener(HandleAnchorListChanged);
			client.OnAnchorPoseChanged.AddListener(HandleAnchorPoseChanged);
			client.OnQuadStencilListChanged.AddListener(HandleQuadStencilListChanged);
			client.OnBoxStencilListChanged.AddListener(HandleBoxStencilListChanged);
			client.OnModelStencilListChanged.AddListener(HandleModelStencilListChanged);
			client.OnStencilPoseChanged.AddListener(HandleStencilPoseChanged);
			client.OnNewFrameReceived.AddListener(HandleNewVideoSourceFrame);
		}

		private void UnbindClientEvents(MikanClient client)
		{
			client.OnConnected.RemoveListener(HandleMikanConnected);
			client.OnDisconnected.RemoveListener(HandleMikanDisconnected);
			client.OnRenderBufferCreated.RemoveListener(HandleRenderBufferCreated);
			client.OnRenderBufferDisposed.RemoveListener(HandleRenderBufferDisposed);
			client.OnAnchorListChanged.RemoveListener(HandleAnchorListChanged);
			client.OnAnchorPoseChanged.RemoveListener(HandleAnchorPoseChanged);
			client.OnQuadStencilListChanged.RemoveListener(HandleQuadStencilListChanged);
			client.OnBoxStencilListChanged.RemoveListener(HandleBoxStencilListChanged);
			client.OnModelStencilListChanged.RemoveListener(HandleModelStencilListChanged);
			client.OnStencilPoseChanged.RemoveListener(HandleStencilPoseChanged);
			client.OnNewFrameReceived.RemoveListener(HandleNewVideoSourceFrame);
		}

		private void BindSettingsEvents(MikanSettings settings)
		{
			settings.OnScenePositionChanged.AddListener(HandleScenePositionChanged);
			settings.OnSceneOrientationChanged.AddListener(HandleSceneOrientationChanged);
			settings.OnSceneScaleChanged.AddListener(HandleSceneScaleChanged);

			HandleScenePositionChanged(settings.ScenePosition);
			HandleSceneOrientationChanged(settings.SceneEulerAngles);
			HandleSceneScaleChanged(settings.SceneScale);
		}

		private void UnbindSettingsEvents(MikanSettings settings)
		{
			settings.OnScenePositionChanged.RemoveListener(HandleScenePositionChanged);
			settings.OnSceneOrientationChanged.RemoveListener(HandleSceneOrientationChanged);
			settings.OnSceneScaleChanged.RemoveListener(HandleSceneScaleChanged);
		}

		void SetupSceneManipulatorCollision()
		{
			BoxCollider sceneCollider = GetComponent<BoxCollider>();
			if (sceneCollider == null)
			{
				sceneCollider = gameObject.AddComponent<BoxCollider>();
			}
			sceneCollider.size = new Vector3(ManipulatorSize, ManipulatorSize, ManipulatorSize);

			TextMesh sceneText = GetComponent<TextMesh>();
			if (sceneText == null)
			{
				sceneText= gameObject.AddComponent<TextMesh>();
			}
			sceneText.text = "Mikan Scene";
			sceneText.characterSize = 0.1f;
			sceneText.anchor = TextAnchor.MiddleCenter;
			sceneText.alignment = TextAlignment.Center;
		}

		void SpawnMikanCamera()
		{
			if (_sceneCamera == null)
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Spawning Mikan camera");
				_sceneCamera= MikanCamera.SpawnCamera(this);
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Ignoring camera spawn request. Already spawned.");
			}
		}

		void DespawnMikanCamera()
		{
			if (_sceneCamera != null)
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Despawn Mikan camera");
				MikanCamera.DespawnCamera(_sceneCamera);
				_sceneCamera = null;
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Ignoring camera de-spawn request. Already despawned.");
			}
		}

		public MikanAnchor GetMikanAnchorById(MikanSpatialAnchorID anchorId)
		{
			if (_sceneAnchors.TryGetValue(anchorId, out MikanAnchor anchor))
			{
				return anchor;
			}

			return null;
		}

		public MikanAnchor GetMikanAnchorByName(string anchorName)
		{
			foreach (var kvp in _sceneAnchors)
			{
				MikanAnchor anchor = kvp.Value;

				if (anchor.AnchorName == anchorName)
				{
					return anchor;
				}
			}

			return null;
		}

		protected void HandleMikanConnected()
		{
			HandleCameraIntrinsicsChanged();
			HandleAnchorListChanged();
			HandleQuadStencilListChanged();
			HandleBoxStencilListChanged();
			HandleModelStencilListChanged();
		}

		protected void HandleMikanDisconnected()
		{
			HandleRenderBufferDisposed();
		}

		protected void HandleRenderBufferCreated()
		{
			if (_client.IsRenderTargetDescriptorValid)
			{
				if (_sceneCamera != null)
				{
					_sceneCamera.RecreateRenderTarget(_client.RenderTargetDescriptor);
				}
				else
				{
					_logger.Log(MikanLogLevel.Warning, "MikanScene: Missing expected scene camera.");
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Error, "MikanScene: Invalid render target descriptor!");
			}
		}

		protected void HandleRenderBufferDisposed()
		{
			if (_sceneCamera != null)
			{
				_sceneCamera.DisposeRenderTarget();
			}
		}

		public void HandleAnchorListChanged()
		{
			MikanResponse listResponse = _client.ClientAPI.SendRequest(new GetSpatialAnchorList()).FetchResponse();
			if (listResponse.resultCode == MikanAPIResult.Success)
			{
				var spatialAnchorListResponse = listResponse as MikanSpatialAnchorListResponse;
				var spatialAnchorList= spatialAnchorListResponse.spatial_anchor_id_list;

				// Destroy any anchors that are no longer in the scene
				foreach (var kvp in _sceneAnchors)
				{
					MikanSpatialAnchorID anchorID = kvp.Key;
					MikanAnchor anchor = kvp.Value;

					if (!spatialAnchorList.Contains(anchorID))
					{
						Destroy(anchor.gameObject);
					}
				}

				foreach (var listAnchorID in spatialAnchorList)
				{
					var requestSpatialAnchor= new GetSpatialAnchorInfo()
					{
						anchorId= listAnchorID
					};
					MikanResponse anchorResponse = 
						_client.ClientAPI.SendRequest(requestSpatialAnchor).FetchResponse();
					if (anchorResponse.resultCode == MikanAPIResult.Success)
					{
						var anchorInfoResponse = anchorResponse as MikanSpatialAnchorInfoResponse;

						if (_sceneAnchors.TryGetValue(listAnchorID, out MikanAnchor anchor))
						{
							anchor.ApplyAnchorInfo(anchorInfoResponse.anchor_info);
						}
						else
						{
							_sceneAnchors.Add(
								listAnchorID, 
								MikanAnchor.SpawnAnchor(this, anchorInfoResponse.anchor_info));
						}
					}
				}
			}
		}

		public void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			MikanAnchor anchor = GetMikanAnchorById(AnchorPoseEvent.anchor_id);
			if (anchor != null)
			{
				anchor.HandleAnchorPoseChanged(AnchorPoseEvent);
			}
		}

		public void HandleQuadStencilListChanged()
		{
			MikanResponse listResponse = _client.ClientAPI.SendRequest(new GetQuadStencilList()).FetchResponse();
			if (listResponse.resultCode == MikanAPIResult.Success)
			{
				var stencilListResponse = listResponse as MikanStencilListResponse;

				// Destroy any Quad Stencils that are no longer in the scene
				foreach (var kvp in _sceneQuadStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanQuadStencil Stencil = kvp.Value;

					if (!stencilListResponse.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Quad Stencils or spawn new ones
				foreach (var StencilID in stencilListResponse.stencil_id_list)
				{
					var requestStencilInfo = new GetQuadStencil()
					{
						stencilId = StencilID
					};
					MikanResponse StencilResponse = 
						_client.ClientAPI.SendRequest(requestStencilInfo).FetchResponse();
					if (StencilResponse.resultCode == MikanAPIResult.Success)
					{
						var modelStencilResponse = StencilResponse as MikanStencilQuadInfoResponse;

						if (_sceneQuadStencils.TryGetValue(StencilID, out MikanQuadStencil stencil))
						{
							stencil.ApplyStencilInfo(modelStencilResponse.quad_info);
						}
						else
						{
							_sceneQuadStencils.Add(
								StencilID, 
								MikanQuadStencil.SpawnStencil(this, modelStencilResponse.quad_info));
						}
					}
				}
			}
		}

		public void HandleBoxStencilListChanged()
		{
			MikanResponse listResponse = _client.ClientAPI.SendRequest(new GetBoxStencilList()).FetchResponse();
			if (listResponse.resultCode == MikanAPIResult.Success)
			{
				var stencilList = listResponse as MikanStencilListResponse;

				// Destroy any Box Stencils that are no longer in the scene
				foreach (var kvp in _sceneBoxStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanStencil Stencil = kvp.Value;

					if (!stencilList.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Box Stencils or spawn new ones
				foreach (var StencilID in stencilList.stencil_id_list)
				{
					var requestStencilInfo = new GetQuadStencil()
					{
						stencilId = StencilID
					};
					MikanResponse StencilResponse =
						_client.ClientAPI.SendRequest(requestStencilInfo).FetchResponse();
					if (StencilResponse.resultCode == MikanAPIResult.Success)
					{
						var StencilInfoResponse = StencilResponse as MikanStencilBoxInfoResponse;

						if (_sceneBoxStencils.TryGetValue(StencilID, out MikanBoxStencil stencil))
						{
							stencil.ApplyBoxStencilInfo(StencilInfoResponse.box_info);
						}
						else
						{
							_sceneBoxStencils.Add(
								StencilID, 
								MikanBoxStencil.SpawnStencil(this, StencilInfoResponse.box_info));
						}
					}
				}
			}
		}

		public void HandleModelStencilListChanged()
		{
			MikanResponse listResponse = _client.ClientAPI.SendRequest(new GetModelStencilList()).FetchResponse();
			if (listResponse.resultCode == MikanAPIResult.Success)
			{
				var stencilList = listResponse as MikanStencilListResponse;

				// Destroy any Model Stencils that are no longer in the scene
				foreach (var kvp in _sceneModelStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanStencil Stencil = kvp.Value;

					if (!stencilList.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Model Stencils or spawn new ones
				foreach (var StencilID in stencilList.stencil_id_list)
				{
					// Update or create core stencil object
					var requestStencilInfo = new GetModelStencil()
					{
						stencilId = StencilID
					};
					MikanResponse StencilResponse =
						_client.ClientAPI.SendRequest(requestStencilInfo).FetchResponse();
					if (StencilResponse.resultCode == MikanAPIResult.Success)
					{
						var StencilInfoResponse = StencilResponse as MikanStencilModelInfoResponse;

						if (_sceneModelStencils.TryGetValue(StencilID, out MikanModelStencil modelStencil))
						{
							// Update existing stencil info
							modelStencil.ApplyModelStencilInfo(StencilInfoResponse.model_info);
						}
						else
						{
							// Spawn in a new stencil
							modelStencil= MikanModelStencil.SpawnStencil(this, StencilInfoResponse.model_info);

							if (modelStencil != null)
							{
								_sceneModelStencils.Add(StencilID, modelStencil);

								// Fetch the meshes for the stencil
								var requestGeometry = new GetModelStencilRenderGeometry()
								{
									stencilId = StencilID
								};
								MikanResponse MeshResponse = 
									_client.ClientAPI.SendRequest(requestGeometry).FetchResponse();
								if (MeshResponse.resultCode == MikanAPIResult.Success)
								{
									var modelGeoResponse = MeshResponse as MikanStencilModelRenderGeometryResponse;

									modelStencil.ApplyModelRenderGeometry(modelGeoResponse.render_geometry);
								}
							}
						}
					}
				}
			}
		}

		public MikanStencil GetMikanStencilById(MikanStencilID stencilId)
		{
			if (_sceneModelStencils.TryGetValue(stencilId, out MikanModelStencil modelStencil))
			{
				return modelStencil;
			}

			if (_sceneBoxStencils.TryGetValue(stencilId, out MikanBoxStencil boxStencil))
			{
				return boxStencil;
			}

			if (_sceneQuadStencils.TryGetValue(stencilId, out MikanQuadStencil quadStencil))
			{
				return quadStencil;
			}

			return null;
		}

		public void HandleStencilPoseChanged(MikanStencilPoseUpdateEvent StencilPoseEvent)
		{
			MikanStencil Stencil = GetMikanStencilById(StencilPoseEvent.stencil_id);
			if (Stencil != null)
			{
				Stencil.HandleStencilPoseChanged(StencilPoseEvent);
			}
		}

		public void HandleCameraIntrinsicsChanged()
		{
			if (_sceneCamera != null)
			{
				_sceneCamera.HandleCameraIntrinsicsChanged();
			}
		}

		private void HandleScenePositionChanged(Vector3 newPosition)
		{
			this.transform.localPosition = newPosition;
		}

		private void HandleSceneOrientationChanged(Vector3 newOrientation)
		{
			this.transform.localRotation = Quaternion.Euler(newOrientation);
		}

		private void HandleSceneScaleChanged(float newScale)
		{
			this.transform.localScale = new Vector3(newScale, newScale, newScale);
		}

		public void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
		{
			if (_sceneCamera != null)
			{
				// Compute the camera transform in Mikan Space (but in Unity coordinate system)
				Vector3 sceneCameraPosition = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraPosition);
				Vector3 sceneCameraForward = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraForward);
				Vector3 sceneCameraUp = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraUp);
				Quaternion sceneCameraRotation = Quaternion.LookRotation(sceneCameraForward, sceneCameraUp);
				Matrix4x4 sceneCameraTransform = Matrix4x4.TRS(sceneCameraPosition, sceneCameraRotation, Vector3.one);

				// Update the camera's transform relative to the parent scene
				_sceneCamera.transform.localPosition =
					MikanMath.ExtractTranslationFromMatrix(sceneCameraTransform);
				_sceneCamera.transform.localRotation =
					MikanMath.ExtractRotationFromMatrix(sceneCameraTransform);

				// Render out a new frame
				_sceneCamera.CaptureFrame(newFrameEvent.frame);
			}
		}
	}
}