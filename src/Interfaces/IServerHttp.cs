using System.Net;
using System.Net.Sockets;

namespace ServerHttp.Interfaces
{
    public interface IServerHttp
    {
        TcpListener SocketListener { get; }
        IPAddress IP { get; }
        int Port { get; }
        void StartServer(int port);
    }
}

