using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using CommonMark;
using System.Text;

namespace WebsiteProxy
{
	public static class TemplateLoader
	{
		public static string Render(string path, Dictionary<string, object>? parameters, Log? log = null)
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
			string rendered = template.Render(templateContext);
			if (log != null)
			{
				log.Add("Rendered", LogColor.Data);
			}
			return rendered;
		}

		class MyTemplateLoader : ITemplateLoader
		{
			public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) // TODO: Adapt for relative paths.
			{
				return Path.Combine(Util.currentDirectory, templateName.TrimStart('/', '\\'));
			}

			public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
			{
				if (!Util.IsInCurrentDirectory(templatePath))
				{
					throw new ArgumentException("The file \"" + templatePath + "\" is not in the working directory.");
				}
				if (!File.Exists(templatePath))
				{
					throw new ArgumentException("The file \"" + templatePath + "\" does not exist.");
				}
				if (new string[] { ".txt", ".md" }.Contains(new FileInfo(templatePath).Extension.ToLower()))
				{
					return CommonMarkConverter.Convert(File.ReadAllText(templatePath, Encoding.UTF8));
				}
				return File.ReadAllText(templatePath, Encoding.UTF8);
			}

			public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
			{
				throw new NotImplementedException();
			}
		}
	}
}
