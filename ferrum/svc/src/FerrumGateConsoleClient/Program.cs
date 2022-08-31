using FerrumGateService.Helper.IPC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateConsoleClient
{
    /// <summary>
    /// a console client program for manuel testing
    /// </summary>
    internal class Program
    {

        /// <summary>
        /// install windows service
        /// </summary>
        static void Install()
        {
            try
            {
                using (PowerShell PowerShellInstance = PowerShell.Create())
                {
                    
                    var filename = System.IO.Path.Combine(Environment.CurrentDirectory, "FerrumGateService.exe");
                    //var filename = @"C:\WINDOWS\System32\svchost.exe -k netsvcs";
                    // use "AddScript" to add the contents of a script file to the end of the execution pipeline.
                    // use "AddCommand" to add individual commands/cmdlets to the end of the execution pipeline.
                    PowerShellInstance.AddCommand("New-Service")
                        .AddParameter("Name","FerrumGate")
                        .AddParameter("BinaryPathName", filename)
                        .AddParameter("DisplayName", "FerrumGate Zero Trust")
                        //.AddParameter("Credential", @".\LocalSystem")
                        .AddParameter("StartupType", "Automatic")
                        .AddParameter("Description", "Zero Trust Application Acess (ZTAA)")
                        .Invoke();
                   
                    Console.WriteLine("Service installed successfully");
                }

            }
            catch(Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
        /// <summary>
        /// install windows service
        /// </summary>
        static void Uninstall()
        {
            try
            {
               var pr= Process.Start("sc.exe", "delete FerrumGate");
                pr.WaitForExit();
                Console.WriteLine("Service uninstalled successfully");

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
        static void ExecuteCommand(PipeClient client,String command=null)
        {
            Console.Write("cmd > ");
            var cmd =command==null? Console.ReadLine():command;
            cmd = cmd.Replace("cmd > ","");
           
           if (cmd == "install")
            {
                Install();
                Environment.Exit(0);


            }else
             if (cmd == "uninstall")
            {
                Uninstall();
                Environment.Exit(0);


            }else
             if (cmd == "ping")
            {
                client?.WriteString("ping");
                var response=client?.ReadString();
                if(response!=null)
                Console.WriteLine(response);


            }else
            if (cmd == "exit")
            {
                client?.WriteString("exit");
                Thread.Sleep(1000);
                Environment.Exit(0);


            }
            else
             if (cmd == "disconnect")
            {
                client?.WriteString("disconnect");


            }else
            if (cmd.StartsWith("connect"))
            {
                client?.WriteString(cmd);
                var pipename=client?.ReadString();
                if (pipename != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            string[] splitter = { ":" };
                            pipename = pipename.Split(splitter, StringSplitOptions.RemoveEmptyEntries)[1];
                            Console.WriteLine("connecting sub pipe " + pipename);
                            using (CancellationTokenSource cts = new CancellationTokenSource())
                            using (PipeClient pipeclient = new PipeClient("ferrumgate_" + pipename, cts.Token,null,5000,int.MaxValue))
                            {
                                while (true)
                                {
                                    var msg=pipeclient.ReadString();
                                    Console.WriteLine(msg);
                                    if (msg == "process exit")
                                        break;
                                }
                            }

                        }
                        catch(Exception err)
                        {
                            Console.WriteLine(err.ToString());
                        }
                    });

                }
              


            }
            else
              if (cmd.StartsWith("wait"))
            {
                client?.WriteString(cmd);


            }else
            if (cmd.StartsWith("isWorking"))
            {
                client?.WriteString(cmd);
                
                var result = client?.ReadString();
                Console.WriteLine(result);


            }
            else
            
            {
                Console.WriteLine("usage:");
                Console.WriteLine("     install     #installs service");
                Console.WriteLine("     uninstall   #uninstalls service");
                Console.WriteLine("     exit        #exit");
                Console.WriteLine("     ping        #ping pong");
                Console.WriteLine("     connect $ $ #connects to $host $port ");
                Console.WriteLine("     disconnect  #disconnects");
                Console.WriteLine("     wait $      #wait $miliseconds");
                Console.WriteLine("     isWorking   #is working");
            }
        }
        static void Main(string[] args)
        {

            
            if (args.Length > 0)
            {
                ExecuteCommand(null, args[0]);
                Environment.Exit(0);
            }
            
                try
                {
                    using (var cs = new CancellationTokenSource())
                    using (var client = new PipeClient("ferrumgate", cs.Token,null,60000,60000))
                    {
                        
                        client.WriteString("ping");
                        client.ReadString();
                        Console.WriteLine("connected");
                        while (true)
                        {
                            ExecuteCommand(client);
                        }


                    }
                }catch(Exception err)
                {
                    Console.WriteLine(err.ToString());
                    Console.WriteLine("************************************");
                }
                   
            
        }
    }
}
