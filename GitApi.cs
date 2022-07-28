using System.Diagnostics;

namespace WebsiteProxy
{
	public static class GitApi
	{
		public static void Pull(string path, Log? log = null)
		{
			Process process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "git",
					Arguments = "pull",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					WorkingDirectory = path
				}
			};
			process.Start();
			if (log != null)
			{
				if (process.WaitForExit(5000))
				{
					log.Add(new DirectoryInfo(path).Name, LogColor.Name);
					using (StreamReader stream = process.StandardOutput)
					{
						log.Add(stream.ReadToEnd().Trim('\r', '\n'), LogColor.Data);
					}
				}
			}
		}
	}
}