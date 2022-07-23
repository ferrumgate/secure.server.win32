
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace FerrumGateService
{
    //Artik wixtoolset kullandigimiz icin bu sinifa gerek yok
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            
        }
        void Info(string msg)
        {
           // File.AppendAllText("c:\\temp\\hamza.txt", DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n");
        }

      /*  protected override void OnAfterInstall(IDictionary savedState)
        {

            string src = this.Context.Parameters["InstallerPath"];



            string dst = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string destinationFile = Path.Combine(dst, Configuration.Filename);

            if (!File.Exists(destinationFile))
            {
                if (src == null)
                    throw new ApplicationException("InstallerPath not found");
                Info("OnAfterInstall->Installer path is:" + src);
                src = new FileInfo(src).DirectoryName;
                String sourcefile = Path.Combine(src, Configuration.Filename);
                File.Copy(sourcefile, destinationFile, true);
            }

            var service = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == this.serviceInstaller1.ServiceName);
            if (service != null)
            {
                
                if (service.Status != ServiceControllerStatus.Running)
                {
                    Info("OnAfterInstall->Service will start");
                    service.Start();
                }
                Info("OnAfterInstall->Service started");
            }
            else
            {
                Info("OnAfterInstall->Service not found");
            }


        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {

            var service = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == this.serviceInstaller1.ServiceName);
            if (service != null)
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    Info("OnBeforeUninstall->Service will stop");
                    service.Stop();
                }
                Info("OnBeforeUninstall->Service stoped");
            }
            else
            {
                Info("OnBeforeUninstall->Service not found");
            }
            //base.OnBeforeUninstall(savedState);
        }

        

        protected override void OnBeforeInstall(IDictionary savedState)
        {

            var service = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == this.serviceInstaller1.ServiceName);
            if (service != null)
            {
                if (service.Status == ServiceControllerStatus.Running)
                    service.Stop();

                var installer = new ServiceInstaller();
                var context = new InstallContext(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), null);
                installer.Context = context;
                installer.ServiceName = this.serviceInstaller1.ServiceName;
                installer.Uninstall(null);
                Info("OnBeforeInstall->Service uninstall");
            }
            else
            {
                Info("OnBeforeInstall->Service not found");
            }
        }*/


        //private void ProjectInstaller_AfterUninstall(object sender, InstallEventArgs e)
        //{
        //    Process.Start("sc.exe", "delete " + this.serviceInstaller1.ServiceName).WaitForExit();
        //}

        //private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
        //{
        //    Process.Start("sc.exe", "start " + this.serviceInstaller1.ServiceName).WaitForExit();
        //}

        //private void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
        //{
        //    string src = this.Context.Parameters["InstallerPath"];



        //    string dst = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    string destinationFile = Path.Combine(dst, Configuration.Filename);

        //    if (!File.Exists(destinationFile))
        //    {
        //        if (src == null)
        //            throw new ApplicationException("InstallerPath not found");
        //        src = new FileInfo(src).DirectoryName;
        //        String sourcefile = Path.Combine(src, Configuration.Filename);
        //        File.Copy(sourcefile, destinationFile, true);
        //    }

        //}

        //private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
        //{
        //    //File.WriteAllText("c:\\temp\\hamza.txt", "beforeintall");
        //    Process.Start("sc.exe", "delete " + this.serviceInstaller1.ServiceName).WaitForExit();
        //}










    }
}
