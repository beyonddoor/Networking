using System;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using SolarGames.Networking.Crypting;

namespace SolarGames.Networking
{
	public class TcpConnection : IDisposable
	{
        ICipher cipher;
        byte[] recv_buffer;
        byte[] storage_buffer;
        TcpServer parent;
        bool closed;
        
        public int ConnectionId { get; set; }

        public IConnectedObject ConnectedObject { get; set; }

        public Socket Socket { get; private set; }

        public TcpConnection(TcpServer parent, Socket socket, int bufferSize, Type cipherType)
		{
            if (cipherType != null)
                cipher = Activator.CreateInstance(cipherType) as ICipher;
			this.Socket = socket;
			this.recv_buffer = new byte[bufferSize];
			this.storage_buffer = new byte[0];
			this.parent = parent;
            this.ConnectionId = StrongRandom.NextInt(1, int.MaxValue);
			
            socket.BeginReceive(recv_buffer, 0,
                    recv_buffer.Length, SocketFlags.None,
                    ReceiveCallback, null);
		}
		
  		public void Close()
        {
            if (closed) return;
            closed = true;

            if (ConnectedObject != null)
                ConnectedObject.ConnectionDropped();
           
            lock (parent.connections) 
                parent.connections.Remove(this);

            System.Diagnostics.Debug.WriteLine(string.Format("{0} disconnected. Connections left: {1}", 
                this.ConnectedObject == null ? "Unknown" : this.ConnectedObject.ToString(), parent.connections.Count));

            if (Socket != null)
            {
                Socket.Close();
                Socket = null;
            }
            
            ConnectedObject = null;
        }

        public void Dispose()
        {
            Close();
        }

        public override string ToString()
        {
            if (Socket?.RemoteEndPoint != null)
                return
                    $"TcpConnection[address={Socket.RemoteEndPoint}, connected={Socket.Connected.ToString()}]";
            return "TcpConnection[Closed]";
        }

        public bool IsValidUDPSession(UdpSession session)
        {
            if (session.address.ToString() != ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString()) return false;
            if (session.sessionId != ConnectionId) return false;
            if (ConnectedObject == null) return false;

            return true;
        }
				
		internal void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int bytesRead = 0;
                
                if (Socket != null)
                    bytesRead = Socket.EndReceive(result);

                if (bytesRead == 0)
				{
					Close();
					return;
				}
			
                

				byte[] newbuffer = new byte[storage_buffer.Length + bytesRead];
                storage_buffer.CopyTo(newbuffer, 0);
                Array.Copy(recv_buffer, 0, newbuffer, storage_buffer.Length, bytesRead);
                storage_buffer = newbuffer;

                
                
                TcpPacket packet = null;
                while ((packet = TcpPacket.Parse(ref storage_buffer, cipher)) != null)
                {

                    if (ConnectedObject == null)
                        parent.OnNotAuthorizedPacketInternal(this, packet);
                    else
                        ConnectedObject.Dispatch(packet);
                }

                Socket?.BeginReceive(recv_buffer, 0,
                    recv_buffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), null);

            }
			catch (ObjectDisposedException) { Close(); }
			catch (SocketException) { Close(); }
        }
		
		
		public void Send(TcpPacket packet)
		{
            if (Socket == null) 
                return;

            try
            {
                byte[] sendBuffer = packet.ToByteArray(cipher);
                //socket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), packet);
                Socket.Send(sendBuffer);
            }
            catch (ObjectDisposedException)
            { }
            catch (SocketException)
            {
                 Close();
            }
		}

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                if (Socket == null) return;
                Socket.EndSend(ar);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException)
            {
                Close();
                return;
            }
        }
	}
}

