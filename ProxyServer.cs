using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace Proxy_Server
{
    class ProxyServer
    {
        private IPAddress localHost;
        private int TCP_PORT;
        private string errorPagePath = "ErrorPage.html";

        private static CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private static CancellationToken token = cancelTokenSource.Token;

        public ProxyServer(string host, int port)
        {
            this.localHost = IPAddress.Parse(host);
            this.TCP_PORT = port;
        }

        public void Start()
        {
            TcpListener tcpListener = null;
            try
            {
                tcpListener = new TcpListener(localHost, TCP_PORT);
                tcpListener.Start();

                while (!token.IsCancellationRequested)
                {
                    Socket client = tcpListener.AcceptSocket();
                    Task.Run(() => ClientRequestProcessing(client));
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        private void ClientRequestProcessing(Socket client)
        {
            try
            {
                NetworkStream clientStream = new NetworkStream(client);
                byte[] clientRequest;
                int clientRequestLength;
                (clientRequest, clientRequestLength) = ReadStream(clientStream);

                // Get Host name, ip
                Http httpClientRequest = new Http(Encoding.UTF8.GetString(clientRequest));

                if (httpClientRequest.host != null && IsBlackList(httpClientRequest.host.HostName))
                {
                    ProcessBlackListRequest(clientStream, httpClientRequest.host.HostName);
                    throw new Exception();
                }

                // Create server variable
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverHost = new IPEndPoint(httpClientRequest.ip, httpClientRequest.port);

                server.Connect(serverHost);

                NetworkStream serverStream = new NetworkStream(server);
                clientRequest = Encoding.UTF8.GetBytes(httpClientRequest.modifiedRequest);

                // Send Modified Request to Server
                serverStream.Write(clientRequest, 0, clientRequest.Length);

                byte[] serverResponse;
                int serverResponseLength;

                // Get data from reply
                (serverResponse, serverResponseLength) = ReadStream(serverStream);
                clientStream.Write(serverResponse, 0, serverResponseLength);

                Console.WriteLine(GetResponse(Encoding.UTF8.GetString(serverResponse), httpClientRequest.host.HostName));

                serverStream.CopyTo(clientStream);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
            }
            finally
            {
                client.Close();
            }
        }

        private static string GetResponse(string response, string requestUri)
        {
            string[] lines = response.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string preparedResponse = "";

            preparedResponse = lines[0].Substring(lines[0].IndexOf(" "));

            if (preparedResponse != "")
            {
                return $"Responce to {requestUri}\nStatus: {preparedResponse}\n";
            }
            else
            {
                return null;
            }
        }

        // Return writen buffer and number of bytes read
        private (byte[], int) ReadStream(NetworkStream stream)
        {
            const int BUFFER_SIZE = 1024;

            byte[] myReadBuffer = new byte[BUFFER_SIZE];
            byte[] data = new byte[10 * BUFFER_SIZE];
            int numberOfBytesRead = 0;

            try
            {
                if (stream.CanRead)
                {
                    do
                    {
                        int bytes = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
                        Array.Copy(myReadBuffer, 0, data, numberOfBytesRead, bytes);
                        numberOfBytesRead += bytes;
                    }
                    while (stream.DataAvailable && numberOfBytesRead < data.Length);
                }
                else
                {
                    Console.WriteLine("Sorry.  You cannot read from this NetworkStream.");
                }

                return (data, numberOfBytesRead);
            }
            catch (System.IO.IOException)
            {
                throw new SocketException();
            }
        }

        private bool IsBlackList(string hostname)
        {
            using (StreamReader reader = new StreamReader("Blocked_Sites.txt"))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(hostname))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void ProcessBlackListRequest(NetworkStream clientStream, string hostName)
        {
            byte[] errorResponse = LoadErrorPage();
            clientStream.Write(errorResponse, 0, errorResponse.Length);

            string response = $"Responce to {hostName}\nStatus: 403 Forbidden\n";
            Console.WriteLine(response);
        }

        private byte[] LoadErrorPage()
        {
            using (FileStream fs = new FileStream(errorPagePath, FileMode.Open))
            {
                byte[] page = new byte[fs.Length];
                fs.Read(page, 0, page.Length);

                string header = "HTTP/1.1 403 Forbidden\r\nContent-Type: text/html\r\nContent-Length: "
                            + page.Length + "\r\n\r\n";

                byte[] fullData = new byte[header.Length + page.Length];
                Array.Copy(Encoding.UTF8.GetBytes(header), 0, fullData, 0, Encoding.UTF8.GetBytes(header).Length);
                Array.Copy(page, 0, fullData, Encoding.UTF8.GetBytes(header).Length, page.Length);

                return fullData;
            }
        }
    }
}
