using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

using Network;
using Network.IPC;
using Network.TCPUDP;

namespace ServerBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IPCNamedServer Server = new IPCNamedServer("Test", NamedPipeServerStream.MaxAllowedServerInstances, 4096))
            {
                using (TCPUDPServer NetServer = new TCPUDPServer(26950, 1, 4096))
                {
                    var ServerTask = Task.Run(async () => await Server.Connect(5000));
                    try
                    {
                        ServerTask.Wait();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var innerException in ae.Flatten().InnerExceptions)
                        {
                            Console.WriteLine(innerException);
                        }
                        Console.ReadLine();
                    }

                    var NetServerTask1 = Task.Run(async () => await NetServer.Connect(5000));
                    try
                    {
                        NetServerTask1.Wait();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var innerException in ae.Flatten().InnerExceptions)
                        {
                            Console.WriteLine(innerException);
                        }
                        Console.ReadLine();
                    }

                    while (true)
                    {
                        Packet Test2 = new Packet();
                        Test2.Write("UDP Hello From Server");
                        NetServer.UDPSendMessage(Test2, 0);

                        Network.ServerHandle.FixedUpdate();
                    }
                }
            }
        }
    }
}
