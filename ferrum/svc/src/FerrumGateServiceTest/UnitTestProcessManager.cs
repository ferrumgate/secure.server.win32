using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            using (AutoResetEvent areprocess = new AutoResetEvent(false))
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                Task.Run(() =>
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    using (PipeServer server = new PipeServer("testme", cts.Token))
                    {
                        server.WaitForConnection();
                        string pipename = ProcessManager.Start("ls", areprocess);
                        areprocess.WaitOne();
                        server.WriteString("connect to:" + pipename);
                        are.WaitOne();
                    }
                });

                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (PipeClient client1 = new PipeClient("testme", cts.Token))
                {
                    var pipename = client1.ReadString();
                    pipename = pipename.Replace("connect to:", "");
                    using (PipeClient client = new PipeClient("ferrumgate_" + pipename, cts.Token))
                    {
                        client.ReadString();
                        Thread.Sleep(5000);
                        ProcessManager.Stop();
                    }
                }
                are.Set();
                Assert.IsTrue(!String.IsNullOrEmpty(d));
            }
        }
        [TestMethod]
        public void TestMethodIsWorking()
        {
            String d = "";
            ProcessManager.ProcessOutput = (data) => d += data;
            using (AutoResetEvent areProcess = new AutoResetEvent(false))
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                Task.Run(() =>
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    using (PipeServer server = new PipeServer("testyou", cts.Token))
                    {
                        server.WaitForConnection();
                        string pipename = ProcessManager.Start("sleep 5", areProcess);
                        areProcess.WaitOne();
                        server.WriteString("connect to:" + pipename);
                        are.WaitOne();
                    }
                });


                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (PipeClient client1 = new PipeClient("testyou", cts.Token))
                {

                    var pipename = client1.ReadString();
                    pipename = pipename.Replace("connect to:", "");
                    using (PipeClient client = new PipeClient("ferrumgate_" + pipename, cts.Token))
                    {
                        client.ReadString();
                        Thread.Sleep(2000);
                        var isWorking = ProcessManager.IsWorking();
                        Assert.IsTrue(isWorking);
                    }
                }
                are.Set();
            }
            
            
        }
    }
}
