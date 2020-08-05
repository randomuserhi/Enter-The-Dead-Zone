using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace IPC
{
    public class IPCNamedServer : IDisposable
    {
        private NamedPipeServerStream PipeOut;
        private NamedPipeServerStream PipeIn;

        public IPCNamedServer(string PipeName)
        {
            PipeIn = new NamedPipeServerStream(PipeName + "_IN", PipeDirection.InOut);
            PipeOut = new NamedPipeServerStream(PipeName + "_OUT", PipeDirection.InOut);
        }

        public void Test()
        {
            PipeOut.WaitForConnection();
            PipeIn.WaitForConnection();

            try
            {
                // Read user input and send that to the client process.
                using (StreamWriter sw = new StreamWriter(PipeOut))
                {
                    sw.AutoFlush = true;
                    Console.Write("Enter text: ");
                    sw.WriteLine(Console.ReadLine());
                }
            }
            // Catch the IOException that is raised if the pipe is broken
            // or disconnected.
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }
            Console.ReadLine();
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
