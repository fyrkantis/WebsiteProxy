using System.Diagnostics;

namespace WebsiteProxy
{
	public static class GitApi
	{
		public static async Task Pull(string path)
		{
			Process process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "git",
					Arguments = "pull",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = false
				}
			};
			process.StartInfo.WorkingDirectory = path;
			process.Start();
			await Task.Run(process.WaitForExit);
			using (StreamReader stream = process.StandardOutput)
			{
				MyConsole.color = ConsoleColor.Blue;
				MyConsole.Write(new DirectoryInfo(path).Name + " " + stream.ReadToEnd());
			}
		}
	}
}