using MikanXR;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MikanXRPlugin
{
    public class MikanBehavior : MonoBehaviour
    {
        public MikanClient Client => MikanManager.Instance.MikanClient;
        public MikanAPI ClientAPI => Client.ClientAPI;

        protected void Log(MikanLogLevel logLevel, string message)
        {
            MikanManager.Instance?.Log(logLevel, message);
        }
    }
}
