using MikanXR;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MikanXRPlugin
{
    public class MikanBehavior : MonoBehaviour
    {
        protected void Log(MikanLogLevel logLevel, string message)
        {
            MikanManager.Instance?.Log(logLevel, message);
        }
    }
}
