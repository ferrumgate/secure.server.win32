using FerrumGateService.Helper;
using FerrumGateService.Helper.IPC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestProcessManager
    {
        [TestMethod]
        public void TestMethodStart()
        {
            
                
                
                   
            int pid= ProcessManager.Start("","");

            var pr= System.Diagnostics.Process.GetProcessById(pid);
            Assert.IsNotNull(pr);
            pr.Kill();



        }
        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestMethodCheckHash()
        {


            File.WriteAllText(ProcessManager.ProcessName, "test");
            

            int pid = ProcessManager.Start("","","somthing");


        }

        [TestMethod]
        
        public void TestMethodCheckHashNoException()
        {
            

            File.WriteAllText(ProcessManager.ProcessName, "test");
            using (var ms = new FileStream(ProcessManager.ProcessName, FileMode.Open)) {
                var hash = Util.ComputeSHA256(ms);

                int pid = ProcessManager.Start("", "", hash);
                var pr = System.Diagnostics.Process.GetProcessById(pid);
                Assert.IsNotNull(pr);
                pr.Kill();
            }


        }

    }
}
