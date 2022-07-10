using System.Diagnostics;
using System.Globalization;

namespace WebsiteProxy
{
	public static class Util
	{
		public static string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

		public static CultureInfo cultureInfo = CultureInfo.GetCultureInfo("sv-SE");// "yyyy-MM-dd HH:mm:ss.fff"
		public static TextInfo textInfo = cultureInfo.TextInfo;

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

		public static void WriteLine(object? value)
		{
			Write("\r\n");
			Write(value);
		}

		public static void WriteTimestamp()
		{
			color = ConsoleColor.White;
			WriteLine(DateTime.UtcNow.ToString(Util.cultureInfo.DateTimeFormat.SortableDateTimePattern));
			color = ConsoleColor.Blue;
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