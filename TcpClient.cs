using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using SolarGames.Networking.Crypting;

namespace SolarGames.Networking
{
    /// <summary>
    /// tcpclient的实现，基于System.Net.Sockets.TcpClient
    /// 读写、连接都是异步的
    /// </summary>
    public class TcpClient : IDisposable
    {
        public enum TcpClientStatus
        {
            Disconnected,
            Connecting,
            Connected
        }
        
        public delegate void DOnError(TcpClient tcpClient, SocketException ex);
        public delegate void DOnIncomingPacket(IPacket packet);
        public delegate void DOnConnect(TcpClient tcpClient);
        public delegate void DOnDisconnect(TcpClient tcpClient);
        
        public ICipher Cipher { get; set; }
        public event DOnError OnError;
        public event DOnIncomingPacket OnIncomingPacket;
        public event DOnConnect OnConnect;
        public event DOnDisconnect OnDisconnect;

        System.Net.Sockets.TcpClient tcpClient;
        byte[] buffer;
        byte[] tempBuffer;
        bool blocking;

        AutoResetEvent connectEvent;

        const int DefaultBufferSize = ushort.MaxValue * 2;

        public bool Connected
        {
            get
            {
                lock (tcpClient)
                {
                    return tcpClient?.Client != null && tcpClient.Connected;
                }
            }
        }

        public TcpClientStatus Status { get; private set; }

        public Socket Socket => tcpClient.Client;

        public int ReceiveBufferSize
        {
            get
            {
                lock (tcpClient)
                    return tcpClient.ReceiveBufferSize;
            }
            set
            {
                lock (tcpClient)
                    tcpClient.ReceiveBufferSize = value;
            }
        }
        public int SendBufferSize
        {
            get
            {
                lock (tcpClient)
                    return tcpClient.SendBufferSize;
            }
            set
            {
                lock (tcpClient)
                    tcpClient.SendBufferSize = value;
            }
        }

        /// <summary>
        /// 异步连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void ConnectAsync(string host, int port)
        {
            ReInitSocket();
            tcpClient.BeginConnect(host, port, ConnectCallback, null);
            Status = TcpClientStatus.Connecting;
        }

        /// <summary>
        /// 同步连接，等待连接事件
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Connect(string host, int port)
        {
            ConnectAsync(host, port);
            connectEvent.Reset();
            connectEvent.WaitOne();
        }

        public void Close()
        {
            Status = TcpClientStatus.Disconnected;

            if (tcpClient?.Client == null || !tcpClient.Connected)
                return;

            OnDisconnectInternal();
        }

        void OnErrorInternal(SocketException ex)
        {
            Status = TcpClientStatus.Disconnected;

            lock (tcpClient)
            {
                tcpClient.Close();
            }

            if (ex.ErrorCode == 10054)
                OnDisconnectInternal();

            OnError?.Invoke(this, ex);
        }

        void OnDisconnectInternal()
        {
            Status = TcpClientStatus.Disconnected;

            lock (tcpClient)
            {
                tcpClient.Close();
                OnDisconnect?.Invoke(this);
            }
        }

        void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                lock (tcpClient)
                    tcpClient.EndConnect(ar);

                BeginReceive();

                Status = TcpClientStatus.Connected;
                connectEvent.Set();

                OnConnect?.Invoke(this);
            }
            catch (SocketException ex)
            {
                OnErrorInternal(ex);
            }
        }

        void BeginReceive()
        {
            lock (tcpClient)
            {
                if (tcpClient.Client == null)
                    return;
                
                tcpClient.Client.BeginReceive(tempBuffer, 0, tempBuffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
        }

        /// <summary>
        /// read循环
        /// </summary>
        /// <param name="ar"></param>
        void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = 0;
                lock (tcpClient)
                {
                    if (tcpClient?.Client == null)
                        return;

                    bytesRead = tcpClient.Client.EndReceive(ar);

                    if (bytesRead == 0)
                        OnDisconnectInternal();
                }


                lock (buffer)
                {
                    byte[] newbuffer = new byte[buffer.Length + bytesRead];
                    buffer.CopyTo(newbuffer, 0);
                    Array.Copy(tempBuffer, 0, newbuffer, buffer.Length, bytesRead);
                    buffer = newbuffer;

                    TcpPacket packet;
                    while ((packet = TcpPacket.Parse(ref buffer, Cipher)) != null)
                    {
                        OnIncomingPacket?.Invoke(packet);
                    }
                }

                BeginReceive();
            }
            catch (SocketException ex)
            {
                OnErrorInternal(ex);
            }
        }

        public void Send(TcpPacket packet)
        {
            try
            {
                if (!Connected) return;

                byte[] sendBuffer = packet.ToByteArray(Cipher);
                lock (tcpClient)
                {
                    tcpClient.Client.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, null, null);
                }
            }
            catch (ObjectDisposedException)
            { }
        }

        public void Dispose()
        {
            if (tcpClient != null)
            {
                lock (tcpClient)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }
            }

            if (connectEvent != null)
            {
                lock (connectEvent)
                {
                    connectEvent.Close();
                    connectEvent = null;
                }
            }
        }

        void ReInitSocket()
        {
            Dispose();

            Status = TcpClientStatus.Disconnected;
            connectEvent = new AutoResetEvent(false);
            tcpClient = new System.Net.Sockets.TcpClient();
            tcpClient.Client.Blocking = blocking;
            tcpClient.Client.NoDelay = true;
            buffer = new byte[0];
            tempBuffer = new byte[ReceiveBufferSize];
        }

        public TcpClient(bool blocking = false)
        {
            this.blocking = blocking;
            this.ReInitSocket();
            this.ReceiveBufferSize = DefaultBufferSize;
            this.SendBufferSize = DefaultBufferSize;
        }

    }
}
