using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WebsiteProxy
{
	public class Headers
	{
		public static Dictionary<int, string> messages = new Dictionary<int, string>()
		{
			{ 200, "OK" }, { 204, "No Content" },
			{ 400, "Bad Request" }, { 404, "Not Found" }, { 405, "Method Not Allowed" },
			{ 300, "Multiple Choices" }, { 301, "Moved Permanently" }, { 303, "See Other"},
			{ 500, "Internal Server Error" }, { 504, "Gateway Timeout"}
		};

		const int bufferSize = 1;
		public string? protocol;
		// https://stackoverflow.com/a/13230450
		public Dictionary<string, object> headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		public static byte[]? ReadSocketToNewline(Socket socket)
		{
			byte[] bytesBuffer = new byte[bufferSize];
			List<byte> bytesList = new List<byte>();

			while (true)
			{
				int bufferLength = socket.Receive(bytesBuffer, 0, bufferSize, SocketFlags.None, out SocketError error);
				if (error != SocketError.Success)
				{
					MyConsole.color = ConsoleColor.DarkYellow;
					MyConsole.Write(" Header error: " + error);
					socket.SendError(400, "Connection timed out.");
					return null;
				}
				bytesList.AddRange(bytesBuffer);
				string buffer = Encoding.ASCII.GetString(bytesBuffer);
				//MyConsole.Write(buffer);
				if (bufferLength <= 0 || buffer[0] == '\n')
				{
					return bytesList.ToArray();
				}
			}
		}

		public static byte[] ReadStreamToNewline(Stream stream)
		{
			byte[] bytesBuffer = new byte[bufferSize];
			List<byte> bytesList = new List<byte>();

			while (true)
			{
				int bufferLength = stream.Read(bytesBuffer, 0, bufferSize);
				bytesList.AddRange(bytesBuffer);
				string buffer = Encoding.ASCII.GetString(bytesBuffer);
				MyConsole.Write(buffer);
				if (bufferLength <= 0 || buffer[0] == '\n')
				{
					return bytesList.ToArray();
				}
			}
		}
	}

	public class ResponseHeaders : Headers
	{
		public int code;
		public string message
		{
			get
			{
				if (messages.ContainsKey(code))
				{
					return messages[code];
				}
				else if (code >= 100 && code < 200)
				{
					return "Unknown Information";
				}
				else if (code >= 200 && code < 300)
				{
					return "Unknown Success";
				}
				else if (code >= 300 && code < 400)
				{
					return "Unknown Redirect";
				}
				else if (code >= 400 && code < 500)
				{
					return "Unknown Client Error";
				}
				else if (code >= 500 && code < 600)
				{
					return "Unknown Server Error";
				}
				return "Unknown Error";
			}
		}

		public ResponseHeaders(int headerCode = 200, Dictionary<string, object>? headerFields = null)
		{
			protocol = "HTTP/1.0";
			code = headerCode;
			headers.Add("Content-Language", "en");
			headers.Add("Server", "Dave's Fantastic Server (" + RuntimeInformation.RuntimeIdentifier + ")");

			if (headerFields != null)
			{
				foreach (KeyValuePair<string, object> headerField in headerFields)
				{
					headers.Add(headerField.Key, headerField.Value);
				}
			}
		}

		// Used for setting the MD5 checksum header field (may be unnecessary).
		// https://stackoverflow.com/a/24031467
		public void SetHash(string body)
		{
			SetHash(Encoding.ASCII.GetBytes(body));
		}
		public void SetHash(byte[] bytes)
		{
			using (MD5 md5 = MD5.Create())
			{
				headers.Add("Content-MD5", Convert.ToHexString(md5.ComputeHash(bytes)));
			}
			
		}
		// https://stackoverflow.com/a/10520086
		public void SetHashFile(string path)
		{
			using (MD5 md5 = MD5.Create())
			using (FileStream stream = File.OpenRead(path))
			{
				headers.Add("Content-MD5", Convert.ToHexString(md5.ComputeHash(stream)));
			}
		}

		public void SetPreferredRedirect(string currentUrl, string preferredUrl)
		{
			if (currentUrl != preferredUrl)
			{
				code = 300;
				headers.Add("Location", preferredUrl);
			}
		}

		public string GetString()
		{
			string str = protocol + " " + code + " " + message;
			Dictionary<string, object> allHeaders = new Dictionary<string, object>(headers)
			{
				{ "Status", code + " " + message },
				{ "Date", DateTime.UtcNow.ToString("r") }
			};
			foreach (KeyValuePair<string, object> header in allHeaders)
			{
				str += "\r\n" + header.Key + ": " + header.Value.ToString();
			}
			str += "\r\n\r\n";
			//Writes the raw response header.
			/*MyConsole.color = ConsoleColor.DarkGray;
			MyConsole.WriteLine(str);/**/
			return str;
		}

		public byte[] GetBytes()
		{
			return Encoding.ASCII.GetBytes(GetString());
		}
	}

	public class RequestHeaders : Headers
	{
		public string? method;
		public string? url;
		public byte[]? raw;

		public static RequestHeaders? ReadFromSocket(Socket socket)
		{
			/*SslStream sslStream = Authenticator.GetSslStream(socket);
			if (!sslStream.CanRead)
			{
				MyConsole.WriteTimestamp(socket.RemoteEndPoint);
				MyConsole.color = ConsoleColor.Red;
				MyConsole.Write(" Authentication failed.");
				return null;
			}*/
			//MyConsole.color = ConsoleColor.DarkGray;
			//MyConsole.WriteLine();
			RequestHeaders requestHeaders = new RequestHeaders();
			List<byte> bytesList = new List<byte>();
			while (true)
			{
				//byte[] bytes = ReadStreamToNewline(sslStream);
				byte[]? bytes = ReadSocketToNewline(socket);
				if (bytes == null)
				{
					return null;
				}
				bytesList.AddRange(bytes);
				string header = Encoding.ASCII.GetString(bytes);
				if (string.IsNullOrWhiteSpace(header))
				{
					break;
				}

				string[] headerParts = header.Split(':', 2);
				if (headerParts.Length >= 1 && !string.IsNullOrWhiteSpace(headerParts[0]))
				{
					if (headerParts.Length >= 2 && !string.IsNullOrWhiteSpace(headerParts[1])) // Sets normal header row.
					{
						requestHeaders.headers.Add(headerParts[0].Trim().ToLower(), headerParts[1].Trim());
					}
					else
					{
						headerParts = header.Split(" ", 3);
						if (headerParts.Length >= 1 && !string.IsNullOrWhiteSpace(headerParts[0])) // Sets first header row (method, route and protocol).
						{
							requestHeaders.method = headerParts[0];
							if (headerParts.Length >= 2 && !string.IsNullOrWhiteSpace(headerParts[1]))
							{
								requestHeaders.url = headerParts[1];
								if (headerParts.Length >= 3 && !string.IsNullOrWhiteSpace(headerParts[2]))
								{
									requestHeaders.protocol = headerParts[2];
								}
							}
						}
					}
				}
			}
			if (requestHeaders.method == null || requestHeaders.url == null || requestHeaders.protocol == null)
			{
				MyConsole.color = ConsoleColor.DarkYellow;
				MyConsole.Write(" Header error (MissingFields).");
				socket.SendError(400, "The header is missing vital fields.");
				return null;
			}
			requestHeaders.raw = bytesList.ToArray();
			return requestHeaders;
		}
	}
}
