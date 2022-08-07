using System.Diagnostics;

namespace WebsiteProxy
{
	public static class Restarter
	{
		public static TimeSpan[] restartTimes = new TimeSpan[] // The times of day that the server is restarted on.
		{
			//new TimeSpan(6, 0, 0),
			//new TimeSpan(12, 0, 0),
			//new TimeSpan(18, 0, 0),
			//new TimeSpan(24, 0, 0)
		};
		public static DateTime? nextRestart = ClockTimer.NextTime(restartTimes);

		static Restarter()
		{
			if (nextRestart != null)
			{
				ClockTimer.DoAtTime((DateTime)nextRestart, () =>
				{
					Restart();
				});
			}
		}

		// Bug on linux: The "restarted" process runs in the background somehow and cannot be manually terminated without a kill command.
		// https://stackoverflow.com/a/67702583/13347795.
		public static void Restart()
		{
			Log.Write("Restarting..."); // TODO: Leave delay to give log time to be written.
			Process process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "dotnet",
#if DEBUG
					Arguments = "run -c Debug",
#else
					Arguments = "run -c Release",
#endif
					WorkingDirectory = Util.currentDirectory
				}
			};
			process.Start();
			Environment.Exit(0);
		}
	}
}
