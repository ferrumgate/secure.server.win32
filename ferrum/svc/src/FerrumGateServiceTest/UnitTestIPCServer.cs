using System;
using System.Threading;
using System.Threading.Tasks;
using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestIPCServer
    {
        [TestMethod]
        public void TestMethodStartStop()
        {
            
            var task=IPCServer.Start("xman", 10,1000);
            Thread.Sleep(100);
            IPCServer.Stop();
            Assert.IsTrue(task.IsCompleted);
               
        }

        [TestMethod]
        public void TestMethodStateConnect()
        {

            var task = IPCServer.Start("xman", 10, 5000);
            Thread.Sleep(1000);
            
            using(var cs=new CancellationTokenSource())
            using(var client=new PipeClient("xman", cs.Token))
            {

                client.WriteString("connect 123 324");
                Thread.Sleep(100);
                
               var result= client.ReadString();
                Assert.AreEqual(result, "ok");
                
            }

            IPCServer.Stop();
            Assert.IsTrue(task.IsCompleted);

        }

    }
}
