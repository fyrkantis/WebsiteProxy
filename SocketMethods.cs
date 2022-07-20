using System.Net.Sockets;
using System.Text;

namespace WebsiteProxy
{
	public static class SocketMethods
	{
		// https://stackoverflow.com/a/722265/13347795
		public static bool IsConnected(this Socket socket)
		{
			try
			{
				return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
			}
			catch (SocketException)
			{
				return false;
			}
		}

		public static string? ReadPost(this Socket socket, RequestHeaders requestHeaders)
		{
			if (!requestHeaders.headers.ContainsKey("Content-Length")
				|| !int.TryParse(requestHeaders.headers["Content-Length"].ToString(), out int length))
			{
				return null;
			}
			byte[]? bytes = socket.ReceiveBytes(length);
			if (bytes == null)
			{
				return null;
			}
			return Encoding.UTF8.GetString(bytes);
		}

		public static byte[]? ReceiveBytes(this Socket socket, int bytesLength)
		{
			List<byte> bytesList = new List<byte>();
			while (true)
			{
				int bytesLeft = bytesLength - bytesList.Count;
				if (!socket.IsConnected() || bytesLeft <= 0)
				{
					break;
				}
				byte[] bytesBuffer = new byte[bytesLeft];
				int bufferLength = socket.Receive(bytesBuffer);
				if (bufferLength <= 0)
				{
					break;
				}
				bytesList.AddRange(bytesBuffer);
			}
			if (bytesList.Count > 0)
			{
				return bytesList.ToArray();
			}
			return null;
		}

		public static void SendFileResponse(this Socket socket, string path, Log? log = null)
		{
			socket.SendFileResponse(path, new ResponseHeaders(), log);
		}
		public static void SendFileResponse(this Socket socket, string path, ResponseHeaders responseHeaders, Log? log = null)
		{
			responseHeaders.SetHashFile(path);

			FileInfo fileInfo = new FileInfo(path);
			responseHeaders.Add("Content-Disposition", "inline; filename=\"" + fileInfo.Name + "\"");
			responseHeaders.Add("Content-Type", MimeTypes.GetMimeType(fileInfo.Extension) + "; charset=utf-8");
			responseHeaders.Add("Content-Length", fileInfo.Length);

			if (log != null)
			{
				log.Add(responseHeaders);
			}
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
				socket.SendFile(path);
			}, log);
		}
		public static void TrySend(this Socket socket, Action sendFunction, Log? log = null)
		{
			if (!socket.IsConnected())
			{
				if (log != null)
				{
					log.Add("(Aborted)", LogColor.Error);
					log.Write();
				}
				return;
			}
			try
			{
				sendFunction.Invoke();
				socket.Close();
				if (log != null)
				{
					log.Add("(Sent)", LogColor.Success);
					log.Write();
				}
			}
			catch(SocketException exception)
			{
				if (log != null)
				{
					log.AddRange(LogColor.Error, "(Exception: " + exception.ErrorCode + ")");
					log.secondRow = new LogPart(exception.Message, LogColor.Error);
				}
			}
		}

		public static void SendPageResponse(this Socket socket, string path, Dictionary<string, object>? parameters = null, Log? log = null)
		{
			socket.SendPageResponse(path, new ResponseHeaders(), parameters, log);
		}
		public static void SendPageResponse(this Socket socket, string path, ResponseHeaders responseHeaders, Dictionary<string, object>? parameters = null, Log? log = null)
		{
			responseHeaders.Add("Content-Disposition", "inline; filename=\"" + Path.GetFileName(path) + "\"");
			socket.SendBodyResponse(TemplateLoader.Render(path, parameters, log), responseHeaders, log);
		}

		public static void SendBodyResponse(this Socket socket, string body, Log? log = null)
		{
			socket.SendBodyResponse(body, new ResponseHeaders(), log);
		}
		public static void SendBodyResponse(this Socket socket, string body, ResponseHeaders responseHeaders, Log? log = null)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			responseHeaders.SetHash(bytes);
			responseHeaders.Add("Content-Type", "text/html; charset=utf-8");
			responseHeaders.Add("Content-Length", bytes.Length);

			if (log != null)
			{
				log.Add(responseHeaders);
			}
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
				socket.Send(bytes);
			}, log);
		}

		public static void SendError(this Socket socket, int code, object? additionalInfo = null, Dictionary<string, object>? headerFields = null, Log? log = null)
		{
			socket.SendError(code, new ResponseHeaders(code, headerFields), additionalInfo, log);
		}
		public static void SendError(this Socket socket, int code, ResponseHeaders responseHeaders, object? additionalInfo = null, Log? log = null)
		{
			Dictionary<string, object> parameters = new Dictionary<string, object>()
			{
				{ "navbarButtons", Util.navbarButtons },
				{ "code", code },
				{ "message", responseHeaders.message }
			};
			if (additionalInfo != null)
			{
				parameters.Add("errors", additionalInfo);
				if (log != null)
				{
					log.secondRow = new LogPart(additionalInfo, LogColor.Error);
				}
			}
			socket.SendPageResponse(Path.Combine(Util.currentDirectory, "pages", "error.html"), responseHeaders, parameters, log);
		}

		public static void SendResponse(this Socket socket, int code, Dictionary<string, object>? headerFields = null, Log? log = null)
		{
			socket.SendResponse(new ResponseHeaders(code, headerFields), log);
		}
		public static void SendResponse(this Socket socket, ResponseHeaders responseHeaders, Log? log = null)
		{
			if (log != null)
			{
				log.Add(responseHeaders);
			}
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
			}, log);
		}
		public static void SendRedirectResponse(this Socket socket, int code, string path, Log? log = null)
		{
			socket.SendResponse(code, new Dictionary<string, object>() { { "Location", path } }, log);
		}
	}
}
