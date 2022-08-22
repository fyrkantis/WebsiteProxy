using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Palmer;

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
			DateTime writingTime = DateTime.UtcNow;

			// https://stackoverflow.com/a/7476203/13347795
			LogPart last = log.parts.Last();
			foreach (LogPart logPart in log.parts)
			{
				if (!logPart.Equals(last))
				{
					Write(logPart);
					Write(' ');
				}
				else
				{
					char? lastEnding = null;
					string? lastString = null;
					if (logPart.value != null)
					{
						lastEnding = '.';
						lastString = logPart.value.ToString();
					}
					if (lastString != null && (lastString.EndsWith('.') || lastString.EndsWith('!') || lastString.EndsWith('?')))
					{
						lastEnding = lastString[lastString.Length - 1];
						Write(lastString.Substring(0, lastString.Length - 1), logPart.color);
					}
					else
					{
						Write(logPart);
					}
					Write(lastEnding);
				}
			}
			if (log.writeTimeTaken)
			{
				Write(' ');
				if (log.finishTime != null)
				{
					Write(((DateTime)log.finishTime - log.startTime).Milliseconds + " ms", LogColor.Hidden);
				}
				else
				{
					Write((writingTime - log.startTime).Milliseconds + " ms*", LogColor.Hidden);
				}
			}
			WriteLine();
			if (log.nextRow != null)
			{
				Write(log.nextRow);
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
				DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Util.currentDirectory, "logs"));

				// Throws IOException sometimes.
				// https://stackoverflow.com/a/24859893/13347795
				Retry.On<IOException>().For(TimeSpan.FromSeconds(1)).With(context =>
				{
					File.AppendAllText(Path.Combine(directory.FullName, DateTime.UtcNow.ToString("yyyy-MM-dd") + "log.txt"), value.ToString());
				});
			}
		}

		static void WriteLine(LogPart logPart)
		{
			WriteLine(logPart.value, logPart.color);
		}
		static void WriteLine(object? value = null, LogColor color = LogColor.Default)
		{
			Write(value, color);
			Write(Environment.NewLine, color);
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
		public Log? nextRow = null;
		public DateTime startTime = DateTime.UtcNow;
		public DateTime? finishTime;
		public bool writeTimeTaken;

		public Log(bool timestamp = false, EndPoint? endPoint = null, bool writeTimeTaken = true)
		{
			this.writeTimeTaken = writeTimeTaken;
			if (timestamp)
			{
				Add(startTime.ToString(DateTime.UtcNow.ToString("HH:mm:ss.fff")));
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

		public static void WriteRestartTime()
		{
			if (Restarter.nextRestart == null)
			{
				return;
			}
			Log log = new Log(timestamp: true, writeTimeTaken: false);
			log.Add("Time until next server restart:", LogColor.Info);
			log.Add(((TimeSpan)(Restarter.nextRestart - DateTime.UtcNow)).ToString("h':'m':'s"), LogColor.Data);
			log.Write();
		}

		public static void Write(object? value = null, LogColor color = LogColor.Default)
		{
			Log log = new Log(writeTimeTaken: false);
			log.Add(value, color);
			log.Write();
		}

		public void Write()
		{
			finishTime = DateTime.UtcNow;
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

		public void AddRow(object? value, LogColor color = LogColor.Default, bool writeTimeTaken = false)
		{
			if (nextRow == null)
			{
				nextRow = new Log(writeTimeTaken: writeTimeTaken);
				nextRow.Add(value, color);
			}
			else
			{
				nextRow.AddRow(value, color, writeTimeTaken);
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
