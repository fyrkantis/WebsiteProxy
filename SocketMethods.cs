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

		public static void SendBody(this Socket socket, string body, ResponseHeaders? responseHeadersDefault = null)
		{
			ResponseHeaders responseHeaders;
			if (responseHeadersDefault != null)
			{
				responseHeaders = responseHeadersDefault;
			}
			else
			{
				responseHeaders = new ResponseHeaders();
			}
			byte[] bytes = Encoding.UTF8.GetBytes(body);
			responseHeaders.headers.Add("Content-Type", "text/html; charset=utf-8");
			responseHeaders.headers.Add("Content-Length", bytes.Length);

			MyConsole.WriteHttpStatus(responseHeaders);
			socket.Send(responseHeaders.GetBytes());
			socket.Send(bytes);
			socket.Close();
		}
		public static void SendHeaders(this Socket socket, ResponseHeaders responseHeaders)
		{
			MyConsole.WriteHttpStatus(responseHeaders);
			socket.Send(responseHeaders.GetBytes());
			socket.Close();
		}
		public static void SendResponse(this Socket socket, int code, string? additionalInfo = null)
		{
			socket.SendHeaders(new ResponseHeaders(code));
		}
		public static void SendRedirect(this Socket socket, int code, string path)
		{
			socket.SendHeaders(new ResponseHeaders(code, new Dictionary<string, string>() { { "Location", path } }));
		}
	}
}
