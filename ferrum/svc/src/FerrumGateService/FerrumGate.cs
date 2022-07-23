using FerrumGateService.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateService
{
    internal class FerrumGate
    {
        
        

        public static void StartAsService()
        {

            try
            {
                
                IPCServer.Start("ferrumgate", 10, int.MaxValue, 5000);

            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
            }

        }

        public static void StopAsService()
        {
            IPCServer.Stop(); 

        }
        
                     
    }


}
