using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SpeedMann.SleepingPlayerWatcher
{
    public class Configuration : IRocketPluginConfiguration
    {
        public string Version;
        public bool Debug;
        public void LoadDefaults()
        {
            Version = Plugin.PluginVersion;
            Debug = false;
        }
        public void updateConfig()
        {
            Version = Plugin.PluginVersion;

            Plugin.Inst.Configuration.Save();
        }
    }
}
