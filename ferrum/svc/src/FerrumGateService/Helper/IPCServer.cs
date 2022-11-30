using FerrumGateService.Helper.IPC;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

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
        private static string EncryptionKey;
        
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


            IPCServer.currentTask = Task.Run(async () =>
             {

                 //random encryption key
                 EncryptionKey = Guid.NewGuid().ToString().Replace("-", "");
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
                                  if (cmd.StartsWith("hello"))// start a new process
                                     {
                                         IPCServer.Where = Status.Connecting;
                                         try
                                         {
                                             var username= server.GetImpersonationUserName();
                                             Logger.Info("starting session for user "+username);
                                             var token = Util.EncryptString(IPCServer.EncryptionKey, Guid.NewGuid().ToString());
                                             var process = new ProcessManager(token);
                                             process.StartAsClient();
                                             server.WriteString("ok");
                                         }
                                         catch (Exception ex)
                                         {
                                             var msg = ex.GetAllMessages();
                                             server.WriteString("error:" + msg);
                                             throw ex;
                                         }


                                     }else
                                     if (cmd.StartsWith("connect"))// connect to 
                                 {
                                     IPCServer.Where = Status.Connecting;
                                         try
                                         {
                                             
                                             char[] separators = new char[] { ' ' };

                                             string[] subs = cmd.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                             var sessionKey = subs[1];
                                             Logger.Info("starting executor with key");
                                             var url = subs[2];
                                             var socket = subs[3];

                                             var guid= Util.DecryptString(IPCServer.EncryptionKey, sessionKey);
                                             Guid.Parse(guid);//check if it is a valid guid
                                            
                                             var process = new ProcessManager("");
                                             process.StartAsCurrent(url,socket);
                                             //server.RunAsClient(process.Start);                                             
                                             server.WriteString("ok");
                                         }
                                     catch (Exception ex){
                                             var msg= ex.GetAllMessages();
                                             server.WriteString("error:" + msg);
                                             throw ex;
                                    }
                                     

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
           
            if (IPCServer.currentTask != null)
            {
                cts.Cancel();
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
