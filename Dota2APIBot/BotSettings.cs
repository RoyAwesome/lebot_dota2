using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Dota2APIBot
{
    public class BotSettings
    {
        public string BotName { get; set; }
        public List<string> BotChannels { get; set; }
        public List<string> TrustedUsers { get; set; }
        public List<string> TrustedChannels { get; set; }
        public string VDCLogin { get; set; }
        public string VDCPassword { get; set; }
        public bool Logging { get; set; }
        public string DatabaseFilename { get; set; }
        public List<string> Insults { get; set; }
        public List<string> OtherBots { get; set; }

        public Dictionary<string, string> VDCCaptchas { get; set; }

        public void Save()
        {
            File.WriteAllText("BotSettings.txt", JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
