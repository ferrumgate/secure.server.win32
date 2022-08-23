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
            if (IPCServer.currentTask != null)
                return IPCServer.currentTask;

            IPCServer.currentTask = Task.Run(() =>
             {
                 IPCServer.Where = Status.Exit;
                 Work = true;
                 using(cts=new CancellationTokenSource())
                 while (Work)
                 {
                     try
                     {
                        
                         
                         using (PipeServer server = new PipeServer(pipeName, cts.Token, connectTimeout, readWriteTimeout, maxInstanceCount))
                         {
                             IPCServer.Where = Status.Connect;
                             Logger.Info("waiting for client");
                             server.WaitForConnection();
                             while (Work)
                             {
                                 Logger.Debug("waiting for command");
                                 var cmd = server.ReadString();
                                 Logger.Info(String.Format("command received > {0}", cmd));
                                 if (cmd == "ping")
                                 {
                                     server.WriteString("pong");
                                 }
                                 else
                                 if (cmd == "exit")
                                 {
                                     IPCServer.Where = Status.Exit;
                                     Work = false;
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
                                     //-F ssh_config -N -o "StrictHostKeyChecking no" - w any ferrum@192.168.43.44 -p3333
                                     var arguments = cmd.Replace("connect", "").Trim();
                                         using (AutoResetEvent are = new AutoResetEvent(false))//wait for pipe start
                                         {
                                             var pipename = ProcessManager.Start(arguments,are);
                                             are.WaitOne(3000);
                                             server.WriteString("connect to:" + pipename);
                                         }
                                     

                                 }
                                 else
                                   if (cmd.StartsWith("isWorking"))// connect to 
                                 {


                                     var isWorking = ProcessManager.IsWorking();
                                     server.WriteString(isWorking ? "true" : "false");

                                 }
                                 else
                                 if (cmd.StartsWith("settings"))
                                 {
                                     IPCServer.Where = Status.Settings;
                                     var values = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);


                                 }
                                 else
                                  if (cmd.StartsWith("network"))
                                     {
                                         IPCServer.Where = Status.Settings;
                                         var values = cmd.Substring(8).Trim().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                         //network assignedIp:${iplist.assignedIp} serviceNetwork:${iplist.serviceNetwork}`)
                                         if (values.Length == 2)
                                         {
                                             server.WriteString("network failed");

                                         }
                                         else
                                         {
                                            var assignedIP= values[0].Split(':')[1];
                                            var network = values[1].Split(':')[1];

                                         }


                                     }
                                     else
                                 if (cmd.StartsWith("disconnect"))
                                 {
                                     IPCServer.Where = Status.Disconnecting;
                                     ProcessManager.KillAllProcess(ProcessManager.ProcessName);

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
                         Thread.Sleep(1000);// if any error occures, wait 1s at least
                     }
                     finally
                     {
                       
                     }
                 }
                 
                 Logger.Info("ipc stopped");


             });
            return IPCServer.currentTask;
        }


        public static void Stop()
        {
            Work = false;
            ProcessManager.KillAllProcess(ProcessManager.ProcessName);            
            if (IPCServer.currentTask != null)
            {
                cts.Cancel();
                ProcessManager.KillAllProcess(ProcessManager.ProcessName);
                IPCServer.currentTask.Wait();
                IPCServer.currentTask = null;
                cts = null;
               
            }

            

        }
        public static void Wait()
        {
            if(IPCServer.currentTask!=null)
            IPCServer.currentTask.Wait();
        }
      

    }
}
