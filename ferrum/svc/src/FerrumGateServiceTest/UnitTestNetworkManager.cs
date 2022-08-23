using System;
using System.Net.NetworkInformation;
using FerrumGateService.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestNetworkManager
    {
        [TestMethod]
        public void TestSetIpNotFoundException()
        {
            bool exceptionOccured = false;
            try
            {
                NetworkManager.SetIP("ferrumx", "192.168.1.2", "100.0.0.0/24");
            }catch(Exception er)
            {
                exceptionOccured = true;
                Assert.IsInstanceOfType(er, typeof(ApplicationException));
                Assert.IsTrue(er.ToString().Contains("interface does not"));
            }
            Assert.IsTrue(exceptionOccured);
        }

        [TestMethod]
        public void TestSetIp()
        {
            //this test need administrator priviliges
            var interfaces=NetworkInterface.GetAllNetworkInterfaces();
            NetworkManager.SetIP(interfaces[0].Name, "192.168.1.3", "100.0.0.0/24");

            interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var iplist= interfaces[0].GetIPProperties().UnicastAddresses;
            bool ipFound = false;
            foreach(var ip in iplist)
            {
                if (ip.Address.ToString() == "192.168.1.2")
                    ipFound = true;
            }
            Assert.IsTrue(ipFound);

        }
        [TestMethod]
        public void TestSetIpThrowsException()
        {
            //this test need administrator priviliges
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            bool isExceptionOccured = false;
            try
            {
                NetworkManager.SetIP(interfaces[0].Name, "192.1681", "100.0.0.0/24");
            }catch(Exception err)
            {
                isExceptionOccured = true;
                Assert.IsTrue(err.ToString().Contains("setting ip failed"));
            }
                      
            Assert.IsTrue(isExceptionOccured);

        }



    }
}
