using Amazon.CDK.AWS.IAM;
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
			{ 400, "Bad Request" }, { 401, "Unauthorized"}, { 404, "Not Found" }, { 405, "Method Not Allowed" }, { 408, "Request Timeout"}, { 415, "Unsupported Media Type" }, { 422, "Unprocessable Entity" },
			{ 300, "Multiple Choices" }, { 303, "See Other"}, { 308, "Permanent Redirect" },
			{ 500, "Internal Server Error" }, { 501, "Not Implemented" }, { 504, "Gateway Timeout"}
		};

		const int bufferSize = 1;
		public string? protocol;
		// https://stackoverflow.com/a/13230450
		public Dictionary<string, object> headers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		public void Add(string name, object value)
		{
			if (!headers.ContainsKey(name))
			{
				headers.Add(name, value);
			}
		}

		public static byte[]? ReadSocketToNewline(Socket socket, Log? log = null)
		{
			byte[] bytesBuffer = new byte[bufferSize];
			List<byte> bytesList = new List<byte>();

			while (true)
			{
				if (!socket.IsConnected())
				{
					if (log != null)
					{
						log.Add("Connection lost", LogColor.Error);
						log.Write();
					}
					return null;
				}
				int bufferLength = socket.Receive(bytesBuffer, 0, bufferSize, SocketFlags.None, out SocketError error);
				if (error != SocketError.Success)
				{
					if (log != null)
					{
						log.AddRange(LogColor.Info, "Header error:", error);
					}
					socket.SendError(408, "Connection timed out.", log: log);
					return null;
				}
				bytesList.AddRange(bytesBuffer);
				string buffer = Encoding.ASCII.GetString(bytesBuffer);
				if (bufferLength <= 0 || buffer[0] == '\n')
				{
					return bytesList.ToArray();
				}
			}
		}

		public static byte[]? ReadStreamToNewline(Stream stream, Log? log = null)
		{
			byte[] bytesBuffer = new byte[bufferSize];
			List<byte> bytesList = new List<byte>();

			while (true)
			{
				int bufferLength;
				try
				{
					bufferLength = stream.Read(bytesBuffer, 0, bufferSize);
				}
				catch
				{
					if (log != null)
					{
						log.Add("Connection lost", LogColor.Error);
						log.Write();
					}
					return null;
				}
				bytesList.AddRange(bytesBuffer);
				string buffer = Encoding.ASCII.GetString(bytesBuffer);
				//Log.Write(buffer, LogColor.Hidden);
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

		public ResponseHeaders(int code = 200, Dictionary<string, object>? headers = null)
		{
			protocol = "HTTP/1.0";
			this.code = code;
			this.headers["Content-Language"] = "en";
			this.headers["Server"] = "Dave's Fantastic Server (" + RuntimeInformation.RuntimeIdentifier + ")";

			if (headers != null)
			{
				foreach (KeyValuePair<string, object> headerField in headers)
				{
					this.headers[headerField.Key] = headerField.Value;
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
				headers["Content-MD5"] = Convert.ToHexString(md5.ComputeHash(bytes));
			}
			
		}
		// https://stackoverflow.com/a/10520086
		public void SetHashFile(string path)
		{
			using (MD5 md5 = MD5.Create())
			using (FileStream stream = File.OpenRead(path))
			{
				headers["Content-MD5"] = Convert.ToHexString(md5.ComputeHash(stream));
			}
		}

		public void SetPreferredRedirect(string currentUrl, string preferredUrl)
		{
			if (currentUrl != preferredUrl)
			{
				code = 300;
				headers["Location"] = preferredUrl;
			}
		}

		public void SetUser(User user)
		{
			headers["Set-Cookie"] = "Id=" + user.id + "; Path=/";
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
				str += Environment.NewLine + header.Key + ": " + header.Value.ToString();
			}
			str += Environment.NewLine + Environment.NewLine;
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
		public Dictionary<string, string> cookie = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public static RequestHeaders? ReadFromSocket(Socket socket, Log? log = null)
		{
			/*SslStream sslStream;
			try
			{
				sslStream = Authenticator.GetSslStream(socket, log);
			}
			catch (Exception exception)
			{
				if (log != null)
				{
					log.secondRow = new LogPart(exception, LogColor.Error);
					log.Write();
				}
			}
			return null;
			if (!sslStream.CanRead)
			{
				if (log != null)
				{
					socket.SendError(400, "Authentication failed.");
				}
				return null;
			}/**/
			RequestHeaders requestHeaders = new RequestHeaders();
			List<byte> bytesList = new List<byte>();
			while (true)
			{
				//byte[]? bytes = ReadStreamToNewline(new NetworkStream(socket));
				byte[]? bytes = ReadSocketToNewline(socket, log);
				if (bytes == null)
				{
					string headers = Encoding.ASCII.GetString(bytesList.ToArray());
					if (log != null && !string.IsNullOrWhiteSpace(headers))
					{
						log.Add(headers, LogColor.Hidden);
					}
					return null;
				}
				bytesList.AddRange(bytes);
				string header = Encoding.ASCII.GetString(bytes);
				if (string.IsNullOrWhiteSpace(header))
				{
					break;
				}
				//Console.Write(header, LogColor.Hidden);

				string[] headerParts = header.Split(':', 2);
				if (headerParts.Length >= 1 && !string.IsNullOrWhiteSpace(headerParts[0]))
				{
					if (headerParts.Length >= 2 && !string.IsNullOrWhiteSpace(headerParts[1])) // Sets normal header row.
					{
						requestHeaders.Add(headerParts[0].Trim().ToLower(), headerParts[1].Trim());
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
				if (log != null)
				{
					log.Add("Header error: MissingFields", LogColor.Info);
				}

				socket.SendError(400, "Missing vital header fields.", log: log);
				return null;
			}
			if (requestHeaders.headers.ContainsKey("Cookie"))
			{
				string? rawCookie = requestHeaders.headers["Cookie"].ToString();
				if (rawCookie != null)
				{
					foreach (string cookie in rawCookie.Split(';'))
					{
						Console.WriteLine(cookie);
						string[] cookieParts = cookie.Split('=', 2);
						if (cookieParts.Length >= 2)
						{
							requestHeaders.cookie.Add(cookieParts[0].Trim(), cookieParts[1].Trim());
						}
					}
				}
			}
			requestHeaders.raw = bytesList.ToArray();
			return requestHeaders;
		}
	}
}
