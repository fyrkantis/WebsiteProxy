using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace WebsiteProxy
{
	public enum LogColor
	{
		Default = ConsoleColor.White,
		Name = ConsoleColor.Blue,
		Data = ConsoleColor.Magenta,
		Error = ConsoleColor.Red,
		Success = ConsoleColor.Green,
		Info = ConsoleColor.DarkYellow,
		Hidden = ConsoleColor.DarkGray
	}

	public static class Logger
	{
		public static BlockingCollection<Log> backlog = new BlockingCollection<Log>();

		static void Write(Log log)
		{
			WriteLine();
			// https://stackoverflow.com/a/7476203/13347795
			LogPart last = log.parts.Last();
			foreach (LogPart logPart in log.parts)
			{
				Write(logPart);
				if (!logPart.Equals(last))
				{
					Write(' ');
				}
			}
			if (last.value != null)
			{
				string? lastString = last.value.ToString();
				if (lastString != null && !lastString.EndsWith('.'))
				{
					Write('.');
				}
			}
			if (log.secondRow != null)
			{
				WriteLine(log.secondRow);
			}
		}
		static void Write(LogPart logPart)
		{
			Write(logPart.value, logPart.color);
		}

		static void Write(object? value = null, LogColor color = LogColor.Default)
		{
			Debug.Write(value);
			Console.ForegroundColor = (ConsoleColor)color;
			Console.Write(value);
			Console.ForegroundColor = (ConsoleColor)LogColor.Default;
			if (value != null)
			{
				File.AppendAllText(Util.logPath, value.ToString());
			}
		}
		static void WriteLine(object? value = null, LogColor color = LogColor.Default)
		{
			Write(Environment.NewLine, color);
			Write(value, color);
		}
		static void WriteLine(LogPart logPart)
		{
			Write(Environment.NewLine, logPart.color);
			Write(logPart.value, logPart.color);
		}

		static Task writer = Task.Run(() =>
		{
			while (!backlog.IsCompleted)
			{
				Write(backlog.Take());
			}
		});
	}

	public class Log // TODO: Optimize how references are handled.
	{
		public List<LogPart> parts = new List<LogPart>();
		public LogPart? secondRow;

		public Log(bool timestamp = false, EndPoint? endPoint = null)
		{
			if (timestamp)
			{
				Add(DateTime.UtcNow.ToString(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")));
			}

			if (endPoint != null)
			{
				IPEndPoint? ipEndPoint = endPoint as IPEndPoint;
				if (ipEndPoint != null)
				{
					Add(ipEndPoint.Address, LogColor.Name);
				}
				else
				{
					Add("\"" + endPoint + "\"", LogColor.Name);
				}
			}
		}

		public static void Write(object? value = null, LogColor color = LogColor.Default)
		{
			Log log = new Log();
			log.Add(value, color);
			log.Write();
		}

		public void Write()
		{
			Logger.backlog.Add(this);
		}

		public void Add(object? value, LogColor color = LogColor.Default)
		{
			parts.Add(new LogPart(value, color));
		}
		public void Add(object? value, bool success)
		{
			if (success)
			{
				Add(value, LogColor.Success);
			}
			else
			{
				Add(value, LogColor.Error);
			}
		}
		public void Add(ResponseHeaders responseHeaders)
		{
			AddRange(responseHeaders.code >= 100 && responseHeaders.code < 400, responseHeaders.code, responseHeaders.message);

			if (responseHeaders.headers.ContainsKey("Location"))
			{
				AddRange(LogColor.Data, "->", responseHeaders.headers["Location"]);
			}
		}

		public void AddRange(LogColor color = LogColor.Default, params object?[] values)
		{
			Add(string.Join(' ', values), color);
		}
		public void AddRange(bool success, params object?[] values)
		{
			Add(string.Join(' ', values), success);
		}
	}

	public class LogPart
	{
		public object? value;
		public LogColor color;
		public LogPart(object? value, LogColor color = LogColor.Default)
		{
			this.value = value;
			this.color = color;
		}
	}
}
