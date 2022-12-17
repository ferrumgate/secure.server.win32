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
            try
            {
                IPCServer.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
            }

        }
        /// <summary>
        /// Waits current listening ipc server
        /// </summary>
        public static void WaitAsService()
        {
            try { 
            IPCServer.Wait();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetAllMessages());
            }

        }


    }


}
