using FerrumGateService.Helper;
using System;


namespace FerrumGateService
{
    /// <summary>
    /// ipc server controller
    /// </summary>
    internal class FerrumGate
    {
        
        
        /// <summary>
        /// start ipc listening server
        /// </summary>
        public static void StartAsService()
        {

            try
            {
                
                IPCServer.Start("ferrumgate", 10, int.MaxValue, int.MaxValue);

            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
            }

        }
        /// <summary>
        /// stop ipc listening server
        /// </summary>
        public static void StopAsService()
        {
            IPCServer.Stop(); 

        }
        /// <summary>
        /// Waits current listening ipc server
        /// </summary>
        public static void WaitAsService()
        {
            IPCServer.Wait();

        }


    }


}
