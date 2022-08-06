using System.Globalization;

namespace WebsiteProxy
{
	public static class Util
	{
		public static string currentDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
		public static string repositoryDirectory = Path.Combine(currentDirectory, "website", "repositories");

		public static Dictionary<string, string> environment = ReadEnv(Path.Combine(currentDirectory, "tokens.env"));

		public static CultureInfo cultureInfo = CultureInfo.GetCultureInfo("sv-SE");
		public static TextInfo textInfo = cultureInfo.TextInfo;

		public static Dictionary<string, object> navbarButtons
		{
			get
			{
				Dictionary<string, object> repositories = new Dictionary<string, object>()
				{
					{ "/", "Repositories" }
				};
				foreach (DirectoryInfo repository in new DirectoryInfo(repositoryDirectory).GetDirectories())
				{
					if (TryGetConfigValue(repository.FullName, "name", out string name))
					{
						repositories.Add("/" + repository.Name + "/", name);
					}
					else
					{
						repositories.Add("/" + repository.Name + "/", repository.Name);
					}
				}
				Dictionary<string, object> buttons = new Dictionary<string, object>()
				{
					{ "/", "Home page" },
					{ "/repositories", repositories },
					{ "/ha/youDumbFuck/", "Some crazy third page" }
				};
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

		public static bool TryGetConfig(string repositoryPath, out Dictionary<string, string> config)
		{
			string configPath = Path.Combine(repositoryPath, "config.env");
			if (!File.Exists(configPath))
			{
				config = new Dictionary<string, string>();
				return false;
			}
			config = ReadEnv(configPath);
			return true;
		}
		public static bool TryGetConfigValue(string repositoryPath, string key, out string value)
		{
			if (TryGetConfig(repositoryPath, out Dictionary<string, string> config) && config.TryGetValue(key, out string? nullableValue))
			{
				value = nullableValue;
				return true;
			}
			value = "";
			return false;
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
}