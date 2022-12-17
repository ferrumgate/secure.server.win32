using System;
using System;
using System.IO;
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
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
            {
                var hash = Util.ComputeSHA256(ms);
                Assert.AreEqual(hash, "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
            }
            
        }

        [TestMethod]
        public void TestVerifyHash()
        {
            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
            {
                var result = Util.VerifySHA256(ms, "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
                Assert.AreEqual(result, true);
            }

            using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
            {
                var result2 = Util.VerifySHA256(ms, "0c86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
                Assert.AreEqual(result2, false);
            }
        }

        [TestMethod]
        public void TestEncDec()
        {
            var key = (Guid.NewGuid().ToString()).Replace("-", "");
            var enc=Util.EncryptString(key, "efefda");
            var efefda = Util.DecryptString(key, enc);
            Assert.AreEqual(efefda, "efefda");

        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestEncDecThrows()
        {
            try
            {
                var key = (Guid.NewGuid().ToString()).Replace("-", "");
                var efefda = Util.DecryptString(key, "YXNkZmFzZGZhc2Rm");
            }catch(Exception err)
            {
                throw err;
            }

            

        }

    }
}
