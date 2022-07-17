using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using System.Text;

namespace WebsiteProxy
{
	public static class TemplateLoader
	{
		public static string Render(string path, Dictionary<string, object>? parameters)
		{
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

			Template template = Template.Parse(File.ReadAllText(path, Encoding.UTF8));
			return template.Render(templateContext);
		}

		class MyTemplateLoader : ITemplateLoader
		{
			public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) // TODO: Adapt for relative paths.
			{
				return Path.Combine(Util.currentDirectory, "templates", templateName.TrimStart('/', '\\'));
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
