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
            var config = new ConfigService("config.json");
            config.Write(new Config { host ="deneme"});

            var config2 = new ConfigService("config.json");
            config2.Parse();
            Assert.AreEqual(config2.Config.host, "deneme");

        }

       

    }
}
