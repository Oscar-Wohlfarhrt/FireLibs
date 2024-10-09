using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Web.TCP
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public TcpClient Client { get; private set; }
        public IPAddress? LocalIp { get; private set; }
        public IPAddress? RemoteIp { get; private set; }
        public ClientConnectedEventArgs(TcpClient client)
        {
            Client = client;
            LocalIp = ((IPEndPoint?)client.Client.LocalEndPoint)?.Address;
            RemoteIp = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address;
        }
    }
    public class TcpServer
    {
        public event EventHandler<ClientConnectedEventArgs>? OnClientConnected;

        TcpListener server;
        IAsyncResult? asyncRes = null;
        public bool IsOpen { get; private set; } = false;

        public TcpServer(IPAddress ip, int port)
        {
            server = new TcpListener(ip, port);
        }
        public void Start()
        {
            server.Start();
            IsOpen = true;
            asyncRes = server.BeginAcceptTcpClient(ReciveClient, server);
        }
        public void Stop()
        {
            IsOpen = false;
            if (asyncRes != null && !asyncRes.IsCompleted)
                server.EndAcceptTcpClient(asyncRes);
            server.Stop();
        }

        public void ReciveClient(IAsyncResult ar)
        {
            TcpListener server = (TcpListener)(ar.AsyncState ?? this.server);
            TcpClient cli = server.EndAcceptTcpClient(ar);

            OnClientConnected?.Invoke(this, new(cli));

            if (IsOpen)
                asyncRes = server.BeginAcceptTcpClient(ReciveClient, server);
        }
    }
}
