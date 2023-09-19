using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CraftedBossDrops
{
    public interface IPlugin
    {
        ConfigFile Config { get; }
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

        private void InitializeConfigWatcher()
        {
            string file = Path.GetFileName(plugin.Config.ConfigFilePath);
            string path = Path.GetDirectoryName(plugin.Config.ConfigFilePath);
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path, file);
            fileSystemWatcher.Changed += this.OnConfigFileChangedCreatedOrRenamed;
            fileSystemWatcher.Created += this.OnConfigFileChangedCreatedOrRenamed;
            fileSystemWatcher.Renamed += new RenamedEventHandler(this.OnConfigFileChangedCreatedOrRenamed);
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;

            Jotunn.Logger.LogDebug("Config watcher initialized.");
        }

        private void CheckForConfigManager()
        {
            if (GUIManager.IsHeadless())
            {
                InitializeConfigWatcher();
            }
            else
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
                {
                    this.configurationManager = configManagerInfo.Instance;
                    Jotunn.Logger.LogDebug("Configuration manager found, hooking DisplayingWindowChanged");
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
                    InitializeConfigWatcher();
                }
            }
        }

        private void OnConfigFileChangedCreatedOrRenamed(object sender, FileSystemEventArgs e)
        {
            string path = plugin.Config.ConfigFilePath;

            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                plugin.Config.Reload();
            }
            catch
            {
                Jotunn.Logger.LogError("There was an issue with your " + Path.GetFileName(path) + " file.");
                Jotunn.Logger.LogError("Please check the format and spelling.");
                return;
            }
        }

        private void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            Jotunn.Logger.LogDebug("OnConfigManagerDisplayingWindowChanged recieved.");
            PropertyInfo pi = this.configurationManager.GetType().GetProperty("DisplayingWindow");
            bool cmActive = (bool)pi.GetValue(this.configurationManager, null);

            if (!cmActive)
            {
                plugin.Config.Reload();
            }
        }
    }

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

    // use bepinex ConfigEntry settings
    public static class ConfigHelper
    {
        public static ConfigurationManagerAttributes GetAdminOnlyFlag()
        {
            return new ConfigurationManagerAttributes { IsAdminOnly = true };
        }

        public static ConfigurationManagerAttributes GetTags(Action<ConfigEntryBase> action)
        {
            return new ConfigurationManagerAttributes() { CustomDrawer = action };
        }

        public static ConfigurationManagerAttributes GetTags()
        {
            return new ConfigurationManagerAttributes();
        }

        public static ConfigEntry<T> Config<T>(this IPlugin instance, string group, string name, T value, ConfigDescription description)
        {
            return instance.Config.Bind(group, name, value, description);
        }

        public static ConfigEntry<T> Config<T>(this IPlugin instance, string group, string name, T value, string description) => Config(instance, group, name, value, new ConfigDescription(description, null, GetAdminOnlyFlag()));
    }

    public static class RequirementsEntry
    {
        public static RequirementConfig[] Deserialize(string reqs)
        {
            return reqs.Split(',').Select(r =>
            {
                string[] parts = r.Split(':');
                return new RequirementConfig
                {
                    Item = parts[0],
                    Amount = parts.Length > 1 && int.TryParse(parts[1], out int amount) ? amount : 1,
                    AmountPerLevel = parts.Length > 2 && int.TryParse(parts[2], out int apl) ? apl : 0,
                    Recover = true // defaulting to true
                };
            }).ToArray();
        }

        public static string Serialize(RequirementConfig[] reqs)
        {
            return string.Join(",", reqs.Select(r =>
                r.AmountPerLevel > 0 ?
                    $"{r.Item}:{r.Amount}:{r.AmountPerLevel}" :
                    $"{r.Item}:{r.Amount}"));
        }
    }

    public class Entries
    {
        public string Name { get; set; } = string.Empty;
        public ConfigEntry<string> Table { get; set; } = null;
        public ConfigEntry<int> MinTableLevel { get; set; } = null;
        public ConfigEntry<int> Amount { get; set; } = null;
        public ConfigEntry<string> Requirements { get; set; } = null;

        public static Entries GetFromProps(IPlugin instance, string name, string table, int minTableLevel, int amount, string requirements)
        {
            Entries entries = new Entries();
            entries.Name = name;
            entries.Table = instance.Config(entries.Name, "Table", table,
                new ConfigDescription($"Crafting station needed to craft {entries.Name}.", CraftingStations.GetAcceptableValueList(), ConfigHelper.GetAdminOnlyFlag()));
            entries.MinTableLevel = instance.Config(entries.Name, "Table Level", minTableLevel,
                new ConfigDescription($"Level of crafting station required to craft {entries.Name}.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Amount = instance.Config(entries.Name, "Amount", amount,
                new ConfigDescription($"The amount of {entries.Name} created.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Requirements = instance.Config<string>(entries.Name, "Requirements", requirements,
                new ConfigDescription($"The required items to craft {entries.Name}.", new AcceptableValueConfigNote("You must use valid spawn item codes."),
                new ConfigurationManagerAttributes() { IsAdminOnly = true, CustomDrawer = ConfigDrawers.DrawReqConfigTable() }));

            return entries;
        }

        private Action<object, EventArgs> _action = null;
        private void OnSettingChanged(object sender, EventArgs e)
        {
            if (_action != null)
            {
                _action(sender, e);
            }
        }
        public void AddSettingsChangedHandler(Action<object, EventArgs> action)
        {
            _action = action;
            Table.SettingChanged += OnSettingChanged;
            MinTableLevel.SettingChanged += OnSettingChanged;
            Amount.SettingChanged += OnSettingChanged;
            Requirements.SettingChanged += OnSettingChanged;
        }

        public void RemoveSettingsChangedHandler()
        {
            Table.SettingChanged -= OnSettingChanged;
            MinTableLevel.SettingChanged -= OnSettingChanged;
            Amount.SettingChanged -= OnSettingChanged;
            Requirements.SettingChanged -= OnSettingChanged;
            _action = null;
        }
    }

    public static class ConfigDrawers
    {
        private static BaseUnityPlugin configManager = null;

        private static BaseUnityPlugin GetConfigManager()
        {
            if (ConfigDrawers.configManager == null)
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
                {
                    ConfigDrawers.configManager = configManagerInfo.Instance;
                }
            }

            return ConfigDrawers.configManager;
        }

        private static int GetRightColumnWidth()
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

        public static Action<ConfigEntryBase> DrawReqConfigTable()
        {
            return cfg =>
            {
                List<RequirementConfig> newReqs = new List<RequirementConfig>();
                bool wasUpdated = false;

                int RightColumnWidth = GetRightColumnWidth();

                GUILayout.BeginVertical();

                List<RequirementConfig> reqs = RequirementsEntry.Deserialize((string)cfg.BoxedValue).ToList();

                foreach (var req in reqs)
                {
                    GUILayout.BeginHorizontal();

                    string newItem = GUILayout.TextField(req.Item, new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 40 - 21 - 21 - 9 });
                    string prefabName = string.IsNullOrEmpty(newItem) ? req.Item : newItem;
                    wasUpdated = wasUpdated || prefabName != req.Item;


                    int amount = req.Amount;
                    if (int.TryParse(GUILayout.TextField(amount.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 40 }), out int newAmount) && newAmount != amount)
                    {
                        amount = newAmount;
                        wasUpdated = true;
                    }

                    if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                    }
                    else
                    {
                        newReqs.Add(new RequirementConfig { Item = prefabName, Amount = amount });
                    }

                    if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                        newReqs.Add(new RequirementConfig { Item = "<Prefab Name>", Amount = 1 });
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                if (wasUpdated)
                {
                    cfg.BoxedValue = RequirementsEntry.Serialize(newReqs.ToArray());
                }
            };
        }
    }
}