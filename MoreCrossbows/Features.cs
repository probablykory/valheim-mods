using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;

namespace MoreCrossbows
{

    internal class Feature
    {
        public Feature(string name)
        {
            Name = name;
            LoadedInGame = false;
            DependencyNames = new List<string>();
        }

        public bool RequiresUpdate { get; protected set; }
        public bool LoadedInGame { get; protected set; }

        // config data
        public bool EnabledByDefault { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        // entity data
        public string Table { get; set; }
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
        public virtual bool Update() { return false; }
    }


    internal class FeatureItem : Feature
    {
        public new ItemEntries Entries { get; protected set; }

        public FeatureItem(string name) : base(name) { }
        public string AssetPath { get; set; }
        public string Damages { get; set; } = string.Empty;
        public int Knockback { get; set; } = 0;

        private CustomItem _customItem = null;

        private void OnRecipeSettingChanged(object sender, EventArgs e)
        {
            Jotunn.Logger.LogDebug("OnEntrySettingChanged fired on feature " + Name);

            RequiresUpdate = true;
        }

        public override bool Initialize()
        {
            if (!string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(Description))
            {
                Entries = (ItemEntries) ItemEntries.GetFromFeature(MoreCrossbows.Instance, this); // TODO ugh, refactor
                Entries.AddSettingsChangedHandler(OnRecipeSettingChanged);
                EnabledConfigEntry = MoreCrossbows.Instance.Config(Category, "Enable" + Name, EnabledByDefault, Description);
            }
            return true;
        }

        public override bool Update()
        {
            RequiresUpdate = false;
            
            RecipeConfig newRecipe = new RecipeConfig()
            {
                Name = Entries.Name,
                Item = Entries.Name,
                CraftingStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                RepairStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : Requirements)
            };


            // Directly update the recipe
            Jotunn.Logger.LogDebug("Updating recipe for " + _customItem.ItemDrop.name);
            Jotunn.Logger.LogDebug("... reqs: " + Entries != null ? Entries.Requirements.Value : Requirements);
            _customItem.Recipe.Update(newRecipe);

            Jotunn.Logger.LogDebug("Overwriting damages of " + Name + " with : " + Entries.Damages.Value);
            setDamage(DamagesEntry.Deserialize(Entries.Damages.Value));

            return true;
        }

        public override bool Load()
        {
            Jotunn.Logger.LogDebug("Loading item " + Name);

            ItemConfig config = new ItemConfig()
            {
                CraftingStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                RepairStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : Requirements)
            };
            _customItem = new CustomItem(MoreCrossbows.Instance.assetBundle, AssetPath, true, config);

            Jotunn.Logger.LogDebug("Overwriting damages of " + Name + " with : " + Entries.Damages.Value);
            setDamage(DamagesEntry.Deserialize(Entries.Damages.Value));
            _customItem.ItemDrop.m_itemData.m_shared.m_attackForce = Knockback;

            ItemManager.Instance.AddItem(_customItem);
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Jotunn.Logger.LogDebug("Unloading item " + Name);
            ItemManager.Instance.RemoveItem(Name);
            ObjectDB.instance.Remove(Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
        }

        private void setDamage(Dictionary<string, int> dmgs)
        {
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_damage = dmgs.ContainsKey(DamageTypes.Damage) ? dmgs[DamageTypes.Damage] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_blunt = dmgs.ContainsKey(DamageTypes.Blunt) ? dmgs[DamageTypes.Blunt] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_slash = dmgs.ContainsKey(DamageTypes.Slash) ? dmgs[DamageTypes.Slash] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_pierce = dmgs.ContainsKey(DamageTypes.Pierce) ? dmgs[DamageTypes.Pierce] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_chop = dmgs.ContainsKey(DamageTypes.Chop) ? dmgs[DamageTypes.Chop] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_pickaxe = dmgs.ContainsKey(DamageTypes.Pickaxe) ? dmgs[DamageTypes.Pickaxe] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_fire = dmgs.ContainsKey(DamageTypes.Fire) ? dmgs[DamageTypes.Fire] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_frost = dmgs.ContainsKey(DamageTypes.Frost) ? dmgs[DamageTypes.Frost] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_lightning = dmgs.ContainsKey(DamageTypes.Lightning) ? dmgs[DamageTypes.Lightning] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_poison = dmgs.ContainsKey(DamageTypes.Poison) ? dmgs[DamageTypes.Poison] : 0;
            _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_spirit = dmgs.ContainsKey(DamageTypes.Spirit) ? dmgs[DamageTypes.Spirit] : 0;
        }
    }


    internal class FeatureRecipe : Feature
    {
        public FeatureRecipe(string name) : base(name) { }
        private void OnEntrySettingChanged(object sender, EventArgs e)
        {
            RequiresUpdate = true;
        }

        public override bool Initialize()
        {
            Entries = Entries.GetFromFeature(MoreCrossbows.Instance, this); // TODO ugh, refactor
            Entries.AddSettingsChangedHandler(this.OnEntrySettingChanged);
            EnabledConfigEntry = MoreCrossbows.Instance.Config(Category, "Enable" + Name, EnabledByDefault, Description);
            return true;
        }

        public override bool Update()
        {
            RequiresUpdate = false;

            RecipeConfig newRecipe = new RecipeConfig()
            {
                Name = Entries.Name,
                Item = Entries.Name,
                CraftingStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                RepairStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : Requirements)
            };

            var recipe = ItemManager.Instance.GetRecipe("CraftEarly" + Name);

            Jotunn.Logger.LogDebug("Updating recipe " + Entries.Name);
            Jotunn.Logger.LogDebug("... reqs: " + Entries != null ? Entries.Requirements.Value : Requirements);
            recipe.Update(newRecipe);


            RequiresUpdate = false;

            return true;
        }


        public override bool Load()
        {
            Jotunn.Logger.LogDebug("Loading recipe for " + Name);
            RecipeConfig config = new RecipeConfig()
            {
                Name = "CraftEarly" + Entries.Name,
                Item = Entries.Name,
                CraftingStation = CraftingTable.GetInternalName(Entries.Table.Value),
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
            Jotunn.Logger.LogDebug("Unloading recipe for " + Name);
            ItemManager.Instance.RemoveRecipe("CraftEarly" + Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
        }
    }
}
