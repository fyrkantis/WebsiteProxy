using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Website
	{
		static Route[] routes =
		{
			new Route("test", new string[] { "GET" }, (clientSocket, requestHeaders, responseHeaders) =>
			{
				clientSocket.SendBodyResponse(";)", responseHeaders);
			})
		};

		class Route
		{
			public string name;
			public string[] methods;
			public Action<Socket, RequestHeaders, ResponseHeaders> Action;

			public Route(string routeName, string[] routeMethods, Action<Socket, RequestHeaders, ResponseHeaders> RouteAction)
			{
				name = routeName;
				methods = routeMethods;
				Action = RouteAction;
			}
		}

		public static async void HandleConnection(Socket clientSocket, RequestHeaders requestHeaders)
		{
			MyConsole.color = ConsoleColor.DarkYellow;
			MyConsole.WriteMany(requestHeaders.method, requestHeaders.url);

			// Writes the raw request headers.
			/*if (requestHeaders.raw != null)
			{
				MyConsole.color = ConsoleColor.DarkGray;
				MyConsole.WriteLine(Encoding.ASCII.GetString(requestHeaders.raw));
			}/**/

			if (requestHeaders.url == null)
			{
				clientSocket.SendError(400, "No requested URL was specified.");
				return;
			}
			if (requestHeaders.method == null)
			{
				clientSocket.SendError(400, "No requested method was specified.");
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
					foreach (string method in route.methods)
					{
						if (requestHeaders.method.ToUpper() == method)
						{
							string preferredPath = ("/" + shortPath).TrimEnd('/') + "/";
							if (requestHeaders.url != preferredPath)
							{
								clientSocket.SendRedirectResponse(301, preferredPath);
								return;
							}
							route.Action(clientSocket, requestHeaders, new ResponseHeaders());
							return;
						}
					}
					clientSocket.SendError(405, "The route \"/" + basePath + "\" only accepts " + Util.GrammaticalListing(route.methods) + " requests.", new Dictionary<string, object>() { { "Allow", string.Join(", ", route.methods) } });
					return;
				}
			}

			// Checks if the url path matches a git repository.
			if (!string.IsNullOrWhiteSpace(basePath) && Directory.Exists(Path.Combine(Util.currentDirectory, "websites", basePath)))
			{
				// Tries to load as an asset file.
				if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "websites\\", shortPath), "/" + shortPath))
				{
					return;
				}
				// Tries to load as a html page (but not as template).
				foreach (string pathAlternative in new string[] { shortPath + ".html", Path.Combine(shortPath, "index.html") })
				{
					if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "websites\\", pathAlternative), "/" + shortPath + "/"))
					{
						return;
					}
				}
				// Gives up.
				clientSocket.SendError(404, "The requested file \"" + shortPath.Remove(0, basePath.Length) + "\" could not be found in /" + basePath + ".");
				return;
			}

			// Tries to load as an asset file.
			if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "assets\\", shortPath), "/" + shortPath))
			{
				return;
			}

			// Tries to load as a html page (as template).
			foreach (string pathAlternative in new string[] { shortPath + ".html", Path.Combine(shortPath, "index.html") })
			{
				if (clientSocket.TryLoad(requestHeaders, Path.Combine(Util.currentDirectory, "pages\\", pathAlternative), ("/" + shortPath).TrimEnd('/') + "/", template: true, parameters: new Dictionary<string, object> { { "navbarButtons", Util.navbarButtons } }))
				{
					return;
				}
			}

			clientSocket.SendError(404, "The requested file \"" + shortPath + "\" could not be found.");
			//context.Send(418, "I'm a teapot", "And I can't be asked to brew coffee.");
		}

		// Returns true if the page or an http error was sent, or false otherwise.
		public static bool TryLoad(this Socket clientSocket, RequestHeaders requestHeaders, string path, string? preferredPath = null, bool template = false, Dictionary<string, object>? parameters = null)
		{
			if (!File.Exists(path))
			{
				return false;
			}
			if (requestHeaders.method == null || requestHeaders.method.ToUpper() != "GET") // method should already not be null here.
			{
				clientSocket.SendError(405, "The requested file \"" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } });
				return true;
			}
			ResponseHeaders responseHeaders = new ResponseHeaders();
			if (preferredPath != null && requestHeaders.url != preferredPath)
			{
				clientSocket.SendRedirectResponse(301, preferredPath);
				return true;
			}
			if (template)
			{
				clientSocket.SendPageResponse(path, responseHeaders, parameters);
			}
			else
			{
				clientSocket.SendFileResponse(path, responseHeaders);
			}
			return true;
		}
	}
}