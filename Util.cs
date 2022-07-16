using LibGit2Sharp;
using System.Diagnostics;
using System.Globalization;
using System.Net;

namespace WebsiteProxy
{
	public static class Util
	{
		public static string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

		public static CultureInfo cultureInfo = CultureInfo.GetCultureInfo("sv-SE");
		public static TextInfo textInfo = cultureInfo.TextInfo;

		public static Dictionary<string, string> environment = ReadEnv(Path.Combine(currentDirectory, ".env"));

		public static Dictionary<string, object> navbarButtons
		{
			get
			{
				Dictionary<string, object> buttons = new Dictionary<string, object>();
				DirectoryInfo repositories = new DirectoryInfo(Path.Combine(currentDirectory, "websites\\"));
				foreach (DirectoryInfo repository in repositories.GetDirectories())
				{
					buttons.Add("/" + repository.Name + "/", repository.Name);
				}
				return buttons;
			}
		}

		public static Dictionary<string, string> ReadEnv(string path)
		{
			Dictionary<string, string> env = new Dictionary<string, string>();
			foreach (string line in File.ReadAllLines(path))
			{
				if (line.Contains('='))
				{
					string[] parts = line.Split('=', 2);
					if (parts.Length >= 2)
					{
						env.Add(parts[0], parts[1]);
					}
				}
			}
			return env;
		}

		public static string GrammaticalListing(IEnumerable<object> collection, bool quotes = false)
		{
			int count = collection.Count();
			if (count >= 2)
			{
				if (quotes)
				{
					return "\"" + string.Join("\", \"", collection.Take(count - 1)) + "\" and \"" + collection.Last() + "\"";
				}
				else
				{
					return string.Join(", ", collection.Take(count - 1)) + " and " + collection.Last();
				}
			}
			else if (count == 1)
			{
				string? firstString = collection.First().ToString();
				if (firstString == null)
				{
					return "";
				}
				if (quotes)
				{
					return "\"" + firstString + "\"";
				}
				else
				{
					return firstString;
				}
			}
			return "";
		}

		public static string[] FindMissingKeys(Dictionary<string, object> dictionary, string[] keys)
		{
			List<string> missing = new List<string>();
			foreach(string key in keys)
			{
				if (!dictionary.ContainsKey(key))
				{
					missing.Add(key);
				}
			}
			return missing.ToArray();
		}
	}

	public static class MyConsole
	{
		public static ConsoleColor color
		{
			set
			{
#if DEBUG
				Console.ForegroundColor = value;
#endif
			}
		}

		public static void SetStatusColor(bool condition)
		{
			if (condition)
			{
				color = ConsoleColor.Green;
			}
			else
			{
				color = ConsoleColor.Red;
			}
		}

		public static void Write(object? value)
		{
			Debug.Write(value);
#if DEBUG
			Console.Write(value);
#endif
		}

		public static void WriteLine(object? value = null)
		{
			Write("\r\n");
			Write(value);
		}

		public static void WriteTimestamp(EndPoint? endPoint = null)
		{
			color = ConsoleColor.White;
			WriteLine(DateTime.UtcNow.ToString(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")));
			color = ConsoleColor.Blue;
			if (endPoint != null)
			{
				Write(" ");
				IPEndPoint? ipEndPoint = endPoint as IPEndPoint;
				if (ipEndPoint != null)
				{
					Write(ipEndPoint.Address);
				}
				else
				{
					Write("\"" + endPoint + "\"");
				}
			}
		}

		public static void WriteMergeResult(MergeResult? response)
		{
			if (response == null)
			{
				color = ConsoleColor.Red;
				Write("NoResult");
				return;
			}
			switch (response.Status)
			{
				case MergeStatus.UpToDate:
					color = ConsoleColor.Green;
					break;
				case MergeStatus.FastForward:
					color = ConsoleColor.DarkYellow;
					break;
				default:
					color = ConsoleColor.Red;
					break;
			}
			Write(response.Status);
			if (response.Commit != null)
			{
				color = ConsoleColor.Magenta;
				Write(" (" + response.Commit + ")");
			}
		}

		public static void WriteData(string name, object data)
		{
			color = ConsoleColor.White;
			WriteLine(name + ": ");
			color = ConsoleColor.Magenta;
			Write(data);
		}

		public static void WriteMany(params object?[] elements)
		{
			foreach (object? element in elements)
			{
				if (element != null)
				{
					Write(" " + element.ToString());
				}
			}
		}

		public static void WriteHttpStatus(ResponseHeaders responseHeaders)
		{
			SetStatusColor(responseHeaders.code >= 100 && responseHeaders.code < 400);
			WriteMany(responseHeaders.code, responseHeaders.message);

			if (responseHeaders.headers.ContainsKey("Location"))
			{
				color = ConsoleColor.Magenta;
				WriteMany("->", responseHeaders.headers["Location"]);
			}
		}
	}
}