using System;
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
    /// <summary>
    /// Represents a PipeStream with its respective Buffer and AsyncCallback
    /// </summary>
    public struct NamedPipeServerWrapper
    {
        public byte[] ReceiveBuffer;
        public NamedPipeServerStream Stream;
        public AsyncCallback ReceiveCallback;

        public NamedPipeServerWrapper(NamedPipeServerStream Stream, AsyncCallback ReceiveCallback = null, byte[] ReceiveBuffer = null)
        {
            this.Stream = Stream;
            this.ReceiveBuffer = ReceiveBuffer;
            this.ReceiveCallback = ReceiveCallback;
        }
    }

    public class IPCNamedServer : IDisposable
    {
        private int DataBufferSize;
        private byte[] ReceiveBuffer;
        private Packet ReceivedData = new Packet();

        private NamedPipeServerStream PipeOut;
        private NamedPipeServerStream PipeIn;

        /// <summary>
        /// Creates a new NamedPipeServer
        /// </summary>
        /// <param name="PipeName">Server name</param>
        /// <param name="NumberOfInstances">Number of server instances allowed</param>
        /// <param name="DataBufferSize">Size of data buffer</param>
        public IPCNamedServer(string PipeName, int NumberOfInstances, int DataBufferSize)
        {
            this.DataBufferSize = DataBufferSize;
            ReceiveBuffer = new byte[DataBufferSize];

            PipeIn = new NamedPipeServerStream(PipeName + "_IN", PipeDirection.InOut, NumberOfInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            PipeOut = new NamedPipeServerStream(PipeName + "_OUT", PipeDirection.InOut, NumberOfInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        /// <summary>
        /// Asynchronously attempt connecting to client
        /// </summary>
        /// <returns></returns>
        public async Task Connect(int Timeout = -1)
        {
            //Wait for connection
            IAsyncResult ConnectionResultOut = PipeOut.BeginWaitForConnection(PipeConnected, new NamedPipeServerWrapper(PipeOut));
            IAsyncResult ConnectionResultIn = PipeIn.BeginWaitForConnection(PipeConnected, new NamedPipeServerWrapper(PipeIn, ReceiveCallback, ReceiveBuffer));

            //Do additional server startup code
            OnStart();

            if (Timeout > 0)
            {
                //Wait for connection to complete
                bool Result = WaitHandle.WaitAll(new[] { ConnectionResultIn.AsyncWaitHandle, ConnectionResultOut.AsyncWaitHandle }, Timeout);
                if (Result == false)
                {
                    throw new TimeoutException();
                    //TODO:: handle timeout exception => dispose and close server
                }
            }

            Packet Test = new Packet();
            Test.Write("Hello From Server");
            SendMessage(Test);
        }

        /// <summary>
        /// Called during connection to client, NOTE: Clients may not have successfully connected at this point
        /// </summary>
        protected virtual void OnStart()
        {
        }

        /// <summary>
        /// Sends a given Packet to the client
        /// </summary>
        public void SendMessage(Packet Packet)
        {
            try
            {
                //Check if packet bytes are available, if not convert bytes
                if (Packet.ReadableBuffer == null)
                    Packet.ConvertBufferToArray();

                PipeOut.BeginWrite(Packet.ReadableBuffer, 0, Packet.ReadableBuffer.Length, null, null);
            }
            catch(Exception E)
            {
                Console.WriteLine("IPCNamedServer.SendMessage => " + E);
            }
        }

        /// <summary>
        /// Handles message recieved by the server
        /// </summary>
        /// <param name="Result"></param>
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

                //Handle data as NamedPipes are streambased => Packet data can be sent in 2 deliveries causing the packet to be broken up
                //If a packet is sent over 2 deliveries, HandleData will return false such that the currently recieved data is not reset such
                //that the rest of the packet can be recieved
                ReceivedData.Reset(HandleData(Data, Epoch));

                //Start read for next packet
                PipeIn.BeginRead(ReceiveBuffer, 0, DataBufferSize, ReceiveCallback, null);
            }
            catch(Exception E)
            {
                Console.WriteLine("IPCNamedServer.ReceiveCallback => " + E);
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

        /// <summary>
        /// Called upon succesful pipe connection, begins reading PipeStream
        /// </summary>
        /// <param name="Result"></param>
        private void PipeConnected(IAsyncResult Result)
        {
            NamedPipeServerWrapper Pipe = (NamedPipeServerWrapper)Result.AsyncState;

            try
            {
                Pipe.Stream.EndWaitForConnection(Result);

                //Only read stream if a buffer is provided
                if (Pipe.ReceiveBuffer != null)
                    Pipe.Stream.BeginRead(Pipe.ReceiveBuffer, 0, DataBufferSize, Pipe.ReceiveCallback, null);
            }
            catch(ObjectDisposedException)
            {
                //Server was closed already, do nothing
                return;
            }
        }

        ~IPCNamedServer()
        {
            Dispose(false);
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
