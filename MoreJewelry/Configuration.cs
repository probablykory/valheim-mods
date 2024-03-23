using BepInEx.Bootstrap;
using BepInEx;
using Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;
using Logger = Managers.Logger;

namespace MoreJewelry
{

    public class AcceptableValueConfigNote : AcceptableValueBase
    {
        public virtual string Note { get; }

        public AcceptableValueConfigNote(string note) : base(typeof(string))
        {
            if (string.IsNullOrEmpty(note))
            {
                throw new ArgumentException("A string with atleast 1 character is needed", "Note");
            }
            this.Note = note;
        }

        // passthrough overrides
        public override object Clamp(object value) { return value; }
        public override bool IsValid(object value) { return !string.IsNullOrEmpty(value as string); }

        public override string ToDescriptionString()
        {
            return "# Note: " + Note;
        }
    }
    public class ConfigManagerStyle
    {
        public Color entryBgColor;
        public Color fontBgColor;
        public Color widgetBgColor;
        public Color windowBgColor;

        public Texture2D entryBgTexture = null!;
        public Texture2D widgetBgTexture = null!;
        public Texture2D windowBgTexture = null!;
    }

    // TODO - redesign this to be a proper reflection-based CMManager API

    public static class ConfigDrawers
    {
        public static bool HasConfigManager { get { return configManager != null; } }

        private static Assembly cmAssembly = null;
        private static BaseUnityPlugin configManager = null;
        private static BaseUnityPlugin GetConfigManager()
        {
            if (configManager == null)
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
                {
                    configManager = configManagerInfo.Instance;
                    cmAssembly = configManager.GetType().Assembly;
                }
            }

            return configManager;
        }

        public static T GetConfigManagerField<T>(string fieldName)
        {
            if (configManager == null) return default(T);

            var cm = GetConfigManager();
            object result = AccessTools.Field(cmAssembly.GetType("ConfigurationManager.ConfigurationManager"), fieldName).GetValue(GetConfigManager());
            if (result is T)
            {
                return (T)result;
            }
            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public static ConfigManagerStyle GetConfigManagerStyle()
        {
            if (configManager == null) return null;

            ConfigManagerStyle result = null;

            var tex = GetConfigManagerField<Texture2D>("<EntryBackground>k__BackingField");
            if (tex is not null)
                result = new ConfigManagerStyle() { entryBgTexture = tex };
            tex = GetConfigManagerField<Texture2D>("<WidgetBackground>k__BackingField");
            if (result is not null && tex is not null)
                result.widgetBgTexture = tex;
            tex = GetConfigManagerField<Texture2D>("<WindowBackground>k__BackingField");
            if (result is not null && tex is not null)
                result.windowBgTexture = tex;

            var entry = GetConfigManagerField<ConfigEntry<Color>>("_entryBackgroundColor");
            if (result is not null && entry is not null)
                result.entryBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_widgetBackgroundColor");
            if (result is not null && entry is not null)
                result.widgetBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_windowBackgroundColor");
            if (result is not null && entry is not null)
                result.windowBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_fontColor");
            if (result is not null && entry is not null)
                result.fontBgColor = entry.Value;

            return result;
        }

        public static int GetRightColumnWidth()
        {
            int result = 130;
            BaseUnityPlugin configManager = GetConfigManager();
            if (configManager != null)
            {
                PropertyInfo pi = configManager?.GetType().GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi != null)
                {
                    result = (int)pi.GetValue(configManager);
                }
            }

            return result;
        }

        public static void ReloadConfigDisplay()
        {
            BaseUnityPlugin configManager = GetConfigManager();
            if (configManager != null && configManager.GetType()?.GetProperty("DisplayingWindow")?.GetValue(configManager) is true)
            {
                configManager.GetType().GetMethod("BuildSettingList").Invoke(configManager, Array.Empty<object>());
            }
        }


        public static Action<ConfigEntryBase> DrawLocationsConfigTable()
        {
            _ = GetConfigManager();
            return cfg =>
            {
                List<string> newLocs = new List<string>();
                bool wasUpdated = false;

                int RightColumnWidth = GetRightColumnWidth();

                GUILayout.BeginVertical();

                List<string> locs = ((string)cfg.BoxedValue).Split(':').ToList();

                foreach (var loc in locs)
                {
                    GUILayout.BeginHorizontal();

                    string newLoc = GUILayout.TextField(loc, new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 21 - 21 - 9 }); // RightColumnWidth - 56 - 21 - 21 - 9 });
                    string name = string.IsNullOrEmpty(newLoc) ? loc : newLoc;
                    wasUpdated = wasUpdated || name != loc;

                    if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                    }
                    else
                    {
                        newLocs.Add(name);
                    }

                    if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                        newLocs.Add("<Location Name>");
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                if (wasUpdated)
                {
                    cfg.BoxedValue = string.Join(":", newLocs);
                }
            };
        }
    }


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
        }

        private void CheckForConfigManager()
        {
            if (Main.IsHeadless)
            {
                InitializeWatcher();
            }
            else
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
                {
                    this.configurationManager = configManagerInfo.Instance;
                    Logger.LogDebugOnly("Configuration manager found, hooking DisplayingWindowChanged");
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
                TriggerReload();
            }
            catch
            {
                Logger.LogError("There was an issue with your " + Path.GetFileName(path) + " file.");
                Logger.LogError("Please check the format and spelling.");
                return;
            }
        }

        private void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            PropertyInfo pi = this.configurationManager.GetType().GetProperty("DisplayingWindow");
            bool cmActive = (bool)pi.GetValue(this.configurationManager, null);

            if (!cmActive)
            {
                TriggerReload();
            }
        }

        private void TriggerReload()
        {
            bool prev = plugin.Config.SaveOnConfigSet;
            plugin.Config.SaveOnConfigSet = false;
            plugin.Config.Reload();
            plugin.Config.SaveOnConfigSet = prev;

        }
    }
}
