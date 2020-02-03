using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JamLite.Packet;
using JamLite.Packet.Data;

namespace JamLite
{
    public class JamClient: IDisposable
    {
        #region Event Handlers
        public class MessageReceivedEventArgs : EventArgs
        {
            public JamPacket Packet { get; set; }
        }

        public EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public EventHandler DisposedEvent;

        public void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceivedEvent?.Invoke(this, e);
        }

        public void OnDisposed(EventArgs e)
        {
            DisposedEvent?.Invoke(this, e);
        }
        #endregion

        private readonly AutoResetEvent connectionCompleted = new AutoResetEvent(false);

        private SslStream stream;

        private readonly ConcurrentQueue<JamPacket> packetSendQueue = new ConcurrentQueue<JamPacket>();

        public bool Alive { get; private set; }

        public bool IsConnection
        {
            get { return stream != null && stream.CanWrite && stream.CanRead; }
        }

        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors error)
        {
            //Add some code to verify cert here if needed.
            return true;
        }

        public void Dispose()
        {
            if (Alive)
            {
                Alive = false;
                stream?.Close();
                OnDisposed(null);
            }
        }

        public void Connect(string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(ip, port);
                SslStream inProgressStream = new SslStream(client.GetStream(), false, ValidateCertificate);

                inProgressStream.BeginAuthenticateAsClient(ip, null, SslProtocols.Default, false, ConnectCallback, inProgressStream);
                connectionCompleted.WaitOne();

                if (stream != null && stream.IsAuthenticated)
                {
                    Alive = true;
                    Task.Run(() => Listen());
                    Task.Run(() => SendPacketsFromQueue());
                }
            }
            catch (SocketException) { }
        }

        private void ConnectCallback(IAsyncResult result)
        {
            stream = result.AsyncState as SslStream;
            stream.EndAuthenticateAsClient(result);
            connectionCompleted.Set();
        }

        public void Send(JamPacket packet)
        {
            packetSendQueue.Enqueue(packet);
        }

        private async void SendPacketsFromQueue()
        {
            while (Alive)
            {
                await Task.Run(() => Thread.Sleep(50));

                if (packetSendQueue.Count > 0 && stream.CanWrite)
                {
                    try
                    {
                        if (packetSendQueue.TryDequeue(out JamPacket sendPacket))
                            sendPacket.Send(stream);
                    }
                    catch (IOException)
                    {
                        Dispose();
                    }
                }
            }
        }

        private void Listen()
        {
            while (Alive)
            {
                JamPacket packet = JamPacket.Receive(stream);
                if (packet == null)
                {
                    Dispose();
                    return;
                }

                if (packet.Header.DataType == PingRequest.DATA_TYPE)
                    RespondToPing(packet);
                else
                    OnMessageReceived(new MessageReceivedEventArgs() { Packet = packet });
            }
        }

        public void RespondToPing(JamPacket pingPacket)
        {
            if (pingPacket.Header.DataType != PingRequest.DATA_TYPE)
                return;

            PingRequest request = new PingRequest(pingPacket.Data);
            PingResponse response = new PingResponse(request.PingTimeUtc, DateTime.UtcNow);

            JamPacket responsePacket = new JamPacket(PingResponse.DATA_TYPE, response.GetBytes());
            Send(responsePacket);
        }
    }
}
