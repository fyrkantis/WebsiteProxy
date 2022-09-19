using LiteDB;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Website
	{
		static Route[] routes =
		{
			new Route("/github/", new string[] { "POST" }, (clientSocket, requestHeaders, route, log) =>
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
				foreach (string directory in Directory.GetDirectories(Path.Combine(Util.currentDirectory, "repositories")))
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
			new Route("/repositories/", null, (clientSocket, requestHeaders, route, log) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				if (string.IsNullOrWhiteSpace(route))
				{
					List<Dictionary<string, object>> repositories = new List<Dictionary<string, object>>();
					foreach (DirectoryInfo directory in new DirectoryInfo(Path.Combine(Util.currentDirectory, "repositories")).GetDirectories())
					{
						Dictionary<string, object> repository = new Dictionary<string, object>();
						repository.Add("route", directory.Name);
						if (Util.TryGetConfig(directory.FullName, out Dictionary<string, string> config))
						{
							if (config.TryGetValue("name", out string? name))
							{
								repository.Add("name", name);
							}
						}
						// Looks for readme.
						foreach (FileInfo readme in directory.GetFiles("README*", SearchOption.TopDirectoryOnly))
						{
							repository.Add("readme", Path.Combine("repositories", directory.Name, readme.Name));
							break;
						}
						repositories.Add(repository);
					}
					parameters.Add("repositories", repositories);
				}

				// Tries to load a regular page in the /website/repositories/ folder.
				if (clientSocket.TrySendUnknown(requestHeaders, "repositories/" + route, parameters: parameters, log: log))
				{
					return;
				}

				// Tries to load a git repository page.
				foreach (DirectoryInfo directory in new DirectoryInfo(Path.Combine(Util.currentDirectory, "repositories")).GetDirectories())
				{
					string shortName = directory.Name.Trim('/').ToLower();
					string remainingPath = route.Remove(0, directory.Name.Length).Trim('/', '\\');
					if (route.ToLower().StartsWith(shortName))
					{
						string websitePath = directory.Name;
						Dictionary<string, object> headers = new Dictionary<string, object>();
						if (Util.TryGetConfig(directory.FullName, out Dictionary<string, string> config))
						{
							if (config.TryGetValue("folder", out string? websiteFolder))
							{
								websitePath = Path.Combine(directory.Name, websiteFolder.Trim('/', '\\'));
							}
							if (config.TryGetValue("language", out string? language))
							{
								headers.Add("Content-Language", language);
							}
						}
						// Tries to load file or page.
						if (clientSocket.TrySendUnknown(requestHeaders, Path.Combine(websitePath, remainingPath), directory: "repositories", route: "repositories/" + route, enableTemplate: false, log: log))
						{
							return;
						}

						// Looks for custom 404 page.
						if (Util.TryGetConfigValue(directory.FullName, "404", out string errorPage) && clientSocket.TrySendFile(requestHeaders, Path.Combine(directory.FullName, errorPage.Trim('/', '\\')), headers: headers, log: log))
						{
							return;
						}
						// Gives up and sends own 404 page.
						clientSocket.SendError(404, "The requested page \"" + remainingPath + "\" could not be found in /" + directory.Name + "/.", log: log);
						return;
					}
				}
				clientSocket.SendError(404, "The requested repository \"" + route + "\" could not be found.");
			}),
			new Route("/guest/", new string[] { "POST" }, (clientSocket, requestHeaders, route, log) =>
			{
				string? data = clientSocket.ReadPost(requestHeaders);
				if (log != null)
				{
					log.AddRow(data, LogColor.Data);
				}
				if (data != null)
				{
					if (data.StartsWith("name=")) // TODO: Replace temp data enterpreter.
					{
						//Dictionary<string, BsonValue> bsonDictionary = new Dictionary<string, BsonValue>(){ { "aaa", data.Substring(5) } };
						/*BsonDocument bsonDocument = new BsonDocument();
						bsonDocument["_id"] = ObjectId.NewObjectId();
						bsonDocument["name"] = data.Substring(5);
						BsonMapper mapper = new BsonMapper();
						BsonValue value = mapper.Serialize(data.Substring(5));*/
						//LiteDatabase database = new LiteDatabase(Path.Combine(Util.currentDirectory, "database.db"));
						//var guests = database.GetCollection<string>("guests");
						//guests.Insert(data.Substring(5));
						//clientSocket.SendRedirectResponse(303, "/", log);
						clientSocket.SendError(501, "The guest list is not functional yet.");
					}
					else
					{
						clientSocket.SendError(400, "Data was malformed.", log: log);
					}
				}
				else
				{
					clientSocket.SendError(422, "No data was received.", log: log);
				}
			})

		};

		class Route
		{
			public string name;
			public string[]? methods;
			public Action<Socket, RequestHeaders, string, Log?> action; // clientSocket, requestHeaders, path and log.

			public Route(string name, string[]? methods, Action<Socket, RequestHeaders, string, Log?> action)
			{
				this.name = name;
				this.methods = methods;
				this.action = action;
			}
		}

		public static async void HandleConnection(Socket clientSocket, RequestHeaders requestHeaders, Log? log = null)
		{
			// Path vs route explanation: path is for the server directory and route is the request url, ok?

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

			// The preferred route to be used.
			string route = requestHeaders.url.Replace(".html", null, true, null).Replace("index", null, true, null).Trim('/');

			// Sends a fake .env file when one is requested.
			if (route.ToLower().EndsWith(".env"))
			{
				ResponseHeaders responseHeaders = new ResponseHeaders(headers: new Dictionary<string, object>
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

			// Checks if the route matches a pre-defined route.
			foreach (Route routeAlternative in routes)
			{
				string baseRoute = routeAlternative.name.Trim('/').ToLower();
				if (route.ToLower().StartsWith(baseRoute))
				{
					string subRoute = route.Remove(0, baseRoute.Length).TrimStart('/');
					if (!requestHeaders.url.StartsWith(routeAlternative.name))
					{
						clientSocket.SendRedirectResponse(308, routeAlternative.name + subRoute, log: log);
						return;
					}
					if (routeAlternative.methods == null) // Means that the route doesn't care about which method is used.
					{
						routeAlternative.action.Invoke(clientSocket, requestHeaders, subRoute, log);
					}
					else
					{
						foreach (string method in routeAlternative.methods)
						{
							if (requestHeaders.method.ToUpper() == method)
							{
								routeAlternative.action.Invoke(clientSocket, requestHeaders, subRoute, log);
								return;
							}
						}
						clientSocket.SendError(405, "The route \"" + routeAlternative.name + "\" only accepts " + Util.GrammaticalListing(routeAlternative.methods) + " requests.", new Dictionary<string, object>() { { "Allow", string.Join(", ", routeAlternative.methods) } }, log: log);
					}
					return;
				}
			}

			// Tries to load a file or page.
			if (!clientSocket.TrySendUnknown(requestHeaders, route, log: log))
			{
				clientSocket.SendError(404, "The requested page \"" + requestHeaders.url + "\" could not be found.", log: log);
				//context.Send(418, "I'm a teapot", "And I can't be asked to brew coffee.");
			}
		}

		// Returns true if a response was sent (error or not), otherwise false if the page doesn't exist.
		public static bool TrySendUnknown(this Socket clientSocket, RequestHeaders requestHeaders, string path, string directory = "website", string? route = null, Dictionary<string, object>? parameters = null, bool enableTemplate = true, Dictionary<string, object>? headers = null, Log? log = null)
		{
			string routeName = path;
			if (route != null)
			{
				routeName = route;
			}

			// Tries to load as an asset file.
			if (clientSocket.TrySendFile(requestHeaders, Path.Combine(Util.currentDirectory, directory, path.Trim('/', '\\')), "/" + routeName.Trim('/'), log: log))
			{
				return true;
			}

			// Tries to load as a html page (as template).
			foreach (string routeAlternative in new string[] { path + ".html", Path.Combine(path, "index.html") })
			{
				if (enableTemplate)
				{
					if (clientSocket.TrySendTemplate(requestHeaders, Path.Combine(Util.currentDirectory, directory, routeAlternative.Trim('/', '\\')), ("/" + routeName).TrimEnd('/') + "/", parameters, log: log))
					{
						return true;
					}
				}
				else
				{
					if (clientSocket.TrySendFile(requestHeaders, Path.Combine(Util.currentDirectory, directory, routeAlternative.Trim('/', '\\')), ("/" + routeName).TrimEnd('/') + "/", log: log))
					{
						return true;
					}
				}
			}
			return false;
		}
		public static bool TrySendFile(this Socket clientSocket, RequestHeaders requestHeaders, string path, string? preferredRoute = null, Dictionary<string, object>? headers = null, Log? log = null)
		{
			if (!File.Exists(path))
			{
				return false;
			}
			if (!clientSocket.TrySendError(requestHeaders, path, preferredRoute, log))
			{
				ResponseHeaders responseHeaders = new ResponseHeaders(headers: headers);
				clientSocket.SendFileResponse(path, responseHeaders, log: log);
			}
			return true;
		}
		public static bool TrySendTemplate(this Socket clientSocket, RequestHeaders requestHeaders, string path, string? preferredRoute = null, Dictionary<string, object>? parameters = null, Dictionary<string, object>? headers = null, Log? log = null)
		{
			if (!File.Exists(path))
			{
				return false;
			}
			if (!clientSocket.TrySendError(requestHeaders, path, preferredRoute, log))
			{
				ResponseHeaders responseHeaders = new ResponseHeaders(headers: headers);
				clientSocket.SendPageResponse(path, responseHeaders, parameters, log: log);
			}
			return true;
		}
		// Returns true and sends error response if there are any errors, otherwise returns false.
		public static bool TrySendError(this Socket clientSocket, RequestHeaders requestHeaders, string path, string? preferredPath = null, Log? log = null)
		{
			if (requestHeaders.method == null || requestHeaders.method.ToUpper() != "GET") // method should already not be null here.
			{
				clientSocket.SendError(405, "The requested file \"" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } }, log: log);
				return true;
			}
			if (preferredPath != null && requestHeaders.url != preferredPath)
			{
				clientSocket.SendRedirectResponse(308, preferredPath, log: log);
				return true;
			}
			return false;
		}
	}
}