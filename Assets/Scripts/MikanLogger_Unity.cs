using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
    public class MikanLogger_Unity : MonoBehaviour, IMikanLogger
    {
        public void Log(MikanLogLevel mikanLogLevel, string log_message)
        {
            switch (mikanLogLevel)
            {
                case MikanLogLevel.Trace:
                    Debug.Log($"Trace | {log_message}");
                    break;
                case MikanLogLevel.Debug:
                    Debug.Log($"DEBUG | {log_message}");
                    break;
                case MikanLogLevel.Info:
                    Debug.Log($"INFO | {log_message}");
                    break;
                case MikanLogLevel.Warning:
                    Debug.LogWarning($"WARNING | {log_message}");
                    break;
                case MikanLogLevel.Error:
                    Debug.LogError($"ERROR | {log_message}");
                    break;
                case MikanLogLevel.Fatal:
                    Debug.LogError($"FATAL | {log_message}");
                    break;
            }
        }
    }
}
