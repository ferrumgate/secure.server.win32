using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using FerrumGateService.Helper.IPC;
using System.Configuration;
using FerrumGate.Helper;
using System.Management;

namespace FerrumGateService.Helper
{
    public class ProcessManager
    {

        public const string ProcessName = "FerrumGate.exe";


       private string sessionKey;
       public  ProcessManager(string sessionKey)
        {
            this.sessionKey = sessionKey;
        }


        private  void CheckFileHash(string hash)
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

      




        public  int StartAsCurrent(string url, string pipename)
        {

            Logger.Info("starting ferrum process");

            try // we need this if pipe is successfull
            {

                

                ProcessStartInfo startInfo = new ProcessStartInfo();

                  startInfo.FileName = "\""+ProcessManager.ExecuteFileName()+"\"" ;
                                var workerJS = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", "worker.js")+ "\"" ;
                                            startInfo.Arguments = workerJS+" --url="+url+" --socket="+pipename;

                                startInfo.EnvironmentVariables.Add("ELECTRON_RUN_AS_NODE", "true");
                                startInfo.CreateNoWindow = true;
                                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                startInfo.UseShellExecute = false;
                                startInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\..");






               
                

                Logger.Info(startInfo.FileName + " " + startInfo.Arguments + " process will start at directory " + startInfo.WorkingDirectory);
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

        public uint LastProcessId { get; set; }
        
        public void StartAsClient()
        {





            try // we need this if pipe is successfull
            {




                var fileName = ProcessManager.ExecuteFileName();
                
                
               
                var workingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\..");
                
                this.LastProcessId=ProcessExtensions.StartProcessAsCurrentUser(fileName, "\""+fileName+ "\" --token="+this.sessionKey, workingDirectory, true);
               
                Logger.Info(fileName + " " + " process will started at directory " + workingDirectory+ " pid:"+this.LastProcessId);

            }
            catch (Exception ex)
            {

                Logger.Error(ex.GetAllMessages());
                throw ex;

            }

        }
        public static string ExecuteFileName()
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\..", ProcessName));
        }

       
        public static string GetProcessPath(int id)
        {
            try
            {
                //var process = Process.GetProcessById(id);
                //if (process == null)
                //    throw new ApplicationException("Process not found ");
                //return process.MainModule.FileName;                //wmic process where "ProcessID=id" get ExecutablePath
                var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process where ProcessId="+id;
                using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                using (var results = searcher.Get())
                {
                    
                    foreach (var item in results)
                    {
                        // Do what you want with the Process, Path, and CommandLine
                        var path= item["ExecutablePath"] as String;
                        if (!String.IsNullOrEmpty(path))
                            return path;
                    }
                }
                throw new ApplicationException("Process not found ");


            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
                throw ex;
            }
        }



    }
}
