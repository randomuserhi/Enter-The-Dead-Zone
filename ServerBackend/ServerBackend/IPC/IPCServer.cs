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

        private NamedPipeServerStream PipeOut;
        private NamedPipeServerStream PipeIn;

        public IPCNamedServer(string PipeName, int NumberOfInstances, int DataBufferSize)
        {
            this.DataBufferSize = DataBufferSize;
            ReceiveBuffer = new byte[DataBufferSize];

            PipeIn = new NamedPipeServerStream(PipeName + "_IN", PipeDirection.InOut, NumberOfInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            PipeOut = new NamedPipeServerStream(PipeName + "_OUT", PipeDirection.InOut, NumberOfInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        }

        public async Task Connect()
        {
            int Timeout = 5000;

            //Wait for connection
            IAsyncResult ConnectionResultOut = PipeOut.BeginWaitForConnection(PipeConnected, new NamedPipeServerWrapper(PipeOut));
            IAsyncResult ConnectionResultIn = PipeIn.BeginWaitForConnection(PipeConnected, new NamedPipeServerWrapper(PipeIn, ReceiveCallback, ReceiveBuffer));

            //Do additional server startup code
            OnStart();

            //Wait for connection to complete
            bool Result = WaitHandle.WaitAll(new[] { ConnectionResultIn.AsyncWaitHandle, ConnectionResultOut.AsyncWaitHandle }, Timeout);
            if (Result == false)
            {
                throw new TimeoutException();
                //TODO:: handle timeout exception => dispose and close server
            }

            SendMessage();
        }

        protected virtual void OnStart()
        {
        }

        //Needs to use Packet class to do so
        public virtual void SendMessage()
        {
            try
            {
                byte[] Message = Encoding.ASCII.GetBytes("Hello World");
                PipeOut.BeginWrite(Message, 0, Message.Length, null, null);
            }
            catch(Exception E)
            {
                Console.WriteLine("IPCNamedServer.SendMessage => " + E);
            }
        }

        public virtual void ReceiveCallback(IAsyncResult Result)
        {
            try
            {
                Console.WriteLine("Recieved");
                using (StreamReader sr = new StreamReader(PipeIn))
                {
                    // Display the read text to the console
                    string temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        Console.WriteLine("Received from server: {0}", temp);
                    }
                }
            }
            catch(Exception E)
            {
                Console.WriteLine("IPCNamedServer.ReceiveCallback => " + E);
            }
        }

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
