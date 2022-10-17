using System.Globalization;
using LiteDB;

namespace WebsiteProxy
{
	public static class Util
	{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		public static string currentDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

		public static Dictionary<string, string> environment = ReadEnv(Path.Combine(currentDirectory, "tokens.env"));

		public static CultureInfo cultureInfo = CultureInfo.GetCultureInfo("sv-SE");
		public static TextInfo textInfo = cultureInfo.TextInfo;

		//static LiteDatabase database = new LiteDatabase(Path.Combine(currentDirectory, "database.db"));
		//public static ILiteCollection<string> guests = database.GetCollection<string>("guests");

		public static Dictionary<string, object> navbarButtons
		{
			get
			{
				Dictionary<string, object> projects = new Dictionary<string, object>()
				{
					{ "/", "Projects" },
					{ "/ISOBot/", "ISO-Bot" }
				};
				foreach (DirectoryInfo repository in new DirectoryInfo(Path.Combine(currentDirectory, "repositories")).GetDirectories())
				{
					if (TryGetConfigValue(repository.FullName, "name", out string name))
					{
						projects.Add("/" + repository.Name + "/", name);
					}
					else
					{
						projects.Add("/" + repository.Name + "/", repository.Name);
					}
				}
				Dictionary<string, object> buttons = new Dictionary<string, object>()
				{
					{ "/", "Home page" },
					{ "/projects", projects },
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
		
		public static bool IsInCurrentDirectory(string path)
		{
			return true;
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