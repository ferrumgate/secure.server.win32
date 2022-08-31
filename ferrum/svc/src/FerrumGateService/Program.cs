using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
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

       




        [STAThread]
        public static void Main(string[] args)
        {   
            //no support for x86
            if (!Environment.Is64BitOperatingSystem)
            {
                Logger.Error("Environment is not 64 bit");
                Environment.Exit(1);
            }

            if (Environment.UserInteractive || (args.Length==1 && args[0]=="interactive"))
            {
               
                try
                {
                   
                    
                        AttachConsole(ATTACH_PARENT_PROCESS);
                    Console.WriteLine("process is interactive");


                    Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                       

                        
                        AppDomain.CurrentDomain.ProcessExit +=
                        (sender, eventArgs) =>
                        {
                           
                        };

                   var svc= new Service();
                    svc.Start();
                    svc.Wait();
                    Console.WriteLine("process exited");


                }
                catch (Exception ex)
                {
                    Logger.Error(ex.GetAllMessages());
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("process is not interactive");
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
