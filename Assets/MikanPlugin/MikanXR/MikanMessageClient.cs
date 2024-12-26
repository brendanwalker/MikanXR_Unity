using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using WebSocketSharp;

namespace MikanXR
{
	public class MikanMessageClient : IDisposable
	{
		private static readonly string WEBSOCKET_PROTOCOL_PREFIX = "Mikan-";

		private MikanLogger _logger= null;

		private WebSocket _wsConnection;
		private int _protocolVersion= 0;

		class WebsocketEvent
		{
			public enum EventType
			{
				Connect,
				Disconnect,
				Message,
				Error
			};
			public EventType eventType;
			public string payload;
		}
		private Queue<WebsocketEvent> _websocketThreadEventQueue= null;
		private Queue<WebsocketEvent> _mainThreadEventQueue= null;
		private Queue<string> _mainThreadMessageQueue= null;

		public bool IsConnected
		{
			get
			{
				return (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open);
			}
		}

		public delegate bool TextResponseHandler(string responseString, out string errorMesg);
		private TextResponseHandler _textResponseHandler = null;
		public void SetTextResponseHandler(TextResponseHandler handler)
		{
			_textResponseHandler = handler;
		}

		public delegate bool BinaryResponseHandler(byte[] responseData, out string errorMesg);
		private BinaryResponseHandler _binaryResponseHandler = null;
		public void SetBinaryResponseHandler(BinaryResponseHandler handler)
		{
			_binaryResponseHandler = handler;
		}

		public MikanMessageClient(MikanLogger logger)
		{
			_logger = logger;
			_websocketThreadEventQueue = new Queue<WebsocketEvent>();
			_mainThreadEventQueue = new Queue<WebsocketEvent>();
			_mainThreadMessageQueue = new Queue<string>();
			_protocolVersion = (int)MikanConstants.ClientAPIVersion;
		}

		public MikanAPIResult Initialize()
		{
			return MikanAPIResult.Success;
		}

		public void Dispose()
		{
			Disconnect();
		}

		public MikanAPIResult Connect(string host, string port)
		{
			if (_wsConnection != null && _wsConnection.ReadyState == WebSocketSharp.WebSocketState.Open)
			{
				return MikanAPIResult.Success;
			}

			string url = $"{host}:{port}";
			string protocol = $"{WEBSOCKET_PROTOCOL_PREFIX}{_protocolVersion}";
			_wsConnection = new WebSocket(url, protocol);
			_wsConnection.Log.Level = LogLevel.Info;
			_wsConnection.Log.Output = (data, output) =>
			{
				switch (data.Level)
				{
					case LogLevel.Fatal:
					case LogLevel.Error:
						_logger.Log(MikanLogLevel.Error, data.Message);
						break;
					case LogLevel.Warn:
						_logger.Log(MikanLogLevel.Warning, data.Message);
						break;
					case LogLevel.Info:
						_logger.Log(MikanLogLevel.Info, data.Message);
						break;
					case LogLevel.Debug:
						_logger.Log(MikanLogLevel.Debug, data.Message);
						break;
					case LogLevel.Trace:
						_logger.Log(MikanLogLevel.Trace, data.Message);
						break;
				}
			};

			_wsConnection.OnOpen += EnqueueWebsocketConnectEvent;
			_wsConnection.OnMessage += EnqueueWebsocketMessageEvent;
			_wsConnection.OnError += EnqueueWebsocketErrorEvent;
			_wsConnection.OnClose += EnqueueWebsocketDisconnectEvent;

			_wsConnection.ConnectAsync();

			return MikanAPIResult.Success;
		}

		public void Disconnect(ushort code= 0, string reason= "")
		{
			if (_wsConnection != null)
			{
				// Attempt to both close and dispose the existing connection
				try
				{
					if (code != 0)
					{
						_wsConnection.Close(code, reason);
					}
					else
					{
						_wsConnection.Close(CloseStatusCode.Normal, "User requested disconnect");
					}

					((IDisposable)_wsConnection).Dispose();
				}
				catch (Exception ex)
				{ 
					_logger.Log(MikanLogLevel.Error, $"Error closing websocket connection: {ex.Message}");
				}

				_wsConnection = null;
			}
		}

		private void EnqueueWebsocketConnectEvent(object sender, EventArgs e)
		{
			lock (_websocketThreadEventQueue)
			{
				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Connect,
					payload = string.Empty
				});
			}
		}

		private void EnqueueWebsocketDisconnectEvent(object sender, CloseEventArgs d)
		{
			lock (_websocketThreadEventQueue)
			{
				ushort code = d?.Code ?? 0;
				string reason = d?.Reason ?? "<NONE>";

				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Disconnect,
					payload = $"{MikanEventManager.WEBSOCKET_DISCONNECT_EVENT}:{code}:{reason}"
				});
			}
		}

		private void EnqueueWebsocketErrorEvent(object sender, ErrorEventArgs e)
		{
			EnqueueWebsocketErrorMessage(e.Message);
		}

		private void EnqueueWebsocketErrorMessage(string errorMessage)
		{
			lock (_websocketThreadEventQueue)
			{
				_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
				{
					eventType = WebsocketEvent.EventType.Error,
					payload = errorMessage
				});
			}
		}

		private void EnqueueWebsocketMessageEvent(object sender, MessageEventArgs e)
		{
			if (e.IsText)
			{
				string message = e.Data;
				var root = (JObject)JsonConvert.DeserializeObject(message);

				if (root.TryGetValue("eventTypeId", out JToken eventTypeElement) &&
					eventTypeElement.Type == JTokenType.Integer)
				{
					lock (_websocketThreadEventQueue)
					{
						_websocketThreadEventQueue.Enqueue(new WebsocketEvent()
						{
							eventType = WebsocketEvent.EventType.Message,
							payload = message
						});
					}
				}
				else if (root.TryGetValue("responseTypeId", out JToken responseTypeElement) &&
						responseTypeElement.Type == JTokenType.Integer)
				{
					if (_textResponseHandler != null)
					{
						if (!_textResponseHandler(message, out string errorMesg))
						{
							EnqueueWebsocketErrorMessage(errorMesg);
						}
					}
					else
					{
						EnqueueWebsocketErrorMessage($"Received response message but no handler set: {message}");
					}
				}
				else
				{
					EnqueueWebsocketErrorMessage($"Received unsupported message: {message}");
				}
			}
			else if (e.IsBinary)
			{
				byte[] bytes = e.RawData;

				if (bytes != null && bytes.Length > 0)
				{
					if (_binaryResponseHandler != null)
					{
						if (!_binaryResponseHandler(bytes, out string errorMesg))
						{
							EnqueueWebsocketErrorMessage(errorMesg);
						}
					}
					else
					{
						EnqueueWebsocketErrorMessage($"Received binary response message but no handler set");
					}
				}
				else
				{
					EnqueueWebsocketErrorMessage("Received empty binary response");
				}
			}
			else
			{
				EnqueueWebsocketErrorMessage("Received unsupported message type");
			}
		}

		public MikanAPIResult FetchNextEvent(out string message)
		{
			// Copy events from the websocket thread to the main thread
			lock (_websocketThreadEventQueue)
			{
				while (_websocketThreadEventQueue.Count > 0)
				{
					_mainThreadEventQueue.Enqueue(_websocketThreadEventQueue.Dequeue());
				}
			}

			// Process the main thread events
			while (_mainThreadEventQueue.Count > 0)
			{
				WebsocketEvent websocketEvent = _mainThreadEventQueue.Dequeue();

				switch(websocketEvent.eventType)
				{
					case WebsocketEvent.EventType.Connect:
						HandleConnectEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Disconnect:
						HandleDisconnectEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Message:
						HandleMessageEvent(websocketEvent);
						break;
					case WebsocketEvent.EventType.Error:
						HandleErrorEvent(websocketEvent);
						break;
				}
			}

			// Return the next message event in the queue
			if (_mainThreadMessageQueue.Count > 0)
			{
				message = _mainThreadMessageQueue.Dequeue();
				return MikanAPIResult.Success;
			}

			message = string.Empty;
			return MikanAPIResult.NoData;
		}

		private void HandleConnectEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, "Connected to web socket server");
		}

		private void HandleDisconnectEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"Disconnected from web socket server: {websocketEvent.payload}");
			_mainThreadMessageQueue.Enqueue(websocketEvent.payload);
		}

		private void HandleMessageEvent(WebsocketEvent websocketEvent)
		{
			_mainThreadMessageQueue.Enqueue(websocketEvent.payload);
		}

		private void HandleErrorEvent(WebsocketEvent websocketEvent)
		{
			_logger.Log(MikanLogLevel.Info, $"Web socket Error: {websocketEvent.payload}");
		}

		public MikanAPIResult SendRequest(string message)
		{
			if (IsConnected)
			{
				_wsConnection.Send(message);

				return MikanAPIResult.Success;
			}
			
			return MikanAPIResult.NotConnected;
		}
	}
}
