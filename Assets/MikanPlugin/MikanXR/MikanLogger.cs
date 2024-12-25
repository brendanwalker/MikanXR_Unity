using MikanXR;
using System;
using System.Collections.Generic;

namespace MikanXR
{
	public delegate void MikanLogCallback(MikanLogLevel log_level, string log_message);

	public struct LoggerSettings
	{
		public MikanLogLevel min_log_level;
		public string log_filename;
		public bool enable_console;
		public MikanLogCallback log_callback;
	};

	public class MikanLogger : IDisposable
	{
		private LoggerSettings _settings;
		public LoggerSettings Settings => _settings;

		private List<MikanLogCallback> _logCallbacks = new List<MikanLogCallback>();

		public MikanLogger()
		{
			// Initialize the logger with default settings
		}

		public void Init(LoggerSettings settings)
		{
			// Initialize the logger with the given settings
			_settings= settings;
		}

		public void Dispose()
		{
			// Dispose the logger
		}

		public void AddLogCallback(MikanLogCallback callback)
		{
			_logCallbacks.Add(callback);
		}

		public void RemoveLogCallback(MikanLogCallback callback)
		{
			_logCallbacks.Remove(callback);
		}

		public void Log(MikanLogLevel log_level, string message)
		{
			if (log_level >= _settings.min_log_level)
			{
				foreach (var callback in _logCallbacks)
				{
					if (callback != null)
					{
						callback(log_level, message);
					}
				}
			}
		}
	}
}
