using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using System.Linq;

namespace BronzeAgeChest
{

    public static class CraftingTable
    {
        public static string Inventory { get { return "Inventory"; } }
        public static string Workbench { get { return "Workbench"; } }
        public static string Cauldron { get { return "Cauldron"; } }
        public static string Forge { get { return "Forge"; } }
        public static string ArtisanTable { get { return "ArtisanTable"; } }
        public static string StoneCutter { get { return "StoneCutter"; } }
        public static string MageTable { get { return "MageTable"; } }
        public static string BlackForge { get { return "BlackForge"; } }

        public static string[] GetValues()
        {
            return new string[]
            {
                Inventory,
                Workbench,
                Cauldron,
                Forge,
                ArtisanTable,
                StoneCutter,
                MageTable,
                BlackForge
            };
        }

        public static string GetInternalName(string name)
        {
            switch (name)
            {
                case "Workbench":
                    return "piece_workbench";
                case "Cauldron":
                    return "piece_cauldron";
                case "Forge":
                    return "forge";
                case "ArtisanTable":
                    return "piece_artisanstation";
                case "StoneCutter":
                    return "piece_stonecutter";
                case "MageTable":
                    return "piece_magetable";
                case "BlackForge":
                    return "blackforge";
            }
            return string.Empty; // "Inventory" or error
        }
    }

    public interface IPlugin
    {
        ConfigFile Config { get; }
    }

    // use bepinex ConfigEntry settings
    internal static class ConfigHelper
    {
        public static ConfigurationManagerAttributes GetAdminOnlyFlag()
        {
            return new ConfigurationManagerAttributes { IsAdminOnly = true };
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
                    AmountPerLevel = parts.Length > 2 && int.TryParse(parts[2], out int apl) ? apl : 0
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

    internal class Entries
    {
        public string Name { get; set; } = string.Empty;
        public ConfigEntry<string> Table { get; set; } = null;
        public ConfigEntry<int> MinTableLevel { get; set; } = null;
        public ConfigEntry<int> Amount { get; set; } = null;
        public ConfigEntry<string> Requirements { get; set; } = null;

        public static Entries GetFromProps(IPlugin instance, string name, string table, string requirements)
        {
            Entries entries = new Entries();
            entries.Name = name;
            entries.Table = instance.Config(entries.Name, "Table", table,
                new ConfigDescription($"Crafting station needed to construct {entries.Name}.", new AcceptableValueList<string>(CraftingTable.GetValues()), ConfigHelper.GetAdminOnlyFlag()));
            entries.Requirements = instance.Config<string>(entries.Name, "Requirements", requirements,
                 new ConfigDescription($"The required items to construct {entries.Name}.", null, ConfigHelper.GetAdminOnlyFlag()));


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
}
