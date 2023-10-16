using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using UnityEngine;
using Common;
using System.Linq;
using System.Collections.Generic;

namespace CraftedBossDrops
{
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