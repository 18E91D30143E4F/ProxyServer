
namespace Proxy_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            ProxyServer proxyServer = new ProxyServer("127.0.0.1", 45071);
            proxyServer.Start();
        }
    }
}
