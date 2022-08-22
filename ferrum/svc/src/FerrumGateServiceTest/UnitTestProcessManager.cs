using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestProcessManager
    {
        [TestMethod]
        public void TestMethodStart()
        {
            String d = "";
            ProcessManager.ProcessOutput = (data) => d+=data;
            string pipename=ProcessManager.Start("ls" );
            Thread.Sleep(1000);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeClient client = new PipeClient("ferrumgate_"+pipename, cts.Token))
            {
                client.ReadString();
                Thread.Sleep(5000);
                ProcessManager.Stop();
            }
            Assert.IsTrue(!String.IsNullOrEmpty(d));
        }
        [TestMethod]
        public void TestMethodIsWorking()
        {
            String d = "";
            ProcessManager.ProcessOutput = (data) => d += data;
            string pipename = ProcessManager.Start("sleep 5");
            Thread.Sleep(1000);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeClient client = new PipeClient("ferrumgate_" + pipename, cts.Token))
            {
                client.ReadString();
                Thread.Sleep(2000);
                var isWorking = ProcessManager.IsWorking();
                Assert.IsTrue(isWorking);
            }
            
            
        }
    }
}
