using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
using FerrumGateService.UI;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace FerrumGateService
{
    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;


       

        static void Debug()
        {
           
            Console.WriteLine("deneme");
        }

       


        static void TestIPC()
        {

            while (true)
            {
                try
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource())
                    using (PipeServer server = new PipeServer("deneme", cts.Token, int.MaxValue, int.MaxValue))
                    {
                        server.WaitForConnection();
                        Console.WriteLine("client connected");
                        while (true)
                        {
                            var msg = server.ReadString();
                            Console.WriteLine("client sended:" + msg+ " len:"+msg.Length);
                            server.WriteString(msg);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetAllMessages());
                }
            }
        }


        [STAThread]
        public static void Main(string[] args)
        {
            


            if (Environment.UserInteractive)
            {

                try
                {
                   
                    
                    AttachConsole(ATTACH_PARENT_PROCESS);
                    

                   

                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                       

                        
                        AppDomain.CurrentDomain.ProcessExit +=
                        (sender, eventArgs) =>
                        {
                           
                        };
                       
                        Application.Run(new MainForm());
                    
                    Console.WriteLine("Process exited");


                }
                catch (Exception ex)
                {
                    Logger.Error(ex.GetAllMessages());
                    Environment.Exit(1);
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new FerrumGateService.Service()
                };
                ServiceBase.Run(ServicesToRun);
            }


        }


    }
}
