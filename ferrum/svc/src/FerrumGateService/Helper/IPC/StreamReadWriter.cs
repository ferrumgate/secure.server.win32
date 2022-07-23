using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateService.Helper.IPC
{
    class StreamReadWriter:IDisposable
    {
        internal PipeStream Stream { get; private set; }
        internal AutoResetEvent Door { get; private set; }
        internal byte[] Buffer { get; private set; }
        internal int ReadedSize { get;  set; }
        private int readWriteTimeout;
        List<byte[]> datas = new List<byte[]>();
        CancellationToken cancelToken;
        public StreamReadWriter(PipeStream stream,CancellationToken cancelToken, int readWriteTimeout=5000, int bufferSize=4096)
        {
            this.Stream = stream;
            this.Door = new AutoResetEvent(false);
            this.Buffer = new byte[bufferSize];
            this.readWriteTimeout = readWriteTimeout;
            this.cancelToken = cancelToken;
        }

        private bool IsDataBufferReady(out int readyBytes) 
        {
            int bytesCount = 0;
            if (datas.Sum(p => p.Length) >= 4)
            {
                bool firstByteOk = false;
                bool secondByteOk = false;
                bool thirdByteOk = false;
                bool fourthByteOk = false;

                foreach (var d in datas)
                {
                    foreach (var b in d)
                    {
                        if (!firstByteOk)
                        {
                            bytesCount += b << 24;
                            firstByteOk = true;
                            continue;
                        }
                        if (!secondByteOk)
                        {
                            bytesCount += b << 16;
                            secondByteOk = true;
                            continue;
                        }
                        if (!thirdByteOk)
                        {
                            bytesCount += b << 8;
                            thirdByteOk = true;
                            continue;
                        }
                        if (!fourthByteOk)
                        {
                            bytesCount += b;
                            fourthByteOk = true;
                            break;
                        }
                    }
                    if (fourthByteOk)
                        break;
                }
               
            }

        
            if((datas.Sum(p => p.Length) >= bytesCount + 4))//eğer gelen data uzunluğu ve listeye eklenenlerin toplam uzunluğu +4 ise
            {
                readyBytes = bytesCount;
                return true;
            }
            readyBytes = 0;           
            return false;
        }

        public string ReadMsg()
        {
           
            

            int howManyBytesToRead = 0;
            
            while (true)
            {
                if (IsDataBufferReady(out howManyBytesToRead))
                    break;
                var asyncRead = Stream.BeginRead(Buffer, 0, Buffer.Length, OnRead, this);
                int waitIndex = WaitHandle.WaitAny(new WaitHandle[] {Door,this.cancelToken.WaitHandle}, this.readWriteTimeout);
                if (waitIndex == 1 || waitIndex == WaitHandle.WaitTimeout || this.cancelToken.IsCancellationRequested)
                //if (!Door.WaitOne(this.readWriteTimeout))
                {
                    throw new TimeoutException("Read timeout");

                }
                if (ReadedSize == 0)
                {
                    throw new IOException("No data");
                }
                if (ReadedSize > 0)
                {
                    byte[] temp = new byte[ReadedSize];//gelen datayı kopyala
                    Array.Copy(Buffer, temp, ReadedSize);
                    datas.Add(temp);//gelen listesine ekle
                }               


            }
           

            byte[] tempBuffer = new byte[datas.Sum(p => p.Length)];
            int index = 0;
            foreach (var item in datas)
            {
                Array.Copy(item, 0, tempBuffer, index, item.Length);
                index += item.Length;
            }
            byte[] tempBuffer2 = new byte[howManyBytesToRead];
            Array.Copy(tempBuffer, 4, tempBuffer2, 0, howManyBytesToRead);
            datas.Clear();
            if (tempBuffer.Length - howManyBytesToRead - 4 > 0)
            {
                byte[] remainData = new byte[tempBuffer.Length - howManyBytesToRead - 4];
                Array.Copy(tempBuffer, 4 + howManyBytesToRead, remainData, 0, tempBuffer.Length - howManyBytesToRead - 4);
                datas.Add(remainData);
            }
            return Encoding.UTF8.GetString(tempBuffer2);
        }


        public void WriteMsg(string msg)
        {
           // File.WriteAllText(@"c:\\windows\Mudur\temp\"+DateTime.Now.Ticks + ".txt", msg, Encoding.UTF8);
            byte[] outBuffer = Encoding.UTF8.GetBytes(msg);
            int len = outBuffer.Length;
            if (len > Int32.MaxValue)
            {
                len = (int)Int32.MaxValue;
            }
            byte[] resultBuffer = new byte[outBuffer.Length + 4];


            resultBuffer[0] = ((byte)((len & 0xFF000000) >>24));
            resultBuffer[1] = ((byte)((len & 0x00FF0000) >>16));
            resultBuffer[2] = ((byte)((len & 0x0000FF00) >> 8));
            resultBuffer[3] = ((byte)(len & 0x000000FF));
            Array.Copy(outBuffer, 0, resultBuffer, 4, outBuffer.Length);
            
            this.Stream.BeginWrite(resultBuffer, 0, resultBuffer.Length, OnWrite, this);
            int waitIndex = WaitHandle.WaitAny(new WaitHandle[] { Door, this.cancelToken.WaitHandle }, this.readWriteTimeout);
            if (waitIndex == 1 || waitIndex == WaitHandle.WaitTimeout || this.cancelToken.IsCancellationRequested)
            //if (!this.Door.WaitOne(this.readWriteTimeout))
            {
                throw new TimeoutException("Write timeout");
            }
            //burası tcp gibi çalışmasını sağlar
           // Stream.WaitForPipeDrain();
        }


        private void OnRead(IAsyncResult asyncResult)
        {
            var data = (StreamReadWriter)asyncResult.AsyncState;
            if (data.Stream != null)
            {
                int readed = data.Stream.EndRead(asyncResult);
                data.ReadedSize = readed;

            }
            else
                data.ReadedSize = 0;
            if (data.Door != null)
                data.Door.Set();
        }

        private void OnWrite(IAsyncResult asyncResult)
        {
            var data = (StreamReadWriter)asyncResult.AsyncState;
            if (data.Stream != null)
            {
                data.Stream.EndWrite(asyncResult);

                
            }
            data.Stream.Flush();
            if (data.Door != null)
                data.Door.Set();
        }

        public void Dispose()
        {
            if (this.Door != null)
                this.Door.Dispose();
            this.Door = null;
        }
    }
}
