using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

using Network.IPC;

namespace ServerBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IPCNamedServer Server = new IPCNamedServer("Test", NamedPipeServerStream.MaxAllowedServerInstances, 4096))
            {
                var ServerTask = Task.Run(async () => await Server.Connect());
                try
                {
                    ServerTask.Wait();
                    
                    while(true)
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
