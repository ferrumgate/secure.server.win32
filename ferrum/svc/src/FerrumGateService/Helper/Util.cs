using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FerrumGateService.Helper
{
    public class Util
    {

        internal static String GetHostName()
        {
            return Environment.MachineName;
        }

        public static String Mac(bool createRandom = false)
        {
            String firstMacAddress = NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.GetIPProperties().GatewayAddresses.Count>0)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();

            if (String.IsNullOrEmpty(firstMacAddress))
            {
                firstMacAddress = NetworkInterface
           .GetAllNetworkInterfaces()
           .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
           .Select(nic => nic.GetPhysicalAddress().ToString())
           .FirstOrDefault();

            }           

            if (string.IsNullOrEmpty(firstMacAddress)|| createRandom)
            {
                byte[] macarray = new byte[6];
                Random random = new Random((int)DateTime.Now.Ticks);
                random.NextBytes(macarray);
                firstMacAddress = "";
                
                firstMacAddress= BitConverter.ToString(macarray).Replace("-", string.Empty);
            }
              
            List<string> parts = new List<string>();
            for(int i = 0; i < firstMacAddress.Length; i += 2)
            {
                var t = firstMacAddress.Substring(i, 2).ToLower();
                parts.Add(t);
            }
            return String.Join( ":",parts);
        }

       

        internal static IList<String> Dnsservers()
        {
            IList<String> ips = new List<String>();

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                    foreach (IPAddress dnsAdress in dnsAddresses)
                    {
                        if (dnsAdress.ToString().StartsWith("fec0:"))
                            continue;
                        ips.Add(dnsAdress.ToString());
                    }
                }
            }


            return ips;
        }

        internal static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return ip.ToString();
                }
            }
            return null;
        }
       

      
        public static string GetOs()
        {
            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            return name != null ? name.ToString() : "Unknown";
        }

       
        public static bool IsPrivateIp(string ip)
        {
            return ip.StartsWith("10.") || ip.StartsWith("172.16.") || ip.StartsWith("192.168.") || ip.StartsWith("127.") || ip.StartsWith("169.254.") || ip.StartsWith("fe80:") || ip.StartsWith("fc00:") || ip == "::1";
        }

        private static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "").ToLower();
        }
        private static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }


       
        public static string ComputeSHA256(Stream ms)
        {
            // Salt size
            using (SHA256 mySHA256 = SHA256.Create())
            {
               
                
                    var data = mySHA256.ComputeHash(ms);
                    return ByteArrayToString(data);//hexencoded
                
            }
        }
        public static bool VerifySHA256(Stream datas,                               
                                  string hashValue)
        {
            // Hex-encoded hash
            var expectedHashString = ComputeSHA256(datas);
            return (hashValue == expectedHashString);
        }
        public static string GetFileSha256(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public static string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return BitConverter.ToString(array).Replace("-","");
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = 
            
                Enumerable.Range(0, cipherText.Length)
                                 .Where(x => x % 2 == 0)
                                 .Select(x => Convert.ToByte(cipherText.Substring(x, 2), 16))
                                 .ToArray();
            

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }






    }
    public static class Extensions
    {
        public static string GetAllMessages(this Exception exp)
        {
            if (exp == null) return "exception is null";
            List<string> messages = new List<string>();
            Exception innerException = exp;

            do
            {
                var message = (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                if (!string.IsNullOrEmpty(message))
                    messages.Add(message);
                innerException = innerException.InnerException;
            }
            while (innerException != null);

            return string.Join("->",messages);
        }
       
    }
}
