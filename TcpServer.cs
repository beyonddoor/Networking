using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using SolarGames.Networking.Crypting;

namespace SolarGames.Networking
{
    /// <summary>
    /// 维护若干的connection
    /// </summary>
    public class TcpServer : IDisposable
    {
        public delegate void DOnNotAuthorizedPacket(TcpServer server, TcpConnection connection, IPacket packet);
        public event DOnNotAuthorizedPacket OnNotAuthorizedPacket;

        Socket serverSocket;
        internal List<TcpConnection> connections = new List<TcpConnection>();
        
        public string Host { get; private set; }
        public int Port { get; private set; }
        public int BufferSize { get; private set; }
        public int AcceptThreads { get; private set; }
        
        public TcpConnection[] Connections
        {
            get
            {
                lock (connections)
                    return connections.ToArray();
            }
        }

        public Type CipherType { get; private set; }

        public TcpServer(string host, int port, Type cipherType)
            : this(host, port)
        {
            CipherType = cipherType;
        }

        public TcpServer(string host, int port)
            : this(port)
        {
            if (!String.IsNullOrEmpty(host))
                this.Host = host;
        }

        public TcpServer(int port)
        {
            this.Port = port;
            BufferSize = 1024;
            AcceptThreads = 2;
        }

        public TcpConnection GetConnection(Func<TcpConnection, bool> predicate)
        {
            lock (connections)
                return connections.FirstOrDefault(predicate);
        }

        public void Close()
        {
            serverSocket?.Close();
            ClearConnections();
        }

        public void Dispose()
        {
            Close();
        }

        internal void OnNotAuthorizedPacketInternal(TcpConnection conn, IPacket packet)
        {
            OnNotAuthorizedPacket?.Invoke(this, conn, packet);
        }

        public void ClearConnections()
        {
            lock (connections)
            {
                foreach (var t in connections)
                {
                    t.Close();
                }

                connections.Clear();
            }
        }

        public void Listen(int backlog)
        {
            IPEndPoint myEndpoint = new IPEndPoint(IPAddress.Any, Port);

            if (!string.IsNullOrEmpty(this.Host))
                myEndpoint = new IPEndPoint(IPAddress.Parse(this.Host), Port);

            serverSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            serverSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            serverSocket.Blocking = false;
            serverSocket.Bind(myEndpoint);
            serverSocket.Listen(backlog);

            for (int i = 0; i < AcceptThreads; i++)
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        void AcceptCallback(IAsyncResult result)
        {
            try
            {
                // Finish Accept
                Socket connSocket = serverSocket.EndAccept(result);
                connSocket.Blocking = false;
                var connection = new TcpConnection(this, connSocket, BufferSize, CipherType);
                lock (connections) 
                    connections.Add(connection);

                System.Diagnostics.Debug.WriteLine("New connection from " + connection.ToString());

                // Start new Accept
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback),
                    result.AsyncState);

            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine("TCPServer.AcceptCallback() Exception: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}

