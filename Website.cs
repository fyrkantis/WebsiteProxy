﻿using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Website
	{
		static Route[] routes =
		{
			new Route("github", new string[] { "POST" }, (clientSocket, requestHeaders, log) =>
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
				string path = Path.Combine(Util.currentDirectory, "websites", name);
				if (!Directory.Exists(path))
				{
					clientSocket.SendError(404, "The repository \"" + name + "\" does not exist on this server.", log: log);
					return;
				}
				try
				{
					GitApi.Pull(path, log);
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
			new Route("formtest", new string[] { "POST" }, (clientSocket, requestHeaders, log) =>
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
			new Route("test", new string[] { "GET" }, (clientSocket, requestHeaders, log) =>
			{
				clientSocket.SendBodyResponse(";)", log);
			})
		};

		class Route
		{
			public string name;
			public string[] methods;
			public Action<Socket, RequestHeaders, Log?> Action;

			public Route(string routeName, string[] routeMethods, Action<Socket, RequestHeaders, Log?> RouteAction)
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

			// Checks if the url path matches a pre-defined path.
			string basePath = shortPath.Split('/', 2)[0].ToLower();
			foreach (Route route in routes)
			{
				if (route.name == basePath)
				{
					string preferredPath = ("/" + shortPath).TrimEnd('/') + "/";
					if (requestHeaders.url != preferredPath)
					{
						clientSocket.SendRedirectResponse(308, preferredPath, log: log);
						return;
					}
					foreach (string method in route.methods)
					{
						if (requestHeaders.method.ToUpper() == method)
						{
							route.Action(clientSocket, requestHeaders, log);
							return;
						}
					}
					clientSocket.SendError(405, "The route \"/" + basePath + "/\" only accepts " + Util.GrammaticalListing(route.methods) + " requests.", new Dictionary<string, object>() { { "Allow", string.Join(", ", route.methods) } }, log: log);
					return;
				}
			}

			// Checks if the url path matches a git repository.
			if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(Path.Combine(Util.currentDirectory, "websites", basePath)))
			{
				// Tries to load as an asset file.
				if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "websites", shortPath), "/" + shortPath, log: log))
				{
					return;
				}
				// Tries to load as a html page (but not as template).
				foreach (string pathAlternative in new string[] { shortPath + ".html", Path.Combine(shortPath, "index.html") })
				{
					if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "websites", pathAlternative), "/" + shortPath + "/", log: log))
					{
						return;
					}
				}
				// Gives up.
				clientSocket.SendError(404, "The requested file \"" + shortPath.Remove(0, basePath.Length) + "\" could not be found in /" + basePath + "/.", log: log);
				return;
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
				clientSocket.SendError(405, "The requested file \"/" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } }, log: log);
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