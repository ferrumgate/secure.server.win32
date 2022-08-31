using System;
using System.Threading;
using System.Threading.Tasks;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestPipeClient
    {
        [TestMethod]
        public void TestMethodServerAndClientCommunication()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer("abc",cts.Token))
            {
                var taskWait= Task.Run(() =>
                {
                    server.WaitForConnection();
                    var msg = server.ReadString();
                    Assert.AreEqual(msg, "merhaba");
                    server.WriteString("hello merhaba");
                });                

                using (PipeClient client = new PipeClient("abc",cts.Token))
                {
                   
                    client.WriteString("merhaba");
                    
                    var servermsg= client.ReadString();
                    taskWait.Wait();
                    Assert.AreEqual(servermsg, "hello merhaba");
                   
                }
              
            }
               
        }

       


        [TestMethod]
        public void TestMethodServeSendsStreamedData()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer("abc",cts.Token))
            {
                var taskWait = Task.Run(() =>
                {
                    server.WaitForConnection();
                    var msg = server.ReadString();
                    Assert.AreEqual(msg, "merhaba\r\n");
                    for (int i = 0; i < 10; i++)
                    {
                        server.WriteString("hello merhaba\r\n"+i);
                    }
                    
                });

                
                using (PipeClient client = new PipeClient("abc",cts.Token))
                {

                    client.WriteString("merhaba\r\n");
                    for (int i = 0; i < 10; i++)
                    {
                        var servermsg = client.ReadString();
                        Console.WriteLine(servermsg);
                        Console.WriteLine("****");
                        Assert.AreEqual(servermsg, "hello merhaba\r\n"+i);
                    }
                    
                    taskWait.Wait();                    

                }

            }

        }


        [TestMethod]
        [ExpectedException(typeof(TimeoutException), AllowDerivedTypes = false)]
        public void TestMethodServeSendsStreamedDataCancelTokenThrows()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer("abc", cts.Token))
            {
                var taskWait = Task.Run(() =>
                {
                    server.WaitForConnection();
                    var msg = server.ReadString();
                    Assert.AreEqual(msg, "merhaba\r\n");
                    for (int i = 0; i < 10; i++)
                    {
                        server.WriteString("hello merhaba\r\n" + i);
                    }

                });


                using (PipeClient client = new PipeClient("abc", cts.Token))
                {

                    client.WriteString("merhaba\r\n");
                    for (int i = 0; i < 10; i++)
                    {
                        var servermsg = client.ReadString();
                        Assert.AreEqual(servermsg, "hello merhaba\r\n" + i);
                        if (i == 1)
                            cts.Cancel();
                    }

                    taskWait.Wait();


                }

            }

        }


    }
}
