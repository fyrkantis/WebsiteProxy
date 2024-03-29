﻿using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WebsiteProxy
{
	public static class Authenticator
	{
		// https://stackoverflow.com/a/1345402/13347795
		static X509Certificate2 certificate = new X509Certificate2(Util.environment["certificatePath"], Util.environment["certificatePassword"], X509KeyStorageFlags.MachineKeySet);
		
		// https://stackoverflow.com/a/6742137/13347795

		// https://stackoverflow.com/a/41864048/13347795
		// https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream

		public static bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
			if (sslPolicyErrors == SslPolicyErrors.None)
			{
				return true;
			}

			Log.Write("Certificate error:" + sslPolicyErrors, LogColor.Error);
			return false;
		}

		public static SslStream GetSslStream(Socket socket, Log? log = null)
		{
			Stream networkStream = new NetworkStream(socket);
			SslStream sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(ValidateCertificate), null);

			// https://stackoverflow.com/a/55316144/13347795
			sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls12, true);
			sslStream.ReadTimeout = 5000;
			sslStream.WriteTimeout = 5000;

			string messageData = sslStream.ReadMessage();
			if (log != null)
			{
				log.Add(messageData, LogColor.Data);
			}

			// Write a message to the client.
			byte[] message = Encoding.UTF8.GetBytes("Hello from the server.<EOF>");
			sslStream.Write(message);
			sslStream.Close();
			socket.Close();

			return sslStream;
		}

		static string ReadMessage(this SslStream sslStream)
		{
			// Read the  message sent by the client.
			// The client signals the end of the message using the
			// "<EOF>" marker.
			byte[] buffer = new byte[2048];
			StringBuilder messageData = new StringBuilder();
			int bytes = -1;
			do
			{
				// Read the client's test message.
				bytes = sslStream.Read(buffer, 0, buffer.Length);

				// Use Decoder class to convert from bytes to UTF8
				// in case a character spans two buffers.
				Decoder decoder = Encoding.UTF8.GetDecoder();
				char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
				decoder.GetChars(buffer, 0, bytes, chars, 0);
				messageData.Append(chars);
				// Check for EOF or an empty message.
				if (messageData.ToString().IndexOf("<EOF>") != -1)
				{
					break;
				}
			} while (bytes != 0);

			return messageData.ToString();
		}
	}
}