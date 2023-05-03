using System;
using System;
using System.IO;
using FerrumGateService;
using FerrumGateService.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestConfigService
    {
        [TestMethod]
        public void TestParseWrite()
        {
            File.WriteAllLines("tmp.txt", new string[] { "test", "test2" });
            var config = new ConfigService("tmp.txt");
            config.Parse();
            Assert.IsTrue(config.Config.hosts.Count == 2);



        }

       

    }
}
