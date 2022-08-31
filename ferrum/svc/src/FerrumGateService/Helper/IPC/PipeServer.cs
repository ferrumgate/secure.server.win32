using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateService.Helper.IPC
{
     /// <summary>
     /// server with pipe
     /// </summary>
    public class PipeServer:IDisposable
    {
        
        /// <summary>
        /// Object and data holder for named pipes
        /// </summary>
        class PipeData : IDisposable
        {
            public AutoResetEvent Door { get; private set; }
            public byte[] ReadBuffer { get; private set; }
            public int ReadedSize { get; set; }
            
            public PipeData(NamedPipeServerStream pipe)
            {
                this.Server = pipe;
                this.Door = new AutoResetEvent(false);
                this.ReadBuffer = new byte[4096];
                
            }

            public NamedPipeServerStream Server { get; private set; }

            public void Dispose()
            {
               
                if (this.Server != null)
                {
                    this.Server.Dispose();
                    this.Server = null;
                }
                if (this.Door != null)
                {
                    this.Door.Dispose();
                    this.Door = null;
                }
            }
        }

        PipeData data;
        StreamReadWriter readerWriter;
        int readWriteTimeout;
        int connectTimeout;
        CancellationToken cancelToken;

        public PipeServer(string name,CancellationToken cancelToken, int connectTimeout = 5000, int readWriteTimeout = 5000,int numberOfInstances=1)
        {
            PipeSecurity pSecure = new PipeSecurity();
            // set access to every one, no security hole here
            pSecure.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
            pSecure.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
            pSecure.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
            


            var pipeServer = new NamedPipeServerStream(name, PipeDirection.InOut, numberOfInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1 * 1024 * 1024, 1 * 1024 * 1024, pSecure);
            this.connectTimeout = connectTimeout;
            this.readWriteTimeout = readWriteTimeout;
            this.cancelToken = cancelToken;
            data = new PipeData(pipeServer);
            // stream
            readerWriter = new StreamReadWriter(pipeServer, this.cancelToken,this.readWriteTimeout);
            
        }
        IAsyncResult connectAsyncResult = null;
        /// <summary>
        /// wait for a client to connect
        /// </summary>
        /// <exception cref="TimeoutException"></exception>
        public void WaitForConnection()
        {
            
            var asyncConnect = data.Server.BeginWaitForConnection(OnPipeConnected, data);
            connectAsyncResult = asyncConnect;
            int waitIndex = WaitHandle.WaitAny(new WaitHandle[] { data.Door, this.cancelToken.WaitHandle }, this.connectTimeout);
            if(waitIndex == 1 || waitIndex == WaitHandle.WaitTimeout || this.cancelToken.IsCancellationRequested)
            {
                throw new TimeoutException("Connection accept timeout");
            }
            

        }



       


        ~PipeServer()
        {
            this.Dispose();

        }

        /// <summary>
        /// dispose pattern
        /// </summary>
        public void Dispose()
        {
            if (isDisposed || isDisposing) return;
            isDisposing = true;
            
            if (data != null)
                data.Dispose();
            data = null;
            if (this.readerWriter != null)
                this.readerWriter.Dispose();
            this.readerWriter = null;

            isDisposed = true;
            isDisposing = false;
            
            
        }


        private bool isDisposed = false;

        private bool isDisposing = false;

        /// <summary>
        /// read data as string from client
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return this.readerWriter.ReadMsg();
        }

        /// <summary>
        /// write data as string to client
        /// </summary>
        /// <param name="msg"></param>
        public void WriteString(string msg)
        {
            this.readerWriter.WriteMsg(msg);
        }

         

      

        private void OnPipeConnected(IAsyncResult asyncResult)
        {
            var data = (PipeData)asyncResult.AsyncState;            
            if (!isDisposed && !isDisposing && data.Server != null )
            {
                data.Server.EndWaitForConnection(asyncResult);
                //Console.WriteLine("Client connected.");
            }
            if (!isDisposed && !isDisposing  && data.Door != null)
                data.Door.Set();


        }

        

       
    }
}
