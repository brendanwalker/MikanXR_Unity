using MikanXR;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MikanXRPlugin
{
	public class MikanManager : MonoBehaviour
	{
		public static MikanManager Instance => _instance;
		private static MikanManager _instance = null;

		private MikanClient _mikanClient = null;
		public MikanClient MikanClient => _mikanClient;

		private GameObject _mikanSceneObject = null;
		private MikanScene _mikanScene = null;
		public MikanScene MikanScene => _mikanScene;

		void Awake()
		{
			// For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
			//   and destroy any that are created while one already exists.
			if (_instance != null)
			{
				Log(MikanLogLevel.Warning, $"MikanManager: Instance of {GetType().Name} already exists, destroying.");
				DestroyImmediate(this);
				return;
			}
			_instance = this;
			DontDestroyOnLoad(this); // Don't destroy this object on scene changes

			// If there is a unity logger scipt attached, use that by default
			var defaultLogger = this.gameObject.AddComponent<MikanLogger_Unity>();
			SetLogHandler(defaultLogger);
			Log(MikanLogLevel.Debug, $"MikanManager: {name}: Awake()");

			// Create a mikan client sibling component to manage client API
			_mikanClient = this.gameObject.AddComponent<MikanClient>();

			// Listen for when the scene changes
			SceneManager.activeSceneChanged += OnSceneChanged;

			// If there is an active scene, spawn the Mikan Scene
			if (SceneManager.GetActiveScene().isLoaded)
			{
				SpawnMikanScene();
			}
		}

		void OnEnable()
		{
			Log(MikanLogLevel.Info, $"MikanManager: OnEnable Called");
			_mikanClient.InitClient();
		}

		void OnDisable()
		{
			Log(MikanLogLevel.Info, $"MikanManager: OnDisable Called");
			_mikanClient.DisposeClient();
		}

		void OnApplicationQuit()
		{
			Log(MikanLogLevel.Info, $"MikanManager: OnApplicationQuit Called");
			_mikanClient.DisposeClient();
		}

		private IMikanLogger _logHandler = null;
		public void SetLogHandler(IMikanLogger logHandler)
		{
			_logHandler = logHandler;
		}

		public void Log(MikanLogLevel log_level, string log_message)
		{
			if (_logHandler != null)
			{
				_logHandler.Log(log_level, log_message);
			}
		}

		private void OnSceneChanged(Scene oldScene, Scene newScene)
		{
			Log(MikanLogLevel.Info,
				$"MikanManager: Active scene changed: {oldScene.name} -> {newScene.name}");

			DespawnMikanScene();
			SpawnMikanScene();
		}

		void DespawnMikanScene()
		{
			Log(MikanLogLevel.Info, "MikanManager: Unloading MikanXR Scene");

			if (_mikanSceneObject != null)
			{
				UnbindMikanScene(_mikanScene);
				Destroy(_mikanSceneObject);
				_mikanSceneObject = null;
			}
		}

		void SpawnMikanScene()
		{
			if (_mikanSceneObject == null)
			{
				Log(MikanLogLevel.Warning, "MikanManager: Spawning Mikan XR Scene");
				_mikanSceneObject = new GameObject(
					"MikanSceneObject",
					new System.Type[] {
						typeof(MikanScene)
					});

				MikanScene sceneComponent = _mikanSceneObject.GetComponent<MikanScene>();

				// Bind the scene to the MikanManager
				BindMikanScene(sceneComponent);
			}
			else
			{
				Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Ignoring Spawn Mikan XR Scene request. Already spawned");
			}
		}

		public void BindMikanScene(MikanScene InScene)
		{
			_mikanScene = InScene;
			Log(MikanLogLevel.Info, $"MikanManager: Binding Mikan Scene");
		}

		public void UnbindMikanScene(MikanScene InScene)
		{
			if (_mikanScene == InScene)
			{
				Log(MikanLogLevel.Info, $"MikanManager: Unbinding Mikan Scene");
				_mikanScene = InScene;
			}
			else
			{
				Log(MikanLogLevel.Warning, $"MikanManager: Trying to unbind incorrect scene.");
			}
		}
	}
}