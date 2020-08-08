using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;

using Network.IPC;

namespace ServerBackendReciever
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IPCNamedClient Client = new IPCNamedClient(".", "Test", 4096))
            {
                var ServerTask = Task.Run(async () => await Client.Connect());
                try
                {
                    ServerTask.Wait();

                    while (true)
                    {
                        Network.ServerHandle.FixedUpdate();
                    }

                }
                catch (AggregateException ae)
                {
                    foreach (var innerException in ae.Flatten().InnerExceptions)
                    {
                        Console.WriteLine(innerException);
                    }
                    Console.ReadLine();
                }
            }
        }
    }
}
