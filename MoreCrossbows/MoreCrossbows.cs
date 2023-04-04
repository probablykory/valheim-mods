using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace MoreCrossbows
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class MoreCrossbows : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "MoreCrossbows";
        public const string PluginVersion = "1.1.1.0";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static MoreCrossbows Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("crossbows");
        private Harmony harmony = null;
        private bool _vanillaPrefabsAvailable = false;
        private List<Feature> _features = new List<Feature>();

        private void Awake()
        {
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            MoreCrossbows.Instance = this;
            Config.SaveOnConfigSet = true;

            InitializeFeatures();
            AddLocalizations();

            Patches.OnPlayerSpawned += OnPlayerSpawned;


            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        }

        private void OnPlayerSpawned(Player obj)
        {
            Jotunn.Logger.LogDebug("Player spawned received.");
            EnsureFeaturesUpdates();
        }

        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            Jotunn.Logger.LogDebug("Configuration Sync received.");

            if (_vanillaPrefabsAvailable)
            {
                AddOrRemoveFeatures();
            }
        }

        private void OnVanillaPrefabsAvailable()
        {
            Jotunn.Logger.LogDebug("Vanilla Prefabs Available received.");
            _vanillaPrefabsAvailable = true;
            AddOrRemoveFeatures();
        }

        private void OnDestroy()
        {
            Jotunn.Logger.LogDebug("OnDestroy called.");

            SynchronizationManager.OnConfigurationSynchronized -= OnConfigurationSynchronized;
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
            Patches.OnPlayerSpawned -= OnPlayerSpawned;

            Config.Save();
        }

        // force references to fix for indirect dependencies
        private void RegisterCustomPrefab(AssetBundle bundle, string assetName)
        {
            string prefabName = assetName.Replace(".prefab", "");
            if (!String.IsNullOrEmpty(prefabName) && !PrefabManager.Instance.PrefabExists(prefabName))
            {
                var prefab = new CustomPrefab(bundle, assetName, true);
                Jotunn.Logger.LogInfo("Registering " + prefab.Prefab.name);
                if (prefab != null && prefab.IsValid())
                {
                    prefab.Prefab.FixReferences(true);
                    PrefabManager.Instance.AddPrefab(prefab);
                }
            }
        }

        private void EnsureFeaturesUpdates()
        {
            Jotunn.Logger.LogDebug("Ensuring feature updates.");

            foreach (Feature f in _features)
            {
                if (f.RequiresUpdate)
                {
                    f.Update();
                }
            }
        }

        private void AddOrRemoveFeatures()
        {
            bool areAllFeaturesEnabled = true;

            foreach (Feature f in _features)
            {
                bool isEnabled = false;

                if (f.EnabledConfigEntry == null)
                {
                    isEnabled = areAllFeaturesEnabled;
                }
                else
                {
                    isEnabled = f.EnabledConfigEntry.Value;
                    areAllFeaturesEnabled = areAllFeaturesEnabled && isEnabled;
                }

                //if (_debug)
                //{
                //    Jotunn.Logger.LogDebug("DEBUG: allFeaturesEnabled = " + areAllFeaturesEnabled.ToString());
                //    Jotunn.Logger.LogDebug("DEBUG: Feature " + f.Name + " is " + (isEnabled ? "enabled" : "disabled") + " and " + (f.LoadedInGame ? "Loaded" : "Unloaded"));
                //}

                if (isEnabled != f.LoadedInGame)
                {
                    if (isEnabled)
                    {
                        if (f.DependencyNames.Count > 0)
                        {
                            foreach (string dep in f.DependencyNames)
                            {
                                RegisterCustomPrefab(assetBundle, dep);
                            }
                        }
                        f.Load();
                    }
                    else
                    {
                        f.Unload();
                    }
                }
            }
        }

        private void AddLocalizations()
        {
            CustomLocalization loc = new CustomLocalization();

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                {"item_crossbow_wood", "Wooden Crossbow"}, {"item_crossbow_wood_description", "A crudely-made but powerful weapon."},
                {"item_crossbow_bronze", "Bronze Crossbow"}, {"item_crossbow_bronze_description", "A powerful weapon, forged in bronze."},
                {"item_crossbow_iron", "Iron Crossbow"}, {"item_crossbow_iron_description", "An accurate, powerful messenger of death."},
                {"item_crossbow_silver", "Silver Crossbow"}, {"item_crossbow_silver_description", "A sleek weapon, crafted from the mountain top."},
                {"item_crossbow_blackmetal", "Blackmetal Crossbow"}, {"item_crossbow_blackmetal_description", "A vicious thing.  Handle with care."},

                {"item_bolt_wood", "Wood Bolt"}, {"item_bolt_wood_description", "A brittle crossbow bolt of sharpened wood."},
                {"item_bolt_fire", "Fire Bolt"}, {"item_bolt_fire_description", "A piercing bolt of fire."},
                {"item_bolt_silver", "Silver Bolt"}, {"item_bolt_silver_description", "A bolt to calm restless spirits."},
                {"item_bolt_poison", "Poison Bolt"}, {"item_bolt_poison_description", "A bitter dose for your enemies."},
                {"item_bolt_frost", "Frost Bolt"}, {"item_bolt_frost_description", "A piercing bolt of ice."},

                {"item_bolt_lightning", "Lightning Bolt"}, {"item_bolt_lightning_description", "Noone can know when or where it will strike."},
                {"item_arrow_lightning", "Lightning Arrow"}, {"item_arrow_lightning_description", "Noone can know when or where it will strike."},

                {"item_bolt_explosive", "Explosive Bolt"}, {"item_bolt_explosive_description", "Do not use indoors.  Why should sorcerers have all the fun?"},
            });

            LocalizationManager.Instance.AddLocalization(loc);
        }

        private void InitializeFeatures()
        {

            _features.Add(new FeatureItem("CrossbowWood")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Wooden Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowWood.prefab",

                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:20:5,Stone:8:2,LeatherScraps:8:2",
            });

            _features.Add(new FeatureItem("CrossbowBronze")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Bronze Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowBronze.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:5,FineWood:4:2,Bronze:10:5,DeerHide:2:1",
            });

            _features.Add(new FeatureItem("CrossbowIron")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Iron Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowIron.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:5,ElderBark:4:2,Iron:20:10,Root:1",
            });

            _features.Add(new FeatureItem("CrossbowSilver")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Silver Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowSilver.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:4,Silver:10:5,Iron:10:5,WolfHairBundle:6",
            });

            _features.Add(new FeatureItem("CrossbowBlackmetal")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Blackmetal Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowBlackmetal.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "FineWood:10:5,BlackMetal:16:8,Iron:8:4,LoxPelt:2:1",
            });

            _features.Add(new FeatureItem("BoltWood")
            {
                Category = "2 - Bolts",
                Description = "Adds new wood bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltWood.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:8",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltFire")
            {
                Category = "2 - Bolts",
                Description = "Adds new fire bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltFire.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:8,Resin:8,Feathers:2",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_fire.prefab"
                }
            });


            _features.Add(new FeatureItem("BoltSilver")
            {
                Category = "2 - Bolts",
                Description = "Adds new silver bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltSilver.prefab",

                Amount = 20,
                Table = CraftingTable.Forge,
                MinTableLevel = 3,
                Requirements = "Wood:8,Silver:1,Feathers:2",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltPoison")
            {
                Category = "2 - Bolts",
                Description = "Adds new poison bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltPoison.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 3,
                Requirements = "Wood:8,Obsidian:4,Feathers:2,Ooze:2",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_poison.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltFrost")
            {
                Category = "2 - Bolts",
                Description = "Adds new frost bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltFrost.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 4,
                Requirements = "Wood:8,Obsidian:4,Feathers:2,FreezeGland:1",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_frost.prefab"
                }
            });


            _features.Add(new FeatureItem("BoltLightning")
            {
                Category = "2 - Bolts",
                Description = "Adds new lightning bolts",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltLightning.prefab",

                Amount = 20,
                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Feathers:2,Eitr:1",
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit.prefab",
                    "arbalest_projectile_lightning.prefab"
                }
            });

            _features.Add(new FeatureItem("ArrowLightning")
            {
                Category = "3 - Arrows",
                Description = "Adds new lightning arrows",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/ArrowLightning.prefab",

                Amount = 20,
                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Feathers:2,Eitr:1",
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit.prefab",
                    "arbalest_projectile_lightning.prefab"
                }
            });


            // new workbench recipes for existing bolts
            _features.Add(new FeatureRecipe("BoltBone")
            {
                Category = "2 - Bolts",
                Description = "Enables bone bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "BoneFragments:8,Feathers:2",
                Amount = 20,
            });

            _features.Add(new FeatureRecipe("BoltIron")
            {
                Category = "2 - Bolts",
                Description = "Enables iron bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Forge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Iron:1,Feathers:2",
                Amount = 20,
            });

            _features.Add(new FeatureRecipe("BoltBlackmetal")
            {
                Category = "2 - Bolts",
                Description = "Enables blackmetal bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Forge,
                MinTableLevel = 4,
                Requirements = "Wood:8,BlackMetal:2,Feathers:2",
                Amount = 20,
            });

            // Must be last - easter egg
            _features.Add(new FeatureItem("BoltExplosive")
            {
                AssetPath = "Assets/PrefabInstance/BoltExplosive.prefab",
                // no enabled setting
                //Category = "2 - Bolts",
                //Description = "",
                //EnabledByDefault = false,

                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Wood:5,Feathers:2,Ooze:5,SurtlingCore:5,Coal:5,Eitr:5,Flametal:1",
                Amount = 10,
            });

            foreach (var f in _features)
            {
                f.Initialize();
            }
        }
    }
}

