using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace FerrumGateService.Helper
{
    public class NetworkManager
    {
        private static void ExecuteCmd(string cmd)
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            //psi.Verb = "runas";
            psi.Arguments = cmd;
            var p = Process.Start(psi);
            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new ApplicationException("setting ip failed with:" + output + " " + error);

        }
        public static void SetIP(string interfacename,String ip, String route)
        {
            var interfaces=NetworkInterface.GetAllNetworkInterfaces();
            var ferrumInterface=interfaces.FirstOrDefault(x => x.Name == interfacename);
            if (ferrumInterface == null)
                throw new ApplicationException("ferrumgate interface does not exists");

            var index = ferrumInterface.GetIPProperties().GetIPv4Properties().Index;


            ExecuteCmd("/c netsh interface ipv4 set address \"" + interfacename + "\" static " + ip + " 255.255.255.255");
            ExecuteCmd("/c route ADD "+route+" "+ip+" IF "+index);

           


        }
    }
}
