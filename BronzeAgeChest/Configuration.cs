using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using System.Linq;
using Common;

namespace BronzeAgeChest
{
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
                new ConfigDescription($"Crafting station needed to construct {entries.Name}.", CraftingStations.GetAcceptableValueList(), ConfigHelper.GetAdminOnlyFlag()));
            entries.Requirements = instance.Config<string>(entries.Name, "Requirements", requirements,
                new ConfigDescription($"The required items to construct {entries.Name}.", new AcceptableValueConfigNote("You must use valid spawn item codes."),
                new ConfigurationManagerAttributes() { IsAdminOnly = true, CustomDrawer = SharedDrawers.DrawReqConfigTable() }));


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
