using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FireLibs.Web.TCP
{
    public class SocketConnectedEventArgs : EventArgs
    {
        public Socket Client { get; private set; }
        public EndPoint? LocalIp { get; private set; }
        public EndPoint? RemoteIp { get; private set; }
        public SocketConnectedEventArgs(Socket client)
        {
            Client = client;
            LocalIp = client.LocalEndPoint;
            RemoteIp = client.RemoteEndPoint;
        }
    }
    public class SocketServer
    {
        public event EventHandler<SocketConnectedEventArgs>? OnSocketConnected;

        Socket server;
        IAsyncResult? asyncRes = null;

        public SocketServer(SocketType type, ProtocolType protocol, EndPoint endPoint)
        {
            server = new Socket(type,protocol);
            server.Bind(endPoint);
        }
        public void Start()
        {
            server.Listen();
            asyncRes = server.BeginAccept(ReciveClient, server);
        }
        public void Stop()
        {
            if (asyncRes != null && !asyncRes.IsCompleted)
                server.EndAccept(asyncRes);
            server.Close();
        }

        public void ReciveClient(IAsyncResult ar)
        {
            Socket server = (Socket)(ar.AsyncState ?? this.server);
            Socket cli = server.EndAccept(ar);

            OnSocketConnected?.Invoke(this, new(cli));

            asyncRes = server.BeginAccept(ReciveClient, server);
        }
    }
}
