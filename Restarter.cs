using System.Diagnostics;

namespace WebsiteProxy
{
	static class Restarter
	{
		public static void Restart()
		{
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
