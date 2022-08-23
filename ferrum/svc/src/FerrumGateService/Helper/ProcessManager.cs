using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using FerrumGateService.Helper.IPC;

namespace FerrumGateService.Helper
{
    public class ProcessManager
    {
        #if !DEBUG
        public const string ProcessName = "ssh_ferrum.exe";
        #else
        public const string ProcessName = "powershell.exe";
        #endif

        
        static Process ferrum = null;
        public static Task Task = null;
        
        public delegate void ProcessOutputHandler(string output);
        public static ProcessOutputHandler ProcessOutput = null;
        private static string StartFerrum(String args,AutoResetEvent are)
        {
               string pipeName = Guid.NewGuid().ToString().Replace("-", "");
            

                ProcessManager.Task= Task.Run(() =>
                {
                    KillAllProcess(ProcessName);

                    try
                    {
                        Console.WriteLine("opening sub pipe ferrumgate_" + pipeName);
                        using (var cts = new CancellationTokenSource())
                        using (PipeServer pipe = new PipeServer("ferrumgate_" + pipeName, cts.Token, 5000, int.MaxValue, 10))
                        {
                            are.Set();//pipe opened
                            pipe.WaitForConnection();

                            try // we need this if pipe is successfull
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo();
#if !DEBUG
                                startInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "External", Environment.Is64BitOperatingSystem ? "x64" : "x86", ProcessName);
#else
                            startInfo.FileName = ProcessName;
#endif


                                startInfo.CreateNoWindow = true;
                                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                startInfo.UseShellExecute = false;
                                startInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "External", Environment.Is64BitOperatingSystem ? "x64" : "x86");
                                startInfo.RedirectStandardOutput = true;
                                startInfo.RedirectStandardError = true;
                                startInfo.RedirectStandardInput = true;

                                startInfo.Arguments = args;

                                Logger.Info(startInfo.FileName + " process starting");
                                using (Process process = new Process())
                                {
                                    process.StartInfo = startInfo;

                                    process.OutputDataReceived += (s, e) =>
                                    {
                                        try
                                        {
                                            if (pipe != null && !string.IsNullOrEmpty(e.Data))
                                                pipe.WriteString(e.Data);
                                            if (ProcessOutput != null && !string.IsNullOrEmpty(e.Data))
                                                ProcessOutput(e.Data);
                                        }
                                        catch (Exception ignored) { }
                                    };
                                    process.ErrorDataReceived += (s, e) =>
                                    {
                                        try
                                        {
                                            if (pipe != null && !string.IsNullOrEmpty(e.Data))
                                                pipe.WriteString(e.Data);
                                            if (ProcessOutput != null && !string.IsNullOrEmpty(e.Data))
                                                ProcessOutput(e.Data);
                                        }
                                        catch (Exception ignored) { }
                                    };
                                    ferrum = process;
                                    Logger.Info(startInfo.FileName + " process started");
                                    process.Start();
                                    pipe.WriteString("process started:");

                                    process.BeginErrorReadLine();
                                    process.BeginOutputReadLine();
                                    process.WaitForExit();
                                    Logger.Info(startInfo.FileName + " process finished");

                                    pipe.WriteString("process exit:");
                                }
                            }
                            catch (Exception err)
                            {
                                try//if pipe is broken, make it safe
                                {
                                    pipe.WriteString(err.ToString());
                                    pipe.WriteString("process exit:");
                                }
                                catch (Exception ignored)
                                { };
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        ferrum = null;
                        Logger.Error(ex.GetAllMessages());

                    }
                    finally
                    {
                        KillAllProcess(ProcessName);

                        ProcessManager.Task = null;
                    }

                });
  
            return pipeName;
           
        }

        private static void KillProcess(Process process)
        {
            try
            {
                Logger.Info("killing process " + process.ProcessName);
                process.Kill();

            }catch(Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
            }
        }

        public static void KillAllProcess(String name)
        {
            try
            {
                Logger.Info("gettingprocess by name " + name);
                System.Diagnostics.Process[] process = Process.GetProcessesByName(name.Replace(".exe",""));
                Logger.Info("gettingprocess by name " + name+" found:"+process.Length);
                process.ToList().ForEach((x) =>
                {
                    KillProcess(x);

                });
            }
            catch (Exception ex)
            {
                Logger.Error("killing process failed:"+ex.GetAllMessages());
            }
        }
        public static string Start(string args,AutoResetEvent are)
        {
            
            Logger.Info("starting ferrum process");
            if (!CurrentUser.IsAdministrator())
            {
                Logger.Error("current user is not in administrators");
                throw new ApplicationException("current user is not in administrators");
            }
            
            return StartFerrum(args,are);
        }



        public static void Stop()
        {
            Logger.Info("stoping ferrum process");
            
            if (ferrum!=null)
            {
                try
                {
                    ferrum.Kill();
                }catch(Exception ignore)
                {

                }
            }
            KillAllProcess(ProcessName);
            Logger.Info("stoped service");
        }

        public static bool IsWorking()
        {
            System.Diagnostics.Process[] process = Process.GetProcessesByName(ProcessName.Replace(".exe", ""));
            return process.Length> 0;
        }
    }
}
