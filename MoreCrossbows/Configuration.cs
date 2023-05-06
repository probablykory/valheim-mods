using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreCrossbows
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

    public static class DamageTypes
    {
        public static string Damage { get { return "Damage"; } }
        public static string Blunt { get { return "Blunt"; } }
        public static string Slash { get { return "Slash"; } }
        public static string Pierce { get { return "Pierce"; } }
        public static string Chop { get { return "Chop"; } }
        public static string Pickaxe { get { return "Pickaxe"; } }
        public static string Fire { get { return "Fire"; } }
        public static string Frost { get { return "Frost"; } }
        public static string Lightning { get { return "Lightning"; } }
        public static string Poison { get { return "Poison"; } }
        public static string Spirit { get { return "Spirit"; } }

        public static string[] GetValues()
        {
            return new string[]
            {
                Damage,
                Blunt,
                Slash,
                Pierce,
                Chop,
                Pickaxe,
                Fire,
                Frost,
                Lightning,
                Poison,
                Spirit
            };
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
        public override object Clamp(object value){return value;}
        public override bool IsValid(object value) { return !string.IsNullOrEmpty(value as string);  }

        public override string ToDescriptionString()
        {
            return "# Note: " + Note;
        }
    }

    public class AcceptableKeysString : AcceptableValueBase
    {
        public virtual string[] AcceptableKeys{ get; }

        public AcceptableKeysString(params string[] acceptableKeys)
            : base(typeof(string))
        {
            if (acceptableKeys == null)
            {
                throw new ArgumentNullException("acceptableValues");
            }
            if (acceptableKeys.Length == 0)
            {
                throw new ArgumentException("At least one acceptable key is needed", "AcceptableKeys");
            }
            this.AcceptableKeys = acceptableKeys;
        }

        public override object Clamp(object value)
        {
            return value; // passthrough
        }

        public override bool IsValid(object value)
        {
            // mostly passthrough
            return (value is string && !string.IsNullOrEmpty((string)value));
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable keys: " + string.Join(", ", this.AcceptableKeys.Select(x => x.ToString()).ToArray<string>());
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

    public static class DamagesEntry
    {
        public static Dictionary<string, int> Deserialize(string dmgs)
        {
            if (string.IsNullOrEmpty(dmgs)) return null;

            return dmgs.Split(',').Select(d =>
            {
                string[] parts = d.Split(':');
                if (parts.Length > 1)
                    return new KeyValuePair<string, int>(parts[0], int.TryParse(parts[1], out int amount) ? amount : 1);
                else
                    return new KeyValuePair<string, int>("Damage", int.TryParse(parts[0], out int amount) ? amount : 1);
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static string Serialize(Dictionary<string, int> dmgs)
        {
            return string.Join(",", dmgs.Select(d => $"{d.Key}:{d.Value}"));
        }
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

        public static Entries GetFromFeature(IPlugin instance, Feature config, Entries entries = null)
        {
            if (entries == null)
            {
                entries = new Entries();
            }
            entries.Name = config.Name;
            entries.Table = instance.Config(entries.Name, "Table", config.Table,
                new ConfigDescription($"Crafting station where {entries.Name} is available.", new AcceptableValueList<string>(CraftingTable.GetValues()), ConfigHelper.GetAdminOnlyFlag()));
            entries.MinTableLevel = instance.Config(entries.Name, "Table Level", config.MinTableLevel,
                new ConfigDescription($"Level of crafting station required to craft {entries.Name}.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Amount = instance.Config(entries.Name, "Amount", config.Amount,
                new ConfigDescription($"The amount of {entries.Name} created.", null, ConfigHelper.GetAdminOnlyFlag()));
            entries.Requirements = instance.Config<string>(entries.Name, "Requirements", config.Requirements,
                 new ConfigDescription($"The required items to craft {entries.Name}.", new AcceptableValueConfigNote("You must use valid spawn item codes or this will not work."), ConfigHelper.GetAdminOnlyFlag()));

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

    internal class ItemEntries: Entries
    {
        public ConfigEntry<string> Damages { get; set; } = null;

        public static Entries GetFromFeature(IPlugin instance, FeatureItem config)
        {
            ItemEntries entries = new ItemEntries();
            entries = (ItemEntries) GetFromFeature(instance, config, entries);
            entries.Damages = instance.Config<string>(entries.Name, "Damages", config.Damages,
                new ConfigDescription($"The damage done by {entries.Name}.", new AcceptableKeysString(DamageTypes.GetValues()), ConfigHelper.GetAdminOnlyFlag()));

            return entries;
        }

    }
}
