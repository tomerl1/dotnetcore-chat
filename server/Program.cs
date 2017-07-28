using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace server
{
    public class Program
    {
        private static object _lock = new object();

        private static Dictionary<Guid, TcpClient> _clientsPool = new Dictionary<Guid, TcpClient>();

        public static void Main(string[] args)
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var port = 5656;
            var listener = new TcpListener(ip, port);

            listener.Start();
            System.Console.WriteLine($"Server listening on: {ip}:{port}");

            // await connections
            while (true)
            {
                var client = listener.AcceptTcpClientAsync().Result;

                // handle new client connection
                var clientThread = new Thread((tcpClient) => HandleClientConnection((TcpClient)tcpClient));
                clientThread.Start(client);
            }
        }

        public static void HandleClientConnection(TcpClient client)
        {
            var clientId = Guid.NewGuid();
            System.Console.WriteLine($"New client connected [{clientId.ToString("N")}]");

            lock (_lock)
            {
                _clientsPool.Add(clientId, client);
            }

            BroadcastMessage($"{clientId} has connected.");

            var stream = client.GetStream();
            var reader = new BinaryReader(stream, Encoding.UTF8);

            var bEof = false;
            while (!bEof)
            {
                try
                {
                    var message = reader.ReadString();
                    BroadcastMessage($"{clientId}: {message}");
                }
                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        _clientsPool.Remove(clientId);
                    }

                    reader.Dispose();
                    stream.Dispose();

                    bEof = true;
                    BroadcastMessage($"{clientId} has disconnected.");
                }
            }
        }

        private static void BroadcastMessage(string message)
        {
            System.Console.WriteLine("[Broadcast] " + message);
            lock (_lock)
            {
                foreach (var kvp in _clientsPool)
                {
                    var client = kvp.Value;
                    if (client.Connected)
                    {
                        var stream = client.GetStream();
                        var writer = new BinaryWriter(stream, Encoding.UTF8);
                        writer.Write(message);
                        writer.Flush();
                    }
                }
            }
        }
    }
}
