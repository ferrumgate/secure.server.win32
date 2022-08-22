using System;
using FerrumGateService.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FerrumGateServiceTest
{
    [TestClass]
    public class UnitTestUtil
    {
        [TestMethod]
        public void TestMac()
        {
            String[] splitter = { ":" };
            String mac = Util.Mac();
            Assert.AreEqual(mac.Length, 12 + 5);
            Assert.IsTrue(mac.Contains(":"));
            Assert.AreEqual(mac.Split(splitter, StringSplitOptions.RemoveEmptyEntries).Length, 6);
        }

        [TestMethod]
        public void TestMacRandom()
        {
            String[] splitter = { ":" };
            String mac = Util.Mac(true);
            Assert.AreEqual(mac.Length, 12 + 5);
            Assert.IsTrue(mac.Contains(":"));
            Assert.AreEqual(mac.Split(splitter, StringSplitOptions.RemoveEmptyEntries).Length, 6);
        }


      


      


        [TestMethod]
        public void TestSHA256()
        {
            var hash= Util.ComputeSHA256("test");
            Assert.AreEqual(hash, "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
            
        }

        [TestMethod]
        public void TestVerifyHash()
        {
            var result = Util.VerifySHA256("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
            Assert.AreEqual(result, true);

            var result2 = Util.VerifySHA256("test", "0c86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
            Assert.AreEqual(result2, false);
        }

    }
}
