using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Website
	{
		static Route[] routes =
		{
			new Route("github", new string[] { "POST" }, (clientSocket, requestHeaders, path, log) =>
			{
				string? data = clientSocket.ReadPost(requestHeaders);
				if (!requestHeaders.headers.ContainsKey("Content-Type"))
				{
					clientSocket.SendError(400, "Missing \"Content-Type\" header field.", log: log);
					return;
				}
				if (((string)requestHeaders.headers["Content-Type"]).ToLower() != "application/json")
				{
					clientSocket.SendError(415, "Expects application/json.", log: log);
					return;
				}
				if (data == null)
				{
					clientSocket.SendError(400, "No data was received.", log: log);
					return;
				}
				JObject? json = JObject.Parse(data);
				if (json == null)
				{
					clientSocket.SendError(400, "Unable to decode json.", log: log);
					return;
				}
				JToken? repository = json.SelectToken("repository");
				if (repository == null)
				{
					clientSocket.SendError(400, "\"repository\" field is missing.", log: log);
					return;
				}
				string? name = repository.Value<string>("name");
				if (name == null)
				{
					clientSocket.SendError(400, "\"name\" field in repository is missing.", log: log);
					return;
				}
				/*string? node = repository.Value<string>("node_id");
				if (node == null || node != Util.environment["gitNodeToken"])
				{
					clientSocket.SendError(401, "The correct repository \"node_id\" token was not provided.");
					return;
				}*/
				//MyConsole.color = ConsoleColor.Blue;
				//MyConsole.WriteMany(name);
				if (name == "WebsiteProxy")
				{
					try
					{
						GitApi.Pull(Util.currentDirectory, log);
						clientSocket.SendResponse(204, log: log);
						Restarter.Restart();
					}
					catch (Exception exception)
					{
						if (log != null)
						{
							log.Add("(Exception: " + exception + ")", LogColor.Error);
						}
						clientSocket.SendError(500, exception.Message, log: log);
					}
					return;
				}

				string? repositoryPath = null;
				foreach (string directory in Directory.GetDirectories(Path.Combine(Util.currentDirectory, "websites")))
				{
					if (Util.TryGetConfigValue(directory, "repository", out string? repositoryName) && repositoryName == name)
					{
						repositoryPath = directory;
						break;
					}
				}

				if (repositoryPath == null)
				{
					clientSocket.SendError(404, "The repository \"" + name + "\" does not exist on this server.", log: log);
					return;
				}
				try
				{
					GitApi.Pull(repositoryPath, log);
					clientSocket.SendResponse(204, log: log);
				}
				catch (Exception exception)
				{
					if (log != null)
					{
						log.Add("(Exception: " + exception + ")", LogColor.Error);
					}
					clientSocket.SendError(500, exception.Message, log: log);
				}
			}),
			new Route("formtest", new string[] { "POST" }, (clientSocket, requestHeaders, path, log) =>
			{
				string? data = clientSocket.ReadPost(requestHeaders);
				if (log != null)
				{
					log.secondRow = new LogPart(data, LogColor.Data);
				}
				if (data != null)
				{
					clientSocket.SendPageResponse(Path.Combine(Util.currentDirectory, "pages", "error.html"), new Dictionary<string, object>
					{
						{ "navbarButtons", Util.navbarButtons },
						{ "message", "Data received" },
						{ "errors", data }
					}, log);
				}
				else
				{
					clientSocket.SendError(400, "No data was received.", log: log);
				}
			}),
			new Route("test", new string[] { "GET" }, (clientSocket, requestHeaders, path, log) =>
			{
				clientSocket.SendBodyResponse(";)", log);
			})
		};

		class Route
		{
			public string name;
			public string[] methods;
			public Action<Socket, RequestHeaders, string, Log?> Action; // clientSocket, requestHeaders, path and log.

			public Route(string routeName, string[] routeMethods, Action<Socket, RequestHeaders, string, Log?> RouteAction)
			{
				name = routeName;
				methods = routeMethods;
				Action = RouteAction;
			}
		}

		public static async void HandleConnection(Socket clientSocket, RequestHeaders requestHeaders, Log? log = null)
		{
			if (log != null)
			{
				log.AddRange(LogColor.Info, requestHeaders.method, requestHeaders.url);
			}

			// Writes the raw request headers.
			/*if (requestHeaders.raw != null)
			{
				MyConsole.color = ConsoleColor.DarkGray;
				MyConsole.WriteLine(Encoding.ASCII.GetString(requestHeaders.raw));
			}/**/

			if (requestHeaders.url == null)
			{
				clientSocket.SendError(400, "No requested URL was specified.", log: log);
				return;
			}
			if (requestHeaders.method == null)
			{
				clientSocket.SendError(400, "No requested method was specified.", log: log);
				return;
			}

			// The preferred path to be used.
			string shortPath = requestHeaders.url.Replace(".html", null, true, null).Replace("index", null, true, null).Trim('/');

			// Sends a fake .env file when one is requested.
			if (shortPath.ToLower().EndsWith(".env"))
			{
				ResponseHeaders responseHeaders = new ResponseHeaders(headerFields: new Dictionary<string, object>
				{
					{ "Content-Disposition", "inline; filename=\".env\"" },
					{ "Content-Type", "application/x-envoy" }
				});
				if (log != null)
				{
					log.Add("Trolled", LogColor.Data);
				}
				clientSocket.SendFileResponse(Path.Combine(Util.currentDirectory, "templates", "fakeEnv.txt"), responseHeaders, log);
				return;
			}

			// Checks if the url path matches a pre-defined path.
			string basePath = shortPath.Split('/', 2)[0];
			string remainingPath = shortPath.Remove(0, basePath.Length).TrimStart('/');
			foreach (Route route in routes)
			{
				if (route.name.ToLower() == basePath.ToLower())
				{
					string preferredPath = ("/" + route.name).TrimEnd('/') + "/";
					if (requestHeaders.url != preferredPath)
					{
						clientSocket.SendRedirectResponse(308, preferredPath, log: log);
						return;
					}
					foreach (string method in route.methods)
					{
						if (requestHeaders.method.ToUpper() == method)
						{
							route.Action(clientSocket, requestHeaders, remainingPath, log);
							return;
						}
					}
					clientSocket.SendError(405, "The route \"/" + route.name + "/\" only accepts " + Util.GrammaticalListing(route.methods) + " requests.", new Dictionary<string, object>() { { "Allow", string.Join(", ", route.methods) } }, log: log);
					return;
				}
			}

			// Checks if the url path matches a git repository.
			foreach (DirectoryInfo directory in new DirectoryInfo(Path.Combine(Util.currentDirectory, "websites")).GetDirectories())
			{
				if (directory.Name.ToLower() == basePath.ToLower())
				{
					// Tries to load as an asset file.
					if (clientSocket.TryLoad(requestHeaders, Path.Combine(directory.FullName, remainingPath), "/" + directory.Name + "/" + remainingPath, log: log))
					{
						return;
					}

					// Tries to load as a html page (but not as template).
					foreach (string pathAlternative in new string[] { remainingPath + ".html", Path.Combine(remainingPath, "index.html") })
					{
						if (clientSocket.TryLoad(requestHeaders, Path.Combine(directory.FullName, pathAlternative), ("/" + directory.Name + "/" + remainingPath).TrimEnd('/') + "/", log: log))
						{
							return;
						}
					}

					// Looks for custom 404 page.
					if (clientSocket.TryLoad(requestHeaders, Path.Combine(directory.FullName, "404.html"), log: log))
					{
						return;
					}
					// Gives up and sends own 404 page.
					clientSocket.SendError(404, "The requested file \"" + remainingPath + "\" could not be found in /" + directory.Name + "/.", log: log);
					return;
				}
			}

			// Tries to load as an asset file.
			if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "assets", shortPath), "/" + shortPath, log: log))
			{
				return;
			}

			// Tries to load as a html page (as template).
			foreach (string pathAlternative in new string[] { shortPath + ".html", Path.Combine(shortPath, "index.html") })
			{
				if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "pages", pathAlternative), ("/" + shortPath).TrimEnd('/') + "/", template: true, parameters: new Dictionary<string, object> { { "navbarButtons", Util.navbarButtons } }, log: log))
				{
					return;
				}
			}

			clientSocket.SendError(404, "The requested file \"/" + shortPath + "\" could not be found.", log: log);
			//context.Send(418, "I'm a teapot", "And I can't be asked to brew coffee.");
		}

		// Returns true if the page or an http error was sent, or false otherwise.
		public static bool TryLoad(this Socket clientSocket, RequestHeaders requestHeaders, string path, string? preferredPath = null, bool template = false, Dictionary<string, object>? parameters = null, Log? log = null)
		{
			if (!File.Exists(path))
			{
				return false;
			}
			if (requestHeaders.method == null || requestHeaders.method.ToUpper() != "GET") // method should already not be null here.
			{
				clientSocket.SendError(405, "The requested file \"" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } }, log: log);
				return true;
			}
			ResponseHeaders responseHeaders = new ResponseHeaders();
			if (preferredPath != null && requestHeaders.url != preferredPath)
			{
				clientSocket.SendRedirectResponse(308, preferredPath, log: log);
				return true;
			}
			if (template)
			{
				clientSocket.SendPageResponse(path, responseHeaders, parameters, log: log);
			}
			else
			{
				clientSocket.SendFileResponse(path, responseHeaders, log: log);
			}
			return true;
		}
	}
}