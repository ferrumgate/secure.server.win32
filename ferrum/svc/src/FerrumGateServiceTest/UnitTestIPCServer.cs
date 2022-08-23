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
            Assert.IsTrue(IPCServer.Where == IPCServer.Status.Connect);
            using(var cs=new CancellationTokenSource())
            using(var client=new PipeClient("xman", cs.Token))
            {
                client.WriteString("connect -X ferrum@1.2.3.4");
                Thread.Sleep(100);
                Assert.IsTrue(IPCServer.Where == IPCServer.Status.Connecting);
                
            }

            IPCServer.Stop();
            Assert.IsTrue(task.IsCompleted);

        }

    }
}
