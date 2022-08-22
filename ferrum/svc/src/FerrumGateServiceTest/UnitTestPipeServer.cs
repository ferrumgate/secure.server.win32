using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestPipeServer
    {
        [TestMethod]
        [ExpectedException(typeof(TimeoutException), AllowDerivedTypes = false)]
        public void TestMethodConnectThrowsExceptions()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer("xman", cts.Token, 100))
            {
                server.WaitForConnection();
            }


        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException), AllowDerivedTypes = false)]
        public void TestMethodConnectThrowsExceptionsBecauseofCanceltoken()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer("xman", cts.Token, 100000))
            {
                Task.Run(() =>
                {
                    Thread.Sleep(100);
                    cts.Cancel();
                });
                server.WaitForConnection();
            }


        }

        private void SendDataToServer(Stream ioStream, string data)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(data);
            int len = outBuffer.Length;
            if (len > Int32.MaxValue)
            {
                len = (int)Int32.MaxValue;
            }
            ioStream.WriteByte((byte)((len & 0xFF000000) >> 24));
            ioStream.WriteByte((byte)((len & 0x00FF0000) >> 16));
            ioStream.WriteByte((byte)((len & 0x0000FF00) >> 8));
            ioStream.WriteByte((byte)(len & 0x000000FF));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();
        }

        [TestMethod]
        public void TestMethodConnectsAndSendsData()
        {
            string pipeName = "xman";
            Task serverTask = Task.Run(() =>
              {
                  using (CancellationTokenSource cts = new CancellationTokenSource())
                  using (PipeServer server = new PipeServer(pipeName, cts.Token, 1000, 1000))
                  {
                      server.WaitForConnection();
                      var data = server.ReadString();
                      Assert.AreEqual(data, "merhaba");
                  }


              });

            NamedPipeClientStream client = new NamedPipeClientStream(pipeName);
            client.Connect(1000);
            SendDataToServer(client, "merhaba");
            serverTask.Wait();



        }


        private void SendDataToServerFake(Stream ioStream, string data)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(data);
            int len = outBuffer.Length;
            if (len > Int32.MaxValue)
            {
                len = (int)Int32.MaxValue;
            }
            ioStream.WriteByte((byte)((len & 0xFF000000) >> 24));
            ioStream.WriteByte((byte)((len & 0x00FF0000) >> 16));
            ioStream.WriteByte((byte)((len & 0x0000FF00) >> 8));
            ioStream.WriteByte((byte)((len & 0x000000FF)));
            ioStream.Write(outBuffer, 0, len / 2);//az daha gönderiyoruz,timeout olmalı
            ioStream.Flush();
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void TestMethodConnectsAndSendsDataSlow()
        {
            string pipeName = "xman";
            Task serverTask = Task.Run(() =>
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (PipeServer server = new PipeServer(pipeName, cts.Token, 1000, 1000))
                {
                    server.WaitForConnection();
                    var data = server.ReadString();
                }



            });

            NamedPipeClientStream client = new NamedPipeClientStream(pipeName);
            client.Connect(1000);
            SendDataToServerFake(client, "merhaba");
            try
            {
                serverTask.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }



        }

        class Helper
        {
            public int ReadSize;
            public AutoResetEvent Door;

            public Helper()
            {
                this.ReadSize = 0;
                this.Door = new AutoResetEvent(false);
            }
        }

        [TestMethod]
        public void TestMethodConnectsAndClientsReadData()
        {
            string pipeName = "xman";
            Task serverTask = Task.Run(() =>
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                using (PipeServer server = new PipeServer(pipeName, cts.Token, 5000, 5000))
                {
                    server.WaitForConnection();
                    var data = server.ReadString();
                    Assert.AreEqual(data, "merhaba");
                    server.WriteString("hello world");
                }


            });

            NamedPipeClientStream client = new NamedPipeClientStream(pipeName);
            client.Connect(1000);
            SendDataToServer(client, "merhaba");
            Helper helper = new Helper();
            byte[] readBuffer = new byte[4096];
            client.BeginRead(readBuffer, 0, readBuffer.Length, new AsyncCallback((result) =>
               {
                   var helpme = result.AsyncState as Helper;
                   helpme.ReadSize = client.EndRead(result);
                   helpme.Door.Set();


               }), helper);
            if (!helper.Door.WaitOne(1000))
                throw new ApplicationException("Fatal error");
            byte[] readedData = new byte[helper.ReadSize - 4];
            Array.Copy(readBuffer, 4, readedData, 0, readedData.Length);
            var msg = Encoding.UTF8.GetString(readedData);
            Assert.AreEqual(msg, "hello world");
            serverTask.Wait();



        }


        [TestMethod]

        public void TestMethodDisposeWhenWaitingConnnection()
        {
            string pipeName = Guid.NewGuid().ToString();

            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (PipeServer server = new PipeServer(pipeName, cts.Token, 50000, 5000))
            {

                var task = Task.Run(() =>
                 {
                     server.WaitForConnection();
                 });

                Thread.Sleep(100);
            }



        }





    }
}
