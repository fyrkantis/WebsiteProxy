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

		public static void SendFileResponse(this Socket socket, string path)
		{
			socket.SendFileResponse(path, new ResponseHeaders());
		}
		public static void SendFileResponse(this Socket socket, string path, ResponseHeaders responseHeaders)
		{
			responseHeaders.SetHashFile(path);

			FileInfo fileInfo = new FileInfo(path);
			responseHeaders.headers.Add("Content-Disposition", "inline; filename=\"" + fileInfo.Name + "\"");
			responseHeaders.headers.Add("Content-Type", MimeTypes.GetMimeType(fileInfo.Extension) + "; charset=utf-8");
			responseHeaders.headers.Add("Content-Length", fileInfo.Length);

			MyConsole.WriteHttpStatus(responseHeaders);
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
				socket.SendFile(path);
			});
		}
		public static void TrySend(this Socket socket, Action sendFunction)
		{
			if (!socket.IsConnected())
			{
				MyConsole.color = ConsoleColor.Red;
				MyConsole.Write(" (Aborted).");
				return;
			}
			try
			{
				sendFunction.Invoke();
				socket.Close();
				MyConsole.color = ConsoleColor.Green;
				MyConsole.Write(" (Sent).");
			}
			catch(SocketException exception)
			{
				MyConsole.color = ConsoleColor.Red;
				MyConsole.Write(" (Exception: " + exception.ErrorCode + ").");
				MyConsole.WriteLine(exception.Message);
			}
		}

		public static void SendPageResponse(this Socket socket, string path, Dictionary<string, object>? parameters = null)
		{
			socket.SendPageResponse(path, new ResponseHeaders(), parameters);
		}
		public static void SendPageResponse(this Socket socket, string path, ResponseHeaders responseHeaders, Dictionary<string, object>? parameters = null)
		{
			responseHeaders.headers.Add("Content-Disposition", "inline; filename=\"" + Path.GetFileName(path) + "\"");
			socket.SendBodyResponse(TemplateLoader.Render(path, parameters), responseHeaders);
		}

		public static void SendBodyResponse(this Socket socket, string body)
		{
			socket.SendBodyResponse(body, new ResponseHeaders());
		}
		public static void SendBodyResponse(this Socket socket, string body, ResponseHeaders responseHeaders)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			responseHeaders.SetHash(bytes);
			responseHeaders.headers.Add("Content-Type", "text/html; charset=utf-8");
			responseHeaders.headers.Add("Content-Length", bytes.Length);

			MyConsole.WriteHttpStatus(responseHeaders);
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
				socket.Send(bytes);
			});
		}

		public static void SendError(this Socket socket, int code, object? additionalInfo = null, Dictionary<string, object>? headerFields = null)
		{
			socket.SendError(code, new ResponseHeaders(code, headerFields), additionalInfo);
		}
		public static void SendError(this Socket socket, int code, ResponseHeaders responseHeaders, object? additionalInfo = null)
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
			}
			socket.SendPageResponse(Path.Combine(Util.currentDirectory, "pages\\error.html"), responseHeaders, parameters);
		}

		public static void SendResponse(this Socket socket, int code, Dictionary<string, object>? headerFields = null)
		{
			socket.SendResponse(new ResponseHeaders(code, headerFields));
		}
		public static void SendResponse(this Socket socket, ResponseHeaders responseHeaders)
		{
			MyConsole.WriteHttpStatus(responseHeaders);
			socket.TrySend(() =>
			{
				socket.Send(responseHeaders.GetBytes());
			});
		}
		public static void SendRedirectResponse(this Socket socket, int code, string path)
		{
			socket.SendResponse(code, new Dictionary<string, object>() { { "Location", path } });
		}
	}
}
