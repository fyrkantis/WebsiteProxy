using System.Net;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Program
	{
		public static void Main()
		{
			// Certificate setup: https://stackoverflow.com/a/33905011
			// Note to self: Certbot makes certificates, openssl combines certificate and key to .pfx file,
			// wich is loaded in with Windows MMC, and then bound to app with netsh http add sslcert. Phew!

			IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
			IPEndPoint endPoint = new IPEndPoint(ipAddress, 80);

			Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(endPoint);

			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("Server running on {0}...", ipAddress);

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
				RequestHeaders requestHeaders = RequestHeaders.ReadFromSocket(clientSocket);
#if DEBUG
				Website.HandleConnection(clientSocket, requestHeaders); // Handles connection synchronously.
#else
				Task.Run(() => Website.HandleConnection(clientSocket, requestHeaders)); // Handles connection asynchronously.
#endif
			}
		}
	}
}