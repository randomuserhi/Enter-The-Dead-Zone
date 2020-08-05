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

        public async Task Connect()
        {
            int Timeout = 5000;

            //Wait for connection
            Task ConnectOut = PipeOut.ConnectAsync();
            Task ConnectIn = PipeIn.ConnectAsync();

            //Do additional client startup code
            OnStart();

            //Wait for connection to complete
            bool Result = Task.WaitAll(new[] { ConnectIn, ConnectOut }, Timeout);
            if (Result == false)
            {
                throw new TimeoutException();
                //TODO:: handle timeout exception => dispose and close client
            }

            PipeConnected(new NamedPipeClientWrapper(PipeIn, ReceiveCallback, ReceiveBuffer));
            PipeConnected(new NamedPipeClientWrapper(PipeOut));

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
            catch (Exception E)
            {
                Console.WriteLine("IPCNamedClient.SendMessage => " + E);
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
                Console.WriteLine("IPCNamedClient.ReceiveCallback => " + E);
            }
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
