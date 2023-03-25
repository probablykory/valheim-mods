using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using System.Linq;

namespace MoreCrossbows
{
    // use bepinex ConfigEntry settings for items+recipes
    internal static class ConfigHelper
    {
        public static ConfigurationManagerAttributes GetAdminOnlyFlag()
        {
            return new ConfigurationManagerAttributes { IsAdminOnly = true };
        }

        public static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description)
        {
            return MoreCrossbows.Instance.Config.Bind(group, name, value, description);
        }

        public static ConfigEntry<T> Config<T>(string group, string name, T value, string description) => Config(group, name, value, new ConfigDescription(description, null, GetAdminOnlyFlag()));

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
                    $"{r.Item}:{r.Amount}:{r.AmountPerLevel}":
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

        public static Entries GetFromFeature(Feature config)
        {
            Entries entries = new Entries();
            entries.Name = config.Name;
            entries.Table = ConfigHelper.Config(entries.Name, "Table", config.Table,
                new ConfigDescription($"Crafting station where {entries.Name} is available.", new AcceptableValueList<string>(CraftingTable.GetValues()), ConfigHelper.GetAdminOnlyFlag()));
            entries.MinTableLevel = ConfigHelper.Config(entries.Name, "Table Level", config.MinTableLevel,
                new ConfigDescription($"Level of crafting station required to craft {entries.Name}.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Amount = ConfigHelper.Config(entries.Name, "Amount", config.Amount,
                new ConfigDescription($"The amount of {entries.Name} created.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Requirements = ConfigHelper.Config<string>(entries.Name, "Requirements", config.Requirements,
                 new ConfigDescription($"The required items to craft {entries.Name}.", null, ConfigHelper.GetAdminOnlyFlag()));
            

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
        public void AddSettingsChangedHandler(Action<object,EventArgs> action)
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
