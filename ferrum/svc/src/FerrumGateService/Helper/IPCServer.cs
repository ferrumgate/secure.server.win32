using FerrumGateService.Helper.IPC;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
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
        
       

        /**
         * create a server
         */
        public static Task Start(string pipeName, int maxInstanceCount, int connectTimeout = 2000, int readWriteTimeout = 5000)
        {
            if (IPCServer.currentTask != null)
                return IPCServer.currentTask;

            try
            {
                ConfigService config = new ConfigService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"config.json"));
                config.Parse();
                if (config.Config!=null)
                {
                    Logger.Info("config file loaded");
                }

                IPCServer.currentTask = Task.Run(async () =>
                     {

                     //random encryption key
                         EncryptionKey = Guid.NewGuid().ToString().Replace("-", "");
                         
                         Work = true;
                         using (cts = new CancellationTokenSource())
                             while (Work)
                             {
                                 try
                                 {

                                     using (PipeServer server = new PipeServer(pipeName, cts.Token, connectTimeout, readWriteTimeout, maxInstanceCount))
                                     {
                                         
                                         Logger.Info("waiting for client");
                                         server.WaitForConnection();
                                         while (Work)
                                         {
                                             Logger.Debug("waiting for command");
                                             var cmd = server.ReadString();
                                             //Logger.Info(String.Format("command received > {0}", cmd));
                                             if (cmd == "ping")
                                             {
                                                 server.WriteString("pong");
                                             }
                                             else
                                          if (cmd.StartsWith("hello") && false)// start a new process, we are not using
                                             {
                                                 
                                                 try
                                                 {
                                                     var username = server.GetImpersonationUserName();
                                                     Logger.Info("starting session for user " + username);
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


                                             }
                                             else
                                             if (cmd.StartsWith("connectWith") && false)// connect to , we are not using
                                             {
                                                 
                                                 try
                                                 {

                                                     char[] separators = new char[] { ' ' };

                                                     string[] subs = cmd.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                                     var sessionKey = subs[1];
                                                     Logger.Info("starting executor with key");
                                                     var url = subs[2];
                                                     var socket = subs[3];

                                                     var guid = Util.DecryptString(IPCServer.EncryptionKey, sessionKey);
                                                     Guid.Parse(guid);//check if it is a valid guid

                                                     var process = new ProcessManager("");
                                                     process.StartAsCurrent(url, socket);
                                                 //server.RunAsClient(process.Start);                                             
                                                     server.WriteString("ok");
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var msg = ex.GetAllMessages();
                                                     server.WriteString("error:" + msg);
                                                     throw ex;
                                                 }


                                             }
                                             else
                                             if (cmd.StartsWith("connect"))// connect to ,
                                             {
                                                 
                                                 try
                                                 {

                                                     char[] separators = new char[] { ' ' };

                                                     string[] subs = cmd.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                                                     
                                                     var url = subs[1];
                                                     var socket = subs[2];
                                                     //check if host is in valid hosts
                                                     if(config.Config !=null && config.Config.host != url)
                                                     {
                                                         throw new ApplicationException(url + " host is not validated");
                                                     }
    
                                                     var process = new ProcessManager("");
                                                     process.StartAsCurrent(url, socket);
                                                     //server.RunAsClient(process.Start);                                             
                                                     server.WriteString("ok");
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var msg = ex.GetAllMessages();
                                                     server.WriteString("error:" + msg);
                                                     throw ex;
                                                 }


                                             }else
                                             if (cmd.StartsWith("config"))// connect to 
                                             {
                                                 
                                                 try
                                                 {

                                                     var str = config.GetConfig();
                                                     server.WriteString("config:"+str);
                                                 }
                                                 catch (Exception ex)
                                                 {
                                                     var msg = ex.GetAllMessages();
                                                     server.WriteString("error:" + msg);
                                                     throw ex;
                                                 }


                                             }
                                             else
                                             {
                                                 
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

            }catch(Exception err)
            {
                Logger.Error(err.GetAllMessages());
            }
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
