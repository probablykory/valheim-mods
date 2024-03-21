using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Common
{
    public class ConfigWatcher
    {
        private BaseUnityPlugin configurationManager;
        private IPlugin plugin;

        public ConfigWatcher(IPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            this.plugin = plugin;
            CheckForConfigManager();
        }

        private void InitializeWatcher()
        {
            string file = Path.GetFileName(plugin.Config.ConfigFilePath);
            string path = Path.GetDirectoryName(plugin.Config.ConfigFilePath);

            var watcher = new Watcher(path, file);
            watcher.FileChanged += OnFileChanged;

            Get.Plugin.LogDebugOnly("File system watcher initialized.");
        }

        private void CheckForConfigManager()
        {
            if (GUIManager.IsHeadless())
            {
                InitializeWatcher();
            }
            else
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
                {
                    this.configurationManager = configManagerInfo.Instance;
                    Get.Plugin.LogDebugOnly("Configuration manager found, hooking DisplayingWindowChanged");
                    EventInfo eventinfo = this.configurationManager.GetType().GetEvent("DisplayingWindowChanged");
                    if (eventinfo != null)
                    {
                        Action<object, object> local = new Action<object, object>(this.OnConfigManagerDisplayingWindowChanged);
                        Delegate converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);
                        eventinfo.AddEventHandler(this.configurationManager, converted);
                    }
                }
                else
                {
                    InitializeWatcher();
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string path = plugin.Config.ConfigFilePath;

            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                plugin.Config.SaveOnConfigSet = false;
                plugin.Config.Reload();
                plugin.Config.SaveOnConfigSet = true;
            }
            catch
            {
                Get.Plugin.LogError("There was an issue with your " + Path.GetFileName(path) + " file.");
                Get.Plugin.LogError("Please check the format and spelling.");
                return;
            }
        }

        private void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            PropertyInfo pi = this.configurationManager.GetType().GetProperty("DisplayingWindow");
            bool cmActive = (bool)pi.GetValue(this.configurationManager, null);

            if (!cmActive)
            {
                plugin.Config.SaveOnConfigSet = false;
                plugin.Config.Reload();
                plugin.Config.SaveOnConfigSet = true;
            }
        }
    }
}
