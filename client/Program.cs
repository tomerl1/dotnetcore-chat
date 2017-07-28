using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var port = 5656;

            // connect to server
            var client = new TcpClient();
            System.Console.WriteLine($"Connecting to: {ip}:{port}");
            client.ConnectAsync(ip, port);
            System.Console.WriteLine("Connected");

            // listen to messages
            var waitForMessagesThread = new Thread((tcpClient) => ReadMessages((TcpClient)tcpClient));
            waitForMessagesThread.Start(client);

            // read input from client & send to server
            var bBye = false;

            var stream = client.GetStream();
            var writer = new BinaryWriter(stream, Encoding.UTF8);

            try
            {
                while (!bBye)
                {
                    var message = Console.ReadLine();
                    if (message == "/quit")
                    {
                        bBye = true;
                        writer.Dispose();
                        stream.Dispose();
                        client.Dispose();
                        continue;
                    }

                    writer.Write(message);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                writer.Dispose();
                stream.Dispose();
            }
        }

        public static void ReadMessages(TcpClient client)
        {
            var stream = client.GetStream();
            var reader = new BinaryReader(stream, Encoding.UTF8);
            try
            {
                while (true)
                {
                    var message = reader.ReadString();
                    System.Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                reader.Dispose();
                stream.Dispose();
            }
        }
    }
}
