using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using JamLite.Packet;
using JamLite.Packet.Data;

namespace JamLite
{
    public class JamServerConnection
    {
        private const int DISCONNECT_POLL_FREQUENCY = 500;

        public readonly JamServer server;

        public readonly TcpClient client;
        private readonly SslStream stream;

        private readonly ConcurrentQueue<JamPacket> packetSendQueue = new ConcurrentQueue<JamPacket>();

        public bool Alive { get; private set; }

        public JamServerConnection(JamServer server, TcpClient client, SslStream stream)
        {
            this.server = server;

            this.client = client;
            this.stream = stream;

            Alive = true;

            Task.Run(() => Listen());
            Task.Run(() => SendPacketsFromQueue());
            Task.Run(() => PollConnection(DISCONNECT_POLL_FREQUENCY));

            server.OnClientConnected(new JamServer.ConnectionEventArgs() { ServerConnection = this, RemoteEndPoint = client.Client.RemoteEndPoint });
        }

        public void Dispose()
        {
            if (Alive)
            {
                Alive = false;
                server.OnClientDisconnected(new JamServer.ConnectionEventArgs() { ServerConnection = this, RemoteEndPoint = client.Client.RemoteEndPoint });

                stream.Close();
                server.Connection = null;
            }
        }

        public void Send(JamPacket packet)
        {
            packetSendQueue.Enqueue(packet);
        }

        private async void SendPacketsFromQueue()
        {
            while (Alive)
            {
                await Task.Delay(50);

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
                else if (packet.Header.DataType != PingResponse.DATA_TYPE)
                    server.OnMessageReceived(new JamServer.MessageReceivedEventArgs() { ServerConnection = this, Packet = packet });
            }
        }

        private async void PollConnection(int pollFrequency)
        {
            while (Alive)
            {
                await Task.Delay(pollFrequency);

                PingRequest pingRequest = new PingRequest(DateTime.UtcNow);
                JamPacket pingPacket = new JamPacket(PingRequest.DATA_TYPE, pingRequest.GetBytes());
                Send(pingPacket);
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
