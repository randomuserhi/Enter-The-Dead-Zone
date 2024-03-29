﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;

namespace Network.IPC
{
    public struct NamedPipeClientWrapper
    {
        public byte[] ReceiveBuffer;
        public NamedPipeClientStream Stream;
        public AsyncCallback ReceiveCallback;

        public NamedPipeClientWrapper(NamedPipeClientStream Stream, AsyncCallback ReceiveCallback = null, byte[] ReceiveBuffer = null)
        {
            this.Stream = Stream;
            this.ReceiveBuffer = ReceiveBuffer;
            this.ReceiveCallback = ReceiveCallback;
        }
    }

    public class IPCNamedClient : IDisposable
    {
        private int DataBufferSize;
        private byte[] ReceiveBuffer;
        private Packet ReceivedData = new Packet();

        public NamedPipeClientStream PipeOut;
        public NamedPipeClientStream PipeIn;

        public IPCNamedClient(string PipeName, string ServerName, int DataBufferSize)
        {
            this.DataBufferSize = DataBufferSize;
            ReceiveBuffer = new byte[DataBufferSize];

            PipeOut = new NamedPipeClientStream(PipeName, ServerName + "_IN", PipeDirection.Out);
            PipeIn = new NamedPipeClientStream(PipeName, ServerName + "_OUT", PipeDirection.In);
        }

        ~IPCNamedClient()
        {
            Dispose(false);
        }

        public async Task Connect(int Timeout = -1)
        {
            //Wait for connection
            Task ConnectOut = PipeOut.ConnectAsync();
            Task ConnectIn = PipeIn.ConnectAsync();

            //Do additional client startup code
            OnStart();

            //Wait for connection to complete
            if (Timeout > 0)
            {
                bool Result = Task.WaitAll(new[] { ConnectIn, ConnectOut }, Timeout);
                if (Result == false)
                {
                    throw new TimeoutException();
                    //TODO:: handle timeout exception => dispose and close client
                }
            }

            PipeConnected(new NamedPipeClientWrapper(PipeIn, ReceiveCallback, ReceiveBuffer));
            PipeConnected(new NamedPipeClientWrapper(PipeOut));

            Packet Test = new Packet();
            Test.Write("Hello From Server");
            SendMessage(Test);
        }

        protected virtual void OnStart()
        {
        }

        //Needs to use Packet class to do so
        public void SendMessage(Packet Packet)
        {
            try
            {
                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray();

                PipeOut.BeginWrite(Packet.ReadableBuffer, 0, Packet.ReadableBuffer.Length, null, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("IPCNamedServer.SendMessage => " + E);
            }
        }

        private void ReceiveCallback(IAsyncResult Result)
        {
            try
            {
                //Get Current Time => for ping calculation
                long Epoch = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000;

                int ByteLength = PipeIn.EndRead(Result);
                if (ByteLength <= 0)
                {
                    return;
                }

                byte[] Data = new byte[ByteLength];
                Array.Copy(ReceiveBuffer, 0, Data, 0, ByteLength);

                //Handle data as NamedPipes are streambased
                ReceivedData.Reset(HandleData(Data, Epoch));

                //Start read for next packet
                PipeIn.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch (Exception E)
            {
                Console.WriteLine("IPCNamedClient.ReceiveCallback => " + E);
            }
        }

        /// <summary>
        /// Handles received data and cases of missing bytes
        /// </summary>
        private bool HandleData(byte[] Data, long Epoch)
        {
            int PacketLength = 0;
            ReceivedData.WriteRange(Data);

            //Check for packet header existing
            if (ReceivedData.UnreadLength() >= sizeof(int))
            {
                PacketLength = ReceivedData.ReadInt();
                if (PacketLength < 1) //If the header states the length, check if the length is 0, if so return true and reset packet reciever
                    return true;
            }

            //Process the packet fully
            while (PacketLength > 0 && PacketLength <= ReceivedData.UnreadLength())
            {
                ServerHandle.ProcessPacket(new Packet(ReceivedData.ReadableBuffer), Epoch);

                PacketLength = 0;

                if (ReceivedData.UnreadLength() >= sizeof(int))
                {
                    PacketLength = ReceivedData.ReadInt();
                    if (PacketLength < 1)
                        return true;
                }
            }

            if (PacketLength <= 1)
                return true;

            return false; //packet is over multiple deliveries, dont reset buffer
        }

        private void PipeConnected(NamedPipeClientWrapper Pipe)
        {
            try
            {
                //Only read stream if a buffer is provided
                if (Pipe.ReceiveBuffer != null)
                    Pipe.Stream.BeginRead(Pipe.ReceiveBuffer, 0, DataBufferSize, Pipe.ReceiveCallback, null);
            }
            catch (ObjectDisposedException)
            {
                //Client was closed already, do nothing
                return;
            }
        }

        //https://stackoverflow.com/questions/18336856/implementing-idisposable-correctly
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            //Dispose managed resources
            if (Disposing)
            {
                PipeIn.Dispose();
                PipeOut.Dispose();
            }

            //Disposed unmannaged resources
        }
    }
}
