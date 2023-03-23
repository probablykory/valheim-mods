using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MoreCrossbows
{

    public static class Extensions
    {
        // A workaround until ItemManager.i.RemoveItem gets fixed.
        private static Dictionary<int, GameObject> itemsByHash = null;
        public static bool Remove(this ObjectDB instance, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            GameObject prefab = instance.GetItemPrefab(prefabName);
            if (prefab != null)
            {
                if (itemsByHash == null)
                {
                    var ibhMember = ObjectDB.instance.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == "m_itemByHash");
                    var ibhField = ibhMember as FieldInfo;
                    if (ibhField != null)
                    {
                        var dict = ibhField.GetValue(ObjectDB.instance) as Dictionary<int, GameObject>;
                        if (dict != null)
                        {
                            itemsByHash = dict;
                        }
                    }
                }

                instance.m_items.Remove(prefab);
                if (itemsByHash != null)
                {
                    itemsByHash.Remove(prefab.name.GetStableHashCode());
                }
                return true;
            }

            return false;
        }
    }

    internal class Feature
    {
        public Feature(string name)
        {
            Name = name;
            LoadedInGame = false;
            DependencyNames = new List<string>();
        }

        public bool RequiresUnload { get; protected set; }
        public bool LoadedInGame { get; protected set; }

        // config data
        public bool EnabledByDefault { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        // entity data
        public CraftingTable Table { get; set; }
        public int MinTableLevel { get; set; } = 1;
        public string Requirements { get; set; }
        public int Amount { get; set; } = 1;

        public List<string> DependencyNames { get; set; }

        // config entries
        public ConfigEntry<bool> EnabledConfigEntry { get; protected set; }
        public Entries Entries { get; protected set; }

        public virtual bool Initialize() { return false; }
        public virtual bool Load() { return false; }
        public virtual bool Unload() { return false; }
    }

    internal class FeatureItem : Feature
    {
        public FeatureItem(string name) : base(name) { }
        public string AssetPath { get; set; }

        private void OnEntrySettingChanged(object sender, EventArgs e)
        {
#if DEBUG
            Jotunn.Logger.LogInfo("OnEntrySettingChanged fired on feature " + Name);
#endif
            RequiresUnload = true;
        }

        public override bool Initialize()
        {
            if (!string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(Description))
            {
                Entries = Entries.GetFromFeature(this);
                Entries.AddSettingsChangedHandler(OnEntrySettingChanged);
                EnabledConfigEntry = ConfigHelper.Config(Category, "Enable" + Name, EnabledByDefault, Description);
            }
            return true;
        }

        public override bool Load()
        {
            Jotunn.Logger.LogInfo("Adding item " + Name);

            ItemConfig config = new ItemConfig()
            {
                CraftingStation = Entries != null ? Entries.Table.Value.InternalName() : Table.InternalName(),
                RepairStation = Entries != null ? Entries.Table.Value.InternalName() : Table.InternalName(),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : Requirements)
            };
            ItemManager.Instance.AddItem(new CustomItem(MoreCrossbows.Instance.assetBundle, AssetPath, true, config));
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Jotunn.Logger.LogInfo("Removing item " + Name);
            ItemManager.Instance.RemoveItem(Name);
            ObjectDB.instance.Remove(Name);
            LoadedInGame = false;
            RequiresUnload = false;

            return true;
        }
    }

    internal class FeatureRecipe : Feature
    {
        public FeatureRecipe(string name) : base(name) { }
        private void OnEntrySettingChanged(object sender, EventArgs e)
        {
            RequiresUnload = true;
        }

        public override bool Initialize()
        {
            Entries = Entries.GetFromFeature(this);
            Entries.AddSettingsChangedHandler(this.OnEntrySettingChanged);
            EnabledConfigEntry = ConfigHelper.Config(Category, "Enable" + Name, EnabledByDefault, Description);
            return true;
        }

        public override bool Load()
        {
            Jotunn.Logger.LogInfo("Adding recipe for " + Name);
            RecipeConfig config = new RecipeConfig()
            {
                Name = "CraftEarly" + Entries.Name,
                Item = Entries.Name,
                CraftingStation = Entries.Table.Value.InternalName(),
                MinStationLevel = Entries.MinTableLevel.Value,
                Amount = Entries.Amount.Value,
                Requirements = RequirementsEntry.Deserialize(Entries.Requirements.Value)
            };
            ItemManager.Instance.AddRecipe(new CustomRecipe(config));
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Jotunn.Logger.LogInfo("Removing recipe for " + Name);
            ItemManager.Instance.RemoveRecipe("CraftEarly" + Name);
            LoadedInGame = false;

            return true;
        }
    }
}
