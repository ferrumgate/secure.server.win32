using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace FerrumGateService
{
    public partial class Service : ServiceBase
    {
        
        public Service()
        {
            InitializeComponent();
            base.ServiceName = "FerrumGate";
            this.CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            
            base.EventLog.WriteEntry("Starting ferrumgate service");
            FerrumGate.StartAsService();
            
            base.EventLog.WriteEntry("Started ferrumgate service");
        }

        protected override void OnStop()
        {
            base.EventLog.WriteEntry("Stoping ferrumgate service");
            FerrumGate.StopAsService();
            base.EventLog.WriteEntry("Stoping ferrumgate service");
        }


        public void Start()
        {
            OnStart(null);
        }

        

        public void Stop2()
        {
            OnStop();
        }

   



        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
           
            
        }
        
        /// <summary>
        /// wait current service to stop,
        /// used for tests
        /// </summary>
        public void Wait()
        {
            FerrumGate.WaitAsService();
        }
    }
}
