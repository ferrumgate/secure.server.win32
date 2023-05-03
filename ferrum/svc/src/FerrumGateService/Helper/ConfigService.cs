using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FerrumGateService
{
    public class Config
    {

        public List<String> hosts { get; set; }
        
    }
    public class ConfigService
    {

        private string configFile;
        public Config Config { get; set; }
        public ConfigService(String configFile)
        {
            this.configFile = configFile;
            this.Config = new Config() { hosts = new List<string>() };

            
        }
        public void Parse()
        {
            if (File.Exists(this.configFile)){
                var items = File.ReadAllText(this.configFile).Split(new string[] { "\r\n","\r","\n" }, StringSplitOptions.RemoveEmptyEntries);
                this.Config.hosts.AddRange(items);
                  
            }
        }
        

    }
}
