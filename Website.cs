using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Web;

namespace WebsiteProxy
{
	public static class Website
	{
		public static Route[] routes =
		{
			new Route("test", new string[] { "GET" }, (clientSocket, requestHeaders, responseHeaders) =>
			{
				clientSocket.SendBodyResponse(";)", responseHeaders);
			})
		};

		public static async void HandleConnection(Socket clientSocket, RequestHeaders requestHeaders)
		{
			MyConsole.WriteTimestamp();
			MyConsole.WriteMany(clientSocket.RemoteEndPoint);

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
			string shortPath = requestHeaders.url.Trim('/').Replace(".html", null).Replace("index", null, true, null);

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
							responseHeaders.SetPreferredRedirect(requestHeaders.url, "/" + shortPath.TrimEnd('/') + "/");
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

			clientSocket.SendResponse(404, "The requested file \"" + shortPath + "\" could not be found.");/*

			// Checks if the url path matches a html file name.
			foreach (string pathAlternative in new string[] { convertedPath + ".html", Path.Combine(convertedPath, "index.html") })
			{
				if (File.Exists(Path.Combine(Util.currentDirectory, "pages\\", pathAlternative)))
				{
					if (context.Request.HttpMethod.ToUpper() != "GET")
					{
						context.Send(405, "Method Not Allowed", "The page at \"" + context.Request.Url.LocalPath + "\" is static and can only be loaded with GET requests.");
						return;
					}
					context.SetPreferredRedirect(shortPath + "/");
					context.SendHtmlFile("pages\\" + pathAlternative);
					return;
				}
			}

			clientSocket.SendResponse(404, "The requested file \"" + shortPath + "\" could not be found.");
			//context.Send(418, "I'm a teapot", "And I can't be asked to brew coffee.");*/
		}

		/*public static void SendHtmlFile(this HttpListenerContext context, string relativePath, Dictionary<string, object>? parameters = null) // TODO: Add error handling for scriban syntax errors.
		{
			string absolutePath = Path.Combine(Util.currentDirectory, relativePath);
			context.Response.AddHeader("Content-Disposition", "inline; filename = \"" + Path.GetFileName(absolutePath) + "\"");

			ScriptObject script = new ScriptObject(); // Used for sending arguments to html template.
			if (parameters != null)
			{
				foreach (KeyValuePair<string, object> parameter in parameters)
				{
					script.Add(parameter.Key, parameter.Value);
				}
			}
			TemplateContext templateContext = new TemplateContext();
			templateContext.TemplateLoader = new MyTemplateLoader();
			templateContext.PushGlobal(script);

			Template template = Template.Parse(File.ReadAllText(absolutePath, Encoding.UTF8));
			context.SendBody(template.Render(templateContext));
			context.Response.Close();
		}*/

		public class Route
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
		public class MyTemplateLoader : ITemplateLoader
		{
			public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) // TODO: Adapt for relative paths.
			{
				return Path.Combine(Util.currentDirectory, templateName.Replace('/', '\\').TrimStart('\\'));
			}

			public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
			{
				return File.ReadAllText(templatePath, Encoding.UTF8);
			}

			public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
			{
				throw new NotImplementedException();
			}
		}
	}
}