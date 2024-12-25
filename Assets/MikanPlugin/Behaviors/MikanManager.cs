using UnityEngine;
using UnityEngine.SceneManagement;

namespace MikanXR
{
	public class MikanManager : MonoBehaviour
	{
		public MikanXRPlugin OwnerPlugin { get; private set; }

		private MikanClient _mikanClient = null;
		public MikanClient MikanClient => _mikanClient;

		private MikanLogger _logger = null;

		private MikanScene _mikanScene = null;
		public MikanScene MikanScene => _mikanScene;

		public void Awake()
		{
			// Don't destroy this object on scene changes
			DontDestroyOnLoad(this);
		}

		public void Setup(MikanXRPlugin ownerPlugin)
		{
			OwnerPlugin = ownerPlugin;

			// Create a mikan client sibling component to manage client API
			_mikanClient = gameObject.AddComponent<MikanClient>();
			_mikanClient.Setup(this);

			// Cache a reference to the logger
			_logger = _mikanClient.ClientAPI.CoreAPI.Logger;

			// Listen for when the scene changes
			SceneManager.activeSceneChanged += OnUnitySceneChanged;

			// If there is an active scene, spawn the Mikan Scene
			if (SceneManager.GetActiveScene().isLoaded)
			{
				BindMikanScene(MikanScene.SpawnScene(this));
			}
		}

		public void TearDown()
		{
			if (_mikanClient != null)
			{
				_mikanClient.TearDown();
				_mikanClient = null;
			}

			_logger= null;

			// Stop listening for scene changes
			SceneManager.activeSceneChanged -= OnUnitySceneChanged;
		}

		private void OnUnitySceneChanged(Scene oldScene, Scene newScene)
		{
			_logger.Log(MikanLogLevel.Info,
				$"MikanManager: Active scene changed: {oldScene.name} -> {newScene.name}");

			if (_mikanScene != null)
			{
				UnbindMikanScene(_mikanScene);
				MikanScene.DespawnScene(_mikanScene);
			}

			BindMikanScene(MikanScene.SpawnScene(this));
		}

		public void BindMikanScene(MikanScene InScene)
		{
			_mikanScene = InScene;
			_logger.Log(MikanLogLevel.Info, $"MikanManager: Binding Mikan Scene");
		}

		public void UnbindMikanScene(MikanScene InScene)
		{
			if (_mikanScene == InScene)
			{
				_logger.Log(MikanLogLevel.Info, $"MikanManager: Unbinding Mikan Scene");
				_mikanScene = InScene;
			}
		}
	}
}