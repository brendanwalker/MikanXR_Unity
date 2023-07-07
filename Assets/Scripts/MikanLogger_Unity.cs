using Mikan;
using UnityEngine;

namespace MikanXR
{
	public class MikanLogger_Unity : MonoBehaviour, IMikanLogger
	{
		public void Log(MikanLogLevel mikanLogLevel, string log_message)
		{
			switch (mikanLogLevel)
			{
			case MikanLogLevel.Trace:
				UnityEngine.Debug.Log($"Trace | {log_message}");
				break;
			case MikanLogLevel.Debug:
				UnityEngine.Debug.Log($"DEBUG | {log_message}");
				break;
			case MikanLogLevel.Info:
				UnityEngine.Debug.Log($"INFO | {log_message}");
				break;
			case MikanLogLevel.Warning:
				UnityEngine.Debug.LogWarning($"WARNING | {log_message}");
				break;
			case MikanLogLevel.Error:
				UnityEngine.Debug.LogError($"ERROR | {log_message}");
				break;
			case MikanLogLevel.Fatal:
				UnityEngine.Debug.LogError($"FATAL | {log_message}");
				break;
			}
		}
	}
}
