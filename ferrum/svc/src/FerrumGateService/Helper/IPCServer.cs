using FerrumGateService.Helper.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateService.Helper
{
    /// <summary>
    /// our ipc server protocol for managing connect and disconnect
    /// </summary>
    public class IPCServer
    {
        private static bool Work;
        private static Task currentTask;
        private static CancellationTokenSource cts;

        public enum Status
        {
            Unknown,Exit,Connect, Waiting, Connecting, Settings, Disconnecting
        }
        public static Status Where{get;set;}
        /**
         * create a server
         */
        public static Task Start(string pipeName, int maxInstanceCount, int connectTimeout = 2000, int readWriteTimeout = 5000)
        {
            IPCServer.currentTask = Task.Run(() =>
             {
                 IPCServer.Where = Status.Exit;
                 Work = true;
                 while (Work)
                 {
                     try
                     {
                         using (cts = new CancellationTokenSource())
                         using (PipeServer server = new PipeServer(pipeName, cts.Token, connectTimeout, readWriteTimeout, maxInstanceCount))
                         {
                             IPCServer.Where = Status.Connect;
                             server.WaitForConnection();
                             while (Work)
                             {
                                 var cmd = server.ReadString();
                                 Logger.Debug(String.Format("command received {0}", cmd));
                                 if (cmd == "exit")
                                 {
                                     IPCServer.Where = Status.Exit;
                                     break;
                                 }
                                 else
                                 if (cmd.StartsWith("wait")) // we need to sleep 
                                 {
                                     IPCServer.Where = Status.Waiting;
                                     var waitMSStr = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1];
                                     Thread.Sleep(Convert.ToInt32(waitMSStr));

                                 }
                                 else
                                 if (cmd.StartsWith("connect"))// connect to 
                                 {
                                     IPCServer.Where = Status.Connecting;
                                     var values = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                     ProcessManager.Start(server, values);

                                 }
                                 else
                                 if (cmd.StartsWith("settings"))
                                 {
                                     IPCServer.Where = Status.Settings;
                                     var values = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);


                                 }
                                 else
                                 if (cmd.StartsWith("disconnect"))
                                 {
                                     IPCServer.Where = Status.Disconnecting;

                                 }
                                 else
                                 {
                                     IPCServer.Where = Status.Unknown;
                                     break;

                                 }
                             }
                         }


                     }
                     catch (Exception ex)
                     {
                         Logger.Error(ex.GetAllMessages());
                     }
                 }            


             });
            return IPCServer.currentTask;
        }


        public static void Stop()
        {
            Work = false;
            IPCServer.cts.Cancel();
            if (IPCServer.currentTask != null)
            {
                IPCServer.currentTask.Wait();
            }

            

        }
      

    }
}
