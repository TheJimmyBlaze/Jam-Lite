using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JamLite.Packet;

namespace JamLite
{
    public class JamServer: IDisposable
    {
        #region Event Handlers
        public class MessageReceivedEventArgs : EventArgs
        {
            public JamServerConnection ServerConnection { get; set; }
            public JamPacket Packet { get; set; }
        }

        public class ConnectionEventArgs : EventArgs
        {
            public JamServerConnection ServerConnection { get; set; }
            public EndPoint RemoteEndPoint { get; set; }
        }

        public EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;
        public EventHandler<ConnectionEventArgs> ClientConnectedEvent;
        public EventHandler<ConnectionEventArgs> ClientDisconnectedEvent;
        public EventHandler DisposedEvent;

        public void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceivedEvent?.Invoke(this, e);
        }

        public void OnClientConnected(ConnectionEventArgs e)
        {
            ClientConnectedEvent?.Invoke(this, e);
        }

        public void OnClientDisconnected(ConnectionEventArgs e)
        {
            ClientDisconnectedEvent?.Invoke(this, e);
        }

        public void OnDisposed(EventArgs e)
        {
            DisposedEvent?.Invoke(this, e);
        }
        #endregion

        private struct ConnectState
        {
            public TcpClient Client { get; set; }
            public SslStream Stream { get; set; }
        }

        private readonly AutoResetEvent acceptCompleted = new AutoResetEvent(false);

        private const string CERTIFICATE_PATH = @"DevCert.pfx";
        private const string CERTIFICATE_PASSWORD = @"";

        public bool Alive { get; private set; }
        private X509Certificate2 certificate;

        public JamServerConnection Connection { get; set; }

        public JamServer()
        {
            certificate = new X509Certificate2(CERTIFICATE_PATH, CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable);
        }

        public void Dispose()
        {
            if (Alive)
            {
                Alive = false;
                Connection?.Dispose();
                OnDisposed(null);
            }
        }

        public void Start(string ip, int port)
        {
            Alive = true;
            Task.Run(() => { Listen(ip, port); });
        }

        private async void Listen(string ip, int port)
        {
            IPAddress address = IPAddress.Any;
            if (IPAddress.TryParse(ip, out IPAddress parsed))
                address = parsed;

            TcpListener listener = new TcpListener(address, port);
            listener.Start();

            while (Alive)
            {
                await Task.Delay(50);
                if (Connection == null)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    SslStream stream = new SslStream(client.GetStream(), false);

                    ConnectState state = new ConnectState()
                    {
                        Client = client,
                        Stream = stream
                    };

                    stream.BeginAuthenticateAsServer(certificate, AcceptCallback, state);
                    acceptCompleted.WaitOne();
                }
            }
        }

        private void AcceptCallback(IAsyncResult result)
        {
            ConnectState state = (ConnectState)result.AsyncState;
            if (state.Stream.CanRead)
                state.Stream.EndAuthenticateAsServer(result);
            else
                return;

            Connection = new JamServerConnection(this, state.Client, state.Stream);

            acceptCompleted.Set();
        }
    }
}
