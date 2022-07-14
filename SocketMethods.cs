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
			socket.Send(responseHeaders.GetBytes());
			socket.SendFile(path);
			socket.Close();
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
			socket.Send(responseHeaders.GetBytes());
			socket.Send(bytes);
			socket.Close();
		}

		public static void SendResponse(this Socket socket, int code, string? additionalInfo = null, Dictionary<string, object>? headerFields = null)
		{
			ResponseHeaders responseHeaders = new ResponseHeaders(code, headerFields);
			MyConsole.WriteHttpStatus(responseHeaders);
			socket.Send(responseHeaders.GetBytes());
			socket.Close();
		}
		public static void SendRedirectResponse(this Socket socket, int code, string path)
		{
			ResponseHeaders responseHeaders = new ResponseHeaders(code, new Dictionary<string, object>() { { "Location", path } });
			MyConsole.WriteHttpStatus(responseHeaders);
			socket.Send(responseHeaders.GetBytes());
			socket.Close();
		}
	}
}
