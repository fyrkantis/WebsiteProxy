using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace WebsiteProxy
{
	public static class Authenticator
	{
		static X509Certificate2 certificate = new X509Certificate2();

		// https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream
		public static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (sslPolicyErrors == SslPolicyErrors.None)
			{
				return true;
			}

			MyConsole.WriteTimestamp();
			MyConsole.color = ConsoleColor.Red;
			MyConsole.WriteLine("Certificate error:" + sslPolicyErrors);
			return false;
		}

		public static SslStream GetSslStream(Socket socket)
		{
			Stream networkStream = new NetworkStream(socket);
			SslStream sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateCertificate), null);

			// https://stackoverflow.com/a/55316144/13347795
			sslStream.AuthenticateAsServer(certificate, false, SslProtocols.Tls12, false);

			return sslStream;
		}
	}
}
