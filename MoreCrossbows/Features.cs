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
        public FeatureItem(string name) : base(name) { }
        public string AssetPath { get; set; }

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
                Entries = Entries.GetFromFeature(MoreCrossbows.Instance, this); // TODO ugh, refactor
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

            return true;
        }

        public override bool Load()
        {
            Jotunn.Logger.LogInfo("Loading item " + Name);

            ItemConfig config = new ItemConfig()
            {
                CraftingStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                RepairStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : Table),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : Requirements)
            };
            _customItem = new CustomItem(MoreCrossbows.Instance.assetBundle, AssetPath, true, config);
            ItemManager.Instance.AddItem(_customItem);
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Jotunn.Logger.LogInfo("Unloading item " + Name);
            ItemManager.Instance.RemoveItem(Name);
            ObjectDB.instance.Remove(Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
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
            Jotunn.Logger.LogInfo("Loading recipe for " + Name);
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
            Jotunn.Logger.LogInfo("Unloading recipe for " + Name);
            ItemManager.Instance.RemoveRecipe("CraftEarly" + Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
        }
    }
}
