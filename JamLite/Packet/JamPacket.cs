using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JamLite.Packet
{
    public class JamPacket
    {
        private struct ReceiveState
        {
            public SslStream Stream { get; set; }
            public JamPacket Packet { get; set; }
            public byte[] HeaderBuffer { get; set; }

            public int BytesRead { get; set; }
            public AutoResetEvent ReceiveCompleted { get; set; }
        }

        private readonly AutoResetEvent sendCompleted = new AutoResetEvent(false);

        public JamPacketHeader Header { get; private set; }
        public byte[] Data { get; private set; }
        public bool Inflated { get; private set; } = false;

        public JamPacket() { }

        public JamPacket(int dataType, byte[] data)
        {
            Header = new JamPacketHeader()
            {
                DataType = dataType,
                DataLength = data.Length
            };

            Data = data;
            Inflated = true;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Data);
        }

        public int Send(SslStream stream)
        {
            byte[] headerBytes = Header.GetBytes();
            byte[] sendBytes = new byte[headerBytes.Length + Data.Length];

            headerBytes.CopyTo(sendBytes, 0);
            Data.CopyTo(sendBytes, headerBytes.Length);

            stream.BeginWrite(sendBytes, 0, sendBytes.Length, SendCallback, stream);
            sendCompleted.WaitOne();

            return sendBytes.Length;
        }

        private void SendCallback(IAsyncResult result)
        {
            SslStream stream = result.AsyncState as SslStream;
            stream.EndWrite(result);
            stream.Flush();

            sendCompleted.Set();
        }

        public static JamPacket Receive(SslStream stream)
        {
            const int RECEIVE_TIMEOUT = 2000;

            ReceiveState state = new ReceiveState()
            {
                Stream = stream,
                Packet = new JamPacket(),
                HeaderBuffer = new byte[Marshal.SizeOf(typeof(JamPacketHeader))],
                ReceiveCompleted = new AutoResetEvent(false)
            };

            try
            {
                stream.BeginRead(state.HeaderBuffer, 0, state.HeaderBuffer.Length, ReceiveHeaderCallback, state);
                if (Debugger.IsAttached)
                    state.ReceiveCompleted.WaitOne();
                else
                    state.ReceiveCompleted.WaitOne(RECEIVE_TIMEOUT);
            }
            catch (IOException) { }

            if (state.Packet.Header.DataType == 0)
                return null;

            state.Packet.Inflated = true;
            return state.Packet;
        }

        private static void ReceiveHeaderCallback(IAsyncResult result)
        {
            try
            {
                ReceiveState state = (ReceiveState)result.AsyncState;
                state.BytesRead += state.Stream.EndRead(result);
                int bytesRequired = Marshal.SizeOf(state.Packet.Header.GetType());

                if (state.BytesRead != bytesRequired)
                {
                    state.Stream.BeginRead(state.HeaderBuffer, state.BytesRead, state.HeaderBuffer.Length - state.BytesRead, ReceiveHeaderCallback, state);
                    return;
                }

                state.Packet.Header = new JamPacketHeader(state.HeaderBuffer);
                state.Packet.Data = new byte[state.Packet.Header.DataLength];
                state.BytesRead = 0;

                state.Stream.BeginRead(state.Packet.Data, 0, state.Packet.Header.DataLength, ReceiveDataCallback, state);
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        private static void ReceiveDataCallback(IAsyncResult result)
        {
            try
            {
                ReceiveState state = (ReceiveState)result.AsyncState;
                state.BytesRead += state.Stream.EndRead(result);
                int bytesRequired = state.Packet.Header.DataLength;

                if (state.BytesRead != bytesRequired)
                {
                    state.Stream.BeginRead(state.Packet.Data, state.BytesRead, state.Packet.Header.DataLength - state.BytesRead, ReceiveDataCallback, state);
                    return;
                }

                state.ReceiveCompleted.Set();
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
