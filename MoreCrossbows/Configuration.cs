using BepInEx.Configuration;
using Jotunn.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using UnityEngine;

namespace MoreCrossbows
{
    public static class DamageTypes
    {
        public static string Damage { get { return nameof(Damage); } }
        public static string Blunt { get { return nameof(Blunt); } }
        public static string Slash { get { return nameof(Slash); } }
        public static string Pierce { get { return nameof(Pierce); } }
        public static string Chop { get { return nameof(Chop); } }
        public static string Pickaxe { get { return nameof(Pickaxe); } }
        public static string Fire { get { return nameof(Fire); } }
        public static string Frost { get { return nameof(Frost); } }
        public static string Lightning { get { return nameof(Lightning); } }
        public static string Poison { get { return nameof(Poison); } }
        public static string Spirit { get { return nameof(Spirit); } }

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

    public static class DamagesDict
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

    internal class Entries
    {
        public static Dictionary<ConfigurationManagerAttributes, Entries> SavedAttributes = new Dictionary<ConfigurationManagerAttributes, Entries>();

        protected bool visible = true;

        public string Name { get; set; } = string.Empty;
        public ConfigEntry<string> Table { get; set; } = null;
        public ConfigEntry<int> MinTableLevel { get; set; } = null;
        public ConfigEntry<int> Amount { get; set; } = null;
        public ConfigEntry<string> Requirements { get; set; } = null;

        public static ConfigurationManagerAttributes GetAttribute(Entries entries, bool isAdminOnly = true, bool isBrowsable = true, Action<ConfigEntryBase> customDrawer = null)
        {
            var cma = new ConfigurationManagerAttributes() { Browsable = isBrowsable, IsAdminOnly = isAdminOnly, CustomDrawer = customDrawer };
            SavedAttributes.Add(cma, entries);
            return cma;
        }

        public static void UpdateBrowsable()
        {
            foreach(KeyValuePair<ConfigurationManagerAttributes, Entries> kvp in SavedAttributes)
            {
                kvp.Key.Browsable = kvp.Value.visible;
            }
        }

        public static Entries GetFromFeature(IPlugin instance, Feature config, Entries entries = null, bool visible = true)
        {
            var hasUpgrades = (config.Type == Feature.FeatureType.Crossbow);

            if (entries == null)
            {
                entries = new Entries();
            }
            entries.visible = visible;
            entries.Name = config.Name;
            entries.Table = instance.Config(entries.Name, "Table", config.Table,
                new ConfigDescription($"Crafting station where {entries.Name} is available.", CraftingStations.GetAcceptableValueList(), GetAttribute(entries, true, visible)));
            entries.MinTableLevel = instance.Config(entries.Name, "Table Level", config.MinTableLevel,
                new ConfigDescription($"Level of crafting station required to craft {entries.Name}.", null, GetAttribute(entries, entries.visible, true)));
            entries.Amount = instance.Config(entries.Name, "Amount", config.Amount,
                new ConfigDescription($"The amount of {entries.Name} created.", null, GetAttribute(entries, entries.visible, true)));
            entries.Requirements = instance.Config<string>(entries.Name, "Requirements", config.Requirements,
                new ConfigDescription($"The required items to craft {entries.Name}.", new AcceptableValueConfigNote("You must use valid spawn item codes."),
                GetAttribute(entries, entries.visible, true, SharedDrawers.DrawReqConfigTable(hasUpgrades))));

            return entries;
        }

        public void SetVisibility(bool visible)
        {
            this.visible = visible;
            UpdateBrowsable();
        }

        private Action<object, EventArgs> _action = null;
        protected void OnSettingChanged(object sender, EventArgs e)
        {
            if (_action != null)
            {
                _action(sender, e);
            }
        }
        public virtual void AddSettingsChangedHandler(Action<object,EventArgs> action)
        {
            _action = action;
            Table.SettingChanged += OnSettingChanged;
            MinTableLevel.SettingChanged += OnSettingChanged;
            Amount.SettingChanged += OnSettingChanged;
            Requirements.SettingChanged += OnSettingChanged;
        }

        public virtual void RemoveSettingsChangedHandler()
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

        public override void AddSettingsChangedHandler(Action<object, EventArgs> action)
        {
            base.AddSettingsChangedHandler(action);

            Damages.SettingChanged += OnSettingChanged;
        }

        public override void RemoveSettingsChangedHandler()
        {
            base.RemoveSettingsChangedHandler();

            Damages.SettingChanged -= OnSettingChanged;
        }

        public static ItemEntries GetFromFeature(IPlugin instance, FeatureItem config, bool visible = true)
        {
            ItemEntries entries = new ItemEntries();
            entries = (ItemEntries) GetFromFeature(instance, config, entries, visible);
            entries.Damages = instance.Config<string>(entries.Name, "Damages", config.Damages,
                new ConfigDescription($"The damage done by {entries.Name}.", new AcceptableKeysString(DamageTypes.GetValues()),
                GetAttribute(entries, true, entries.visible, DamagesEntry.DrawDamagesConfigTable())));

            return entries;
        }

    }

    public class DamagesConfig
    {
        public string Prefab;
        public int Duration = 0;
    }

    public static class DamagesEntry
    {
        public static DamagesConfig[] Deserialize(string dmgs)
        {
            return dmgs.Split(',').Select(r =>
            {
                string[] parts = r.Split(':');
                return new DamagesConfig
                {
                    Prefab = parts[0],
                    Duration = parts.Length > 1 && int.TryParse(parts[1], out int duration) ? duration : 1
                };
            }).ToArray();
        }

        public static string Serialize(DamagesConfig[] dmgs)
        {
            return string.Join(",", dmgs.Select(r =>
                    $"{r.Prefab}:{r.Duration}"));
        }

        public static Action<ConfigEntryBase> DrawDamagesConfigTable()
        {
            return cfg =>
            {
                List<DamagesConfig> newDamages = new List<DamagesConfig>();
                bool wasUpdated = false;

                int RightColumnWidth = SharedDrawers.GetRightColumnWidth();

                GUILayout.BeginVertical();

                List<DamagesConfig> damages = Deserialize((string)cfg.BoxedValue).ToList();

                foreach (var bossPower in damages)
                {
                    GUILayout.BeginHorizontal();

                    string newPrefab = GUILayout.TextField(bossPower.Prefab, new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 56 - 21 - 21 - 9 });
                    string prefabName = string.IsNullOrEmpty(newPrefab) ? bossPower.Prefab : newPrefab;
                    wasUpdated = wasUpdated || prefabName != bossPower.Prefab;


                    int duration = bossPower.Duration;
                    if (int.TryParse(GUILayout.TextField(duration.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 56 }), out int mewDuration) && mewDuration != duration)
                    {
                        duration = mewDuration;
                        wasUpdated = true;
                    }

                    if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                    }
                    else
                    {
                        newDamages.Add(new DamagesConfig { Prefab = prefabName, Duration = duration });
                    }

                    if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }))
                    {
                        wasUpdated = true;
                        newDamages.Add(new DamagesConfig { Prefab = "<Damage Type>", Duration = 120 });
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();

                if (wasUpdated)
                {
                    cfg.BoxedValue = Serialize(newDamages.ToArray());
                }
            };
        }
    }
}
