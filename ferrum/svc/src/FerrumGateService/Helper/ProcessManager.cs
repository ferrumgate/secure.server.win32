using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using FerrumGateService.Helper.IPC;
using System.Configuration;

namespace FerrumGateService.Helper
{
    public class ProcessManager
    {
#if !DEBUG
        public const string ProcessName = "FerrumGate.exe";
#else
        public const string ProcessName = "powershell.exe";
#endif
        

        private static void CheckFileHash(string hash)
        {

            if (string.IsNullOrEmpty(hash))
            {
                Logger.Info("hash of process is empty");
                return;
            }
            using (FileStream st = new FileStream(ProcessName, FileMode.Open, FileAccess.Read))
            {
                bool result = Util.VerifySHA256(st, hash);
                if (!result)
                    throw new ApplicationException(ProcessName + " hash failed " + hash);
            }

        }

        private static int StartFerrum(string url, string pipename,string hash="")
        {





            try // we need this if pipe is successfull
            {

                CheckFileHash(hash);

                ProcessStartInfo startInfo = new ProcessStartInfo();
#if !DEBUG
  startInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..\\..\\..\\..\\..", ProcessName);
                var workerJS = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", "worker.js");
                            startInfo.Arguments = workerJS+" --url="+url+" --socket="+pipename;

                startInfo.EnvironmentVariables.Add("ELECTRON_RUN_AS_NODE", "true");
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = false;
                startInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\..");


#else
                startInfo.FileName = ProcessName;
#endif






                //startInfo.FileName = "C:/Users/test/Desktop/ferrum/secure.client/node_modules/.bin/electron.cmd";
                //startInfo.Arguments = "C:/Users/test/Desktop/ferrum/secure.client/build/src --win32";
                //startInfo.WorkingDirectory = "C:/Users/test/Desktop/ferrum/secure.client/build/src";

                Logger.Info(startInfo.FileName +" "+startInfo.Arguments +" process will start at directory "+startInfo.WorkingDirectory);
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    
                   
                    process.Start();

                    Logger.Info(startInfo.FileName + " process started with pid:"+process.Id);
                    return process.Id;

                }




            }
            catch (Exception ex)
            {

                Logger.Error(ex.GetAllMessages());
                throw ex;

            }






        }




        public static int Start(string url, string pipename, string hash = "")
        {

            Logger.Info("starting ferrum process");

            if (!CurrentUser.IsAdministrator())
            {
                Logger.Error("current user is not in administrators");
                throw new ApplicationException("current user is not in administrators");
            }


            return StartFerrum(url,pipename,hash);
        }




    }
}
