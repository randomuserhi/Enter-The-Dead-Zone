using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;

using Network.IPC;
using Network.TCPUDP;
using System.Net;

namespace ServerBackendReciever
{
    class Program
    {
        static void Main(string[] args)
        {
            using (IPCNamedClient Client = new IPCNamedClient(".", "Test", 4096))
            {
                using (TCPUDPClient NetClient = new TCPUDPClient(4096))
                {
                    /*var ServerTask = Task.Run(async () => await Client.Connect(5000));
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
                    }*/

                    var NetServerTask1 = Task.Run(async () => await NetClient.TCPConnect("192.168.2.26", 26950));
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

                    NetClient.UDPConnect(((IPEndPoint)(NetClient.TCPSocket.Client.LocalEndPoint)).Port, "192.168.2.26", 26950);

                    while (true)
                    {
                        Network.ServerHandle.FixedUpdate();
                    }
                }
            }
        }
    }
}
