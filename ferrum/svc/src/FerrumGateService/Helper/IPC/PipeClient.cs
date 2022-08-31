using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateService.Helper.IPC
{
    /// <summary>
    ///  client with using pipe
    /// </summary>
   public class PipeClient:IDisposable
    {
        /// <summary>
        /// data structure
        /// </summary>
        internal class PipeData
        {
           
            public byte[] ReadBuffer { get; private set; }
            public int ReadedSize { get; set; }
            public NamedPipeClientStream Client { get; private set; }
            public PipeData(NamedPipeClientStream pipe)
            {
                this.Client = pipe;                
                this.ReadBuffer = new byte[16*1024];
            }

           

            public void Dispose()
            {
                if (this.Client != null)
                {
                    this.Client.Dispose();
                    this.Client = null;
                }
                
            }

        }


        PipeData data;
        StreamReadWriter readerWriter = null;
        private int readWriteTimeout = 1000;
        public PipeClient(string pipename,CancellationToken cancelToken, string machine=null,int connectTimeout=5000,int readWriteTimeout=5000)
        {
            if (machine != null)
                data = new PipeData(new NamedPipeClientStream(machine, pipename,PipeDirection.InOut,PipeOptions.Asynchronous));
            else data = new PipeData(new NamedPipeClientStream(".",pipename,PipeDirection.InOut, PipeOptions.Asynchronous));
            data.Client.Connect(connectTimeout);
            this.readWriteTimeout = readWriteTimeout;
            this.readerWriter = new StreamReadWriter(data.Client,cancelToken, this.readWriteTimeout);
        }

        public string ReadString()
        {
            return this.readerWriter.ReadMsg();
           

        }

        public void WriteString(string data)
        {
            this.readerWriter.WriteMsg(data);
        }

        ~PipeClient()
        {
            Dispose();
        }

        private bool isDisposed = false;

        /// <summary>
        /// c# dispose pattern
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;
            if(data!=null)
            data.Dispose();
            data = null;
            isDisposed = true;
        }
    }
}
