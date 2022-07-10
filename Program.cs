using System.Net;

namespace WebsiteProxy
{
	public static class Program
	{
		public static void Main()
		{
			// Certificate setup: https://stackoverflow.com/a/33905011
			// Note to self: Certbot makes certificates, openssl combines certificate and key to .pfx file,
			// wich is loaded in with Windows MMC, and then bound to app with netsh http add sslcert. Phew!
			HttpListener listener = new HttpListener();
			listener.Prefixes.Add("https://+:443/");
			listener.Start();
			listener.Listen();
		}

		public static void Listen(this HttpListener listener)
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Server running...");

#if DEBUG
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(" (Debug mode enabled)");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("DO NOT USE DEBUG MODE IN PRODUCTION!");
#else
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(" (Debug mode disabled)");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("Run in debug mode for logging.");
#endif
			Console.ForegroundColor = ConsoleColor.White;
			while (true)
			{
				
				HttpListenerContext context = listener.GetContext();
#if DEBUG
				context.HandleConnection(); // Handles connection synchronously.
#else
				Task.Run(() => context.HandleConnection()); // Handles connection asynchronously.
#endif
			}
		}
	}
}