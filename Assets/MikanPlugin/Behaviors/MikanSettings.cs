using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MikanXR
{
	// Base class for Mikan settings
	// Plugins in other apps extend this class to save and load settings
	public class MikanSettings
	{
		public UnityEvent<float> OnCameraPositionScaleChanged = new UnityEvent<float>();
		protected float _cameraPositionScale = 1.0f;
		public float CameraPositionScale
		{
			get { return _cameraPositionScale; }
			set
			{
				if (value != _cameraPositionScale)
				{
					_cameraPositionScale = Math.Max(value, 0.001f);
					OnCameraPositionScaleChanged.Invoke(_cameraPositionScale);
				}
			}
		}

		public UnityEvent<Vector3> OnScenePositionChanged = new UnityEvent<Vector3>();
		protected Vector3 _scenePosition = Vector3.zero;
		public Vector3 ScenePosition
		{
			get { return _scenePosition; }
			set { 
				_scenePosition = value; 
				OnScenePositionChanged.Invoke(_scenePosition);
			}
		}

		public UnityEvent<Vector3> OnSceneOrientationChanged = new UnityEvent<Vector3>();
		protected Vector3 _sceneEulerAngles = Vector3.zero;
		public Vector3 SceneEulerAngles
		{
			get { return _sceneEulerAngles; }
			set { 
				_sceneEulerAngles = value; 
				OnSceneOrientationChanged.Invoke(_sceneEulerAngles);
			}
		}

		public UnityEvent<float> OnSceneScaleChanged = new UnityEvent<float>();
		protected float _sceneScale = 1.0f;
		public float SceneScale
		{
			get { return _sceneScale; }
			set { 
				_sceneScale = value; 
				OnSceneScaleChanged.Invoke(_sceneScale);
			}
		}

		public virtual bool LoadSettings() 
		{ 
			return false;
		}

		public virtual bool SaveSettings() 
		{ 
			return false;
		}

		protected virtual void DeserializeSettings(Dictionary<string, string> settings)
		{
			_cameraPositionScale= DeserializeFloat(settings, "cameraPositionScale", 1f);
			_scenePosition = DeserializeVector3(settings, "scenePosition", Vector3.zero);
			_sceneEulerAngles = DeserializeVector3(settings, "sceneEulerAngles", Vector3.zero);
			_sceneScale = DeserializeFloat(settings, "sceneScale", 1f);
		}

		protected virtual void SerializeSettings(Dictionary<string, string> settings)
		{
			SerializeFloat(settings, "cameraPositionScale", _cameraPositionScale);
			SerializeVector3(settings, "scenePosition", _scenePosition);
			SerializeVector3(settings, "sceneEulerAngles", _sceneEulerAngles);
			SerializeFloat(settings, "sceneScale", _sceneScale);
		}

		protected float DeserializeFloat(Dictionary<string, string> settings, string key, float defaultValue)
		{
			if (settings.TryGetValue(key, out string value))
			{
				if (float.TryParse(value, out float result))
				{
					return result;
				}
			}

			return defaultValue;
		}

		protected void SerializeFloat(Dictionary<string, string> settings, string key, float value)
		{
			settings[key] = value.ToString();
		}

		protected Vector3 DeserializeVector3(Dictionary<string, string> settings, string key, Vector3 defaultValue)
		{
			if (settings.TryGetValue(key, out string value))
			{
				string[] stringValues = value.Split(',');

				if (stringValues.Length == 3)
				{
					if (float.TryParse(stringValues[0], out float x) &&
						float.TryParse(stringValues[1], out float y) &&
						float.TryParse(stringValues[2], out float z))
					{
						return new Vector3(x, y, z);
					}
				}
			}

			return defaultValue;
		}

		protected void SerializeVector3(Dictionary<string, string> settings, string key, Vector3 value)
		{
			settings[key] = $"{value.x},{value.y},{value.z}";
		}
	}
}
