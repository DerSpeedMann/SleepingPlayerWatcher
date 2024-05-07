using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Xml;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.SleepingPlayerWatcher
{
    public class Plugin : RocketPlugin<Configuration>
    {
        public static Plugin Inst { get; private set; }
        public static Configuration Conf { get; private set; }

        public const string PluginVersion = "1.0.0";
        public const string PluginName = "SleepingPlayerWatcher";
        public const string Author = "SpeedMann";

        private string watchedPluginName = "SleepingPlayers";
        private string sleeperJsonName = "CurrentSleepers.json";
        private string sleeperConfigName = "SleepingPlayers.configuration.xml";
        private string sleeperStorageIdName = "SleepingPlayerStorageId";
        private string storageExtensionsName = "StorageExtensions";

        private string watchedPluginDirectory;
        private string sleeperJsonPath;
        private string sleeperConfigPath;
        private List<ushort> StorageIds;
        public override TranslationList DefaultTranslations =>
            new TranslationList
            {
            };

        #region Load
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;

            watchedPluginDirectory = Path.Combine(Rocket.Core.Environment.PluginsDirectory, watchedPluginName);
            sleeperJsonPath = Path.Combine(watchedPluginDirectory, sleeperJsonName);
            sleeperConfigPath = Path.Combine(watchedPluginDirectory, sleeperConfigName);

            if (Level.isLoaded)
            {
                OnPostLevelLoaded(0);
            }
            else
            {
                Level.onPostLevelLoaded += OnPostLevelLoaded;
            }

            PrintPluginInfo();
            Conf.updateConfig();
        }
        protected override void Unload()
        {
            Inst = null;
            Conf = null;

            Level.onPostLevelLoaded -= OnPostLevelLoaded;

            Logger.Log($"{PluginName} {PluginVersion} by {Author} Unloaded");
        }
    #endregion
        private void OnPostLevelLoaded(int level)
        {
            if (IsSleepingPlayersLoaded())
            {
                return;
            }
            Logger.LogWarning($"SleepingPlayers was not loaded, starting cleanup...");

            StorageIds = GetSleeperIds();
            if(StorageIds.Count <= 0)
            {
                return;
            }
            if (Conf.Debug)
            {
                string idString = "";
                foreach (ushort id in StorageIds)
                {
                    idString += $"{id}, ";
                }
                Logger.Log($"Found sleeper storage ids {idString}");
            }
            RemoveAllSleepers();
        }
        internal void RemoveAllSleepers()
        {
            List<BarricadeDrop> foundSleepers = new List<BarricadeDrop>();
            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.interactable is InteractableStorage storage && drop.asset is ItemStorageAsset storageAsset)
                    {
                        if (IsSleeperId(storageAsset.id) && storage != null)
                        {
                            while (storage.items.items.Count > 0)
                            {
                                storage.items.items.RemoveAt(0);
                            }
                            foundSleepers.Add(drop);
                        }
                    }
                }
            }
            foreach (BarricadeDrop foundSleeper in foundSleepers)
            {
                BarricadeManager.tryGetRegion(foundSleeper.model.transform, out var x, out var y, out var plant, out var innerRegion);
                BarricadeManager.destroyBarricade(foundSleeper, x, y, plant);
            }

            RemoveSleeperJson();
            Logger.Log($"Removed {foundSleepers.Count} SleepingPlayer storages");
        }

        #region HelperFunctions
        private void RemoveSleeperJson()
        {
            File.Delete(sleeperJsonPath);
        }
        private bool IsSleeperId(ushort id)
        {
            foreach (ushort sleeperId in StorageIds)
            {
                if (id == sleeperId)
                {
                    return true;
                }
            }
            return false;
        }
        private List<ushort> GetSleeperIds()
        {
            List<ushort> ids = new List<ushort>();

            if (!File.Exists(sleeperConfigPath))
            {
                return ids;
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(sleeperConfigPath);

                XmlNode sleepingPlayerStorageIdNode = xmlDoc.SelectSingleNode("/SleepingPlayerConfiguration/SleepingPlayerStorageId");
                ids.Add(Convert.ToUInt16(sleepingPlayerStorageIdNode.InnerText));

                XmlNodeList extensionNodes = xmlDoc.SelectNodes("/SleepingPlayerConfiguration/StorageExtensions/StorageExtension/ExtensionId");
                foreach (XmlNode extensionNode in extensionNodes)
                {
                    ids.Add(Convert.ToUInt16(extensionNode.InnerText));
                }
            }
            catch(Exception e)
            {
                Logger.LogError($"Failed reading Storage IDs from SleepingPlayers Config: {e}");
                ids.Clear();
            }

            return ids;
        }
        private bool IsSleepingPlayersLoaded()
        {
            RocketPlugin p = (RocketPlugin)R.Plugins.GetPlugins().Where(pl => pl.Name.ToLower().Contains(watchedPluginName.ToLower())).FirstOrDefault();
            return p != null;
        }
        private void PrintPluginInfo()
        {

            Logger.Log($"{PluginName} {PluginVersion} by {Author} Loaded");
            
        }
        #endregion
    }
}
