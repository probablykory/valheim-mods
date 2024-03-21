using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using Common;
using static UnityEngine.EventSystems.EventTrigger;
using static Turret;

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
        public FeatureType Type { get; set; }

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

        public virtual string FetchTableEntryValue()
        {
            var table = Entries?.Table?.Value;
            if (string.Equals(table, "Custom") && Entries?.TableCustomName != null)
            {
                table = Entries.TableCustomName.Value;
            }
            else
            {
                table = Table;
            }
            return table;
        }
    }


    internal class FeatureItem : Feature
    {
        public new ItemEntries Entries { get; protected set; }

        public FeatureItem(string name) : base(name) { }
        public string AoePrefabName { get; set; }
        public string AssetPath { get; set; }
        public string Damages { get; set; } = string.Empty;
        public int Knockback { get; set; } = 0;
        public AmmunitionType AmmoType { get; set; } = AmmunitionType.None;

        private CustomItem _customItem = null;

        private void OnEntrySettingChanged(object sender, EventArgs e)
        {
            Get.Plugin.LogDebugOnly("OnEntrySettingChanged fired on feature " + Name);
            RequiresUpdate = true;
        }

        private void OnEnabledSettingChanged(object sender, EventArgs e)
        {
            Get.Plugin.LogDebugOnly("OnEnabledSettingChanged fired on feature " + Name);
            Entries.SetVisibility(EnabledConfigEntry.Value);
            RequiresUpdate = true;
            SharedDrawers.ReloadConfigDisplay();
        }

        public override string FetchTableEntryValue()
        {
            var table = Entries?.Table?.Value;
            if (string.Equals(table, "Custom") && Entries?.TableCustomName != null)
            {
                table = Entries.TableCustomName.Value;
            }
            else
            {
                table = Table;
            }
            return table;
        }

        public override bool Initialize()
        {
            if (!string.IsNullOrEmpty(Category) && !string.IsNullOrEmpty(Description))
            {
                EnabledConfigEntry = MoreCrossbows.Instance.Config(Category, "Enable" + Name, EnabledByDefault, Description);
                EnabledConfigEntry.SettingChanged += OnEnabledSettingChanged;
                Entries = (ItemEntries) ItemEntries.GetFromFeature(MoreCrossbows.Instance, this, EnabledConfigEntry.Value);
                Entries.AddSettingsChangedHandler(OnEntrySettingChanged);
            }
            return true;
        }

        public override bool Update()
        {
            RequiresUpdate = false;
            
            RecipeConfig newRecipe = new RecipeConfig()
            {
                Item = Entries.Name, // Do NOT implicity use RecipeConfig.Name
                CraftingStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                RepairStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries?.Requirements?.Value ?? Requirements)
            };

            // Directly update the recipe
            Get.Plugin.LogDebugOnly("Updating recipe for " + _customItem?.ItemDrop?.name);
            Get.Plugin.LogDebugOnly("... table: " + CraftingStations.GetInternalName(FetchTableEntryValue()) + " level " + (Entries != null ? Entries.MinTableLevel.Value : MinTableLevel));
            Get.Plugin.LogDebugOnly("... reqs: " + (Entries != null ? Entries.Requirements.Value : Requirements));
            _customItem.Recipe.Update(newRecipe);

            Get.Plugin.LogDebugOnly("Overwriting damages of " + Name + " with : " + Entries.Damages.Value);
            setDamage(DamagesDict.Deserialize(Entries.Damages.Value), AoePrefabName);

            return true;
        }

        public override bool Load()
        {
            Get.Plugin.LogDebugOnly("Loading item " + Name);

            ItemConfig config = new ItemConfig()
            {
                CraftingStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                RepairStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries?.Requirements?.Value ?? Requirements)
            };
            _customItem = new CustomItem(MoreCrossbows.Instance.assetBundle, AssetPath, true, config);

            //Get.Plugin.LogDebugOnly("Overwriting damages of " + Name + " with : " + Entries.Damages.Value);
            setDamage(DamagesDict.Deserialize(Entries != null ? Entries.Damages.Value : Damages), AoePrefabName);
            _customItem.ItemDrop.m_itemData.m_shared.m_attackForce = Knockback;
            if (AmmoType == AmmunitionType.Arrow)
            {
                _customItem.ItemDrop.m_itemData.m_shared.m_ammoType = "$ammo_arrows";
            } else if (AmmoType == AmmunitionType.Bolt)
            {
                _customItem.ItemDrop.m_itemData.m_shared.m_ammoType = "$ammo_bolts";
            }
            //_customItem.ItemDrop.m_itemData.m_shared.m_maxQuality

            ItemManager.Instance.AddItem(_customItem);
            RequiresUpdate = false;
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Get.Plugin.LogDebugOnly("Unloading item " + Name);
            ItemManager.Instance.RemoveItem(Name);
            ObjectDB.instance.Remove(Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
        }

        private void setDamage(Dictionary<string, int> dmgs, string aoePrefabName)
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

            if (!string.IsNullOrEmpty(aoePrefabName))
            {
                var prefab = PrefabManager.Instance.GetPrefab(aoePrefabName);
                if (prefab != null)
                {
                    Get.Plugin.LogDebugOnly("seting aoe dmg of " + _customItem.ItemDrop.name);
                    var aoe = prefab.GetComponent<Aoe>();
                    if (aoe != null)
                    {
                        // Aoe frost special case as it scales poorly, 2/3 value goes aoe, 1/3 goes to projectile.
                        float frostThird = _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_frost / 3f;
                        aoe.m_damage.m_frost = frostThird * 2f;
                        _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_frost = frostThird;

                        aoe.m_damage.m_fire = _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_fire;
                        aoe.m_damage.m_lightning = _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_lightning;
                        aoe.m_damage.m_poison = _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_poison;
                        aoe.m_damage.m_spirit = _customItem.ItemDrop.m_itemData.m_shared.m_damages.m_spirit;
                    }
                }
            }
        }
    }


    internal class FeatureRecipe : Feature
    {
        public FeatureRecipe(string name) : base(name) { }
        private void OnEntrySettingChanged(object sender, EventArgs e)
        {
            Get.Plugin.LogDebugOnly("OnEntrySettingChanged fired on feature " + Name);
            RequiresUpdate = true;
        }

        private void OnEnabledSettingChanged(object sender, EventArgs e)
        {
            Get.Plugin.LogDebugOnly("OnEnabledSettingChanged fired on feature " + Name);
            Entries.SetVisibility(EnabledConfigEntry.Value);
            RequiresUpdate = true;
            SharedDrawers.ReloadConfigDisplay();
        }

        public override bool Initialize()
        {
            EnabledConfigEntry = MoreCrossbows.Instance.Config(Category, "Enable" + Name, EnabledByDefault, Description);
            EnabledConfigEntry.SettingChanged += OnEnabledSettingChanged;
            Entries = Entries.GetFromFeature(MoreCrossbows.Instance, this, null, EnabledConfigEntry.Value);
            Entries.AddSettingsChangedHandler(this.OnEntrySettingChanged);
            return true;
        }

        public override bool Update()
        {
            RequiresUpdate = false;

            RecipeConfig newRecipe = new RecipeConfig()
            {
                Item = Entries.Name, // Do NOT implicity use RecipeConfig.Name
                CraftingStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                RepairStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                MinStationLevel = Entries != null ? Entries.MinTableLevel.Value : MinTableLevel,
                Amount = Entries != null ? Entries.Amount.Value : Amount,
                Requirements = RequirementsEntry.Deserialize(Entries?.Requirements?.Value ?? Requirements)
            };

            var recipe = ItemManager.Instance.GetRecipe("CraftEarly" + Name);

            Get.Plugin.LogDebugOnly("Updating recipe " + Entries.Name);
            Get.Plugin.LogDebugOnly("... reqs: " + Entries != null ? Entries.Requirements.Value : Requirements);
            recipe.Update(newRecipe);


            RequiresUpdate = false;

            return true;
        }


        public override bool Load()
        {
            Get.Plugin.LogDebugOnly("Loading recipe for " + Name);
            RecipeConfig config = new RecipeConfig()
            {
                Name = "CraftEarly" + Entries.Name,
                Item = Entries.Name,
                CraftingStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                RepairStation = CraftingStations.GetInternalName(FetchTableEntryValue()),
                MinStationLevel = Entries.MinTableLevel.Value,
                Amount = Entries.Amount.Value,
                Requirements = RequirementsEntry.Deserialize(Entries.Requirements.Value)
            };
            ItemManager.Instance.AddRecipe(new CustomRecipe(config));
            RequiresUpdate = false;
            LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            Get.Plugin.LogDebugOnly("Unloading recipe for " + Name);
            ItemManager.Instance.RemoveRecipe("CraftEarly" + Name);
            LoadedInGame = false;
            RequiresUpdate = false;

            return true;
        }
    }
}
