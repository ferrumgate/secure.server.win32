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

        public String host { get; set; }
    }
    public class ConfigService
    {

        private string configFile;
        public Config Config { get; set; }
        public ConfigService(String configFile)
        {
            this.configFile = configFile;
            
        }
        public void Parse()
        {
            if (File.Exists(this.configFile)){
               this.Config= JsonConvert.DeserializeObject<Config>(File.ReadAllText(this.configFile),
                   new JsonSerializerSettings
                   {
                       MissingMemberHandling = MissingMemberHandling.Ignore,                       
                   });
            }
        }
        public void Write(Config config)
        {

            File.WriteAllText(this.configFile, JsonConvert.SerializeObject(config));
            this.Config = config;
            
        }
        public string GetConfig()
        {
            if (this.Config!=null)
            {
                return JsonConvert.SerializeObject(this.Config);
            }
            return "";
        }

    }
}
