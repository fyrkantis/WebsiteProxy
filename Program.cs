using System.Net;
using System.Net.Sockets;

namespace WebsiteProxy
{
	public static class Program
	{
		public static void Main()
		{
			// Refreshes all internal git repositories.
			List<Task> pullTasks = new List<Task>();
			foreach (string directory in Directory.GetDirectories(Path.Combine(Util.currentDirectory, "repositories")))
			{
				Task task = new Task(() =>
				{
					Log log = new Log();
					GitApi.Pull(directory, log);
					log.Write();
				});
				task.Start();
				pullTasks.Add(task);
			}

			if (!Task.WhenAll(pullTasks).Wait(5000))
			{
				Log.Write("Some pull processes failed.", LogColor.Error);
			}
			Log.Write();

			// Certificate setup: https://stackoverflow.com/a/33905011
			// Note to self: Certbot makes certificates, openssl combines certificate and key to .pfx file,
			// wich is loaded in with Windows MMC, and then bound to app with netsh http add sslcert. Phew!

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 80);

			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(endPoint);
			socket.Listen(10); // Starts listening on port with a max queue of 10.

			Log log = new Log();
			log.Add("Server running...");
#if DEBUG
			log.Add("(Debug mode enabled)", LogColor.Success);
			log.secondRow = new LogPart("DO NOT USE DEBUG MODE IN PRODUCTION!", LogColor.Error);
#else
			log.Add("(Debug mode disabled)", LogColor.Error);
#endif
			log.Write(writeTimeTaken: false);
			Log.WriteRestartTime();

			while (true)
			{

				Socket clientSocket = socket.Accept();
				log = new Log(true, clientSocket.RemoteEndPoint);
				clientSocket.ReceiveTimeout = 2000;
				RequestHeaders? requestHeaders = RequestHeaders.ReadFromSocket(clientSocket, log);
				if (requestHeaders == null)
				{
					continue;
				}
#if DEBUG
				Website.HandleConnection(clientSocket, requestHeaders, log); // Handles connection synchronously.
#else
				Task.Run(() => Website.HandleConnection(clientSocket, requestHeaders, log)); // Handles connection asynchronously.
#endif
			}
		}
	}
}