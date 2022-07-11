using System.Net;
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
			MyConsole.WriteTimestamp(clientSocket.RemoteEndPoint);
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
				clientSocket.SendResponse(400, "No requested URL was specified.");
				return;
			}
			if (requestHeaders.method == null)
			{
				clientSocket.SendResponse(400, "No requested method was specified.");
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
							ResponseHeaders responseHeaders = new ResponseHeaders();
							responseHeaders.SetPreferredRedirect(requestHeaders.url, ("/" + shortPath).TrimEnd('/') + "/");
							route.Action(clientSocket, requestHeaders, responseHeaders);
							return;
						}
					}
					clientSocket.SendResponse(405, "The route \"/" + basePath + "\" only accepts " + Util.GrammaticalListing(route.methods) + " requests.", new Dictionary<string, object>() { { "Allow", string.Join(", ", route.methods) } });
					return;
				}
			}

			// Checks if the url path matches an asset file name.
			string assetPath = Path.Combine(Util.currentDirectory, "assets\\", shortPath); // Absolute path to file in the asset folder.
			if (File.Exists(assetPath))
			{
				if (requestHeaders.method.ToUpper() != "GET")
				{
					clientSocket.SendResponse(405, "The file at \"" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } });
					return;
				}
				ResponseHeaders responseHeaders = new ResponseHeaders();
				responseHeaders.SetPreferredRedirect(requestHeaders.url, "/" + shortPath);
				clientSocket.SendFileResponse(assetPath, responseHeaders);
				return;
			}

			// Checks if the url path matches a html file name.
			foreach (string pathAlternative in new string[] { shortPath + ".html", Path.Combine(shortPath, "index.html") })
			{
				string pagePath = Path.Combine(Util.currentDirectory, "pages\\", pathAlternative);
				if (File.Exists(pagePath))
				{
					if (requestHeaders.method.ToUpper() != "GET")
					{
						clientSocket.SendResponse(405, "The page at \"" + requestHeaders.url + "\" is static and can only be loaded with GET requests.", new Dictionary<string, object>() { { "Allow", "GET" } });
						return;
					}
					ResponseHeaders responseHeaders = new ResponseHeaders();
					responseHeaders.SetPreferredRedirect(requestHeaders.url, ("/" + shortPath).TrimEnd('/') + "/");
					clientSocket.SendPageResponse(pagePath, responseHeaders);
					return;
				}
			}

			clientSocket.SendResponse(404, "The requested file \"" + shortPath + "\" could not be found.");
			//context.Send(418, "I'm a teapot", "And I can't be asked to brew coffee.");
		}
	}
}