using Squirrel;
using System.Net;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Program
	{
		public static void Main()
		{
			// Refreshes all internal git repositories.
			// TODO: https://stackoverflow.com/a/36437100/13347795.
			foreach (string directory in Directory.GetDirectories(Path.Combine(Util.currentDirectory, "websites")))
			{
				MyConsole.color = ConsoleColor.Magenta;
				MyConsole.Write(new DirectoryInfo(directory).Name + " ");

				// Fixed by our lord and savior AaRNOTT https://github.com/Squirrel/Squirrel.Windows/issues/1649#issuecomment-932853326.
				using (UpdateManager manager = new UpdateManager(directory))
				{
					manager.UpdateApp();
				}
			}
			MyConsole.WriteLine();

			// Certificate setup: https://stackoverflow.com/a/33905011.
			// Note to self: Certbot makes certificates, openssl combines certificate and key to .pfx file,
			// wich is loaded in with Windows MMC, and then bound to app with netsh http add sslcert. Phew!

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 80);

			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(endPoint);

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

			socket.Listen(10); // Starts listening on port with a max queue of 10.
			while (true)
			{

				Socket clientSocket = socket.Accept();
				MyConsole.WriteTimestamp(clientSocket.RemoteEndPoint);
				clientSocket.ReceiveTimeout = 2000;
				RequestHeaders? requestHeaders = RequestHeaders.ReadFromSocket(clientSocket);
				if (requestHeaders == null)
				{
					continue;
				}
#if DEBUG
				Website.HandleConnection(clientSocket, requestHeaders); // Handles connection synchronously.
#else
				Task.Run(() => Website.HandleConnection(clientSocket, requestHeaders)); // Handles connection asynchronously.
#endif
			}
		}
	}
}