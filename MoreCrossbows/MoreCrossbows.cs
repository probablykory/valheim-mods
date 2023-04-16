using BepInEx;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;

namespace MoreCrossbows
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class MoreCrossbows : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "MoreCrossbows";
        public const string PluginVersion = "1.2.0.0";
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
            AddDefaultLocalizations();

            PlayerPatches.OnPlayerSpawned += OnPlayerSpawned;

            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            LocalizationManager.OnLocalizationAdded += OnLocalizationAdded;
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
            PlayerPatches.OnPlayerSpawned -= OnPlayerSpawned;

            Config.Save();
        }

        // force references to fix for indirect dependencies
        private void RegisterCustomPrefab(AssetBundle bundle, string assetName)
        {
            string prefabName = assetName.Replace(".prefab", "");
            if (!String.IsNullOrEmpty(prefabName) && !PrefabManager.Instance.PrefabExists(prefabName))
            {
                var prefab = new CustomPrefab(bundle, assetName, true);
                Jotunn.Logger.LogDebug("Registering " + prefab.Prefab.name);
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
            int loadCount = 0;
            int unloadCount = 0;

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
                        loadCount++;
                    }
                    else
                    {
                        f.Unload();
                        unloadCount++;
                    }
                }
            }
            Jotunn.Logger.LogInfo($"{loadCount} features loaded" + (unloadCount > 0 ? $", and {unloadCount} features loaded, " : ""));
        }

        private static Dictionary<string, string> DefaultEnglishLanguageStrings = new Dictionary<string, string>() {
            {"$area_of_effect", "Area effect" },
            {"$every", "every" }, {"$seconds", "seconds" }, {"$per_second", "per second" },

            {"$item_crossbow_wood", "Wooden crossbow"}, {"$item_crossbow_wood_description", "A crudely-made but powerful weapon."},
            {"$item_crossbow_bronze", "Bronze crossbow"}, {"$item_crossbow_bronze_description", "A powerful weapon, forged in bronze."},
            {"$item_crossbow_iron", "Iron crossbow"}, {"$item_crossbow_iron_description", "An accurate, powerful messenger of death."},
            {"$item_crossbow_silver", "Silver crossbow"}, {"$item_crossbow_silver_description", "A sleek weapon, crafted from the mountain top."},
            {"$item_crossbow_blackmetal", "Blackmetal crossbow"}, {"$item_crossbow_blackmetal_description", "A vicious thing.  Handle with care."},

            {"$item_bolt_wood", "Wood bolt"}, {"$item_bolt_wood_description", "A brittle crossbow bolt of sharpened wood."},
            {"$item_bolt_fire", "Fire bolt"}, {"$item_bolt_fire_description", "A piercing bolt of fire."},
            {"$item_bolt_silver", "Silver bolt"}, {"$item_bolt_silver_description", "A bolt to calm restless spirits."},
            {"$item_bolt_poison", "Poison bolt"}, {"$item_bolt_poison_description", "A bitter dose for your enemies."},
            {"$item_bolt_frost", "Frost bolt"}, {"$item_bolt_frost_description", "A piercing bolt of ice."},

            {"$item_bolt_lightning", "Lightning bolt"}, {"$item_bolt_lightning_description", "Noone can know when or where it will strike."},
            {"$item_arrow_lightning", "Lightning arrow"}, {"$item_arrow_lightning_description", "Noone can know when or where it will strike."},

            {"$item_bolt_surtling", "Surtling bolt"}, {"$item_bolt_surtling_description", "Do not use indoors."},
            {"$item_bolt_ooze", "Ooze bolt"}, {"$item_bolt_ooze_description", "The stench is unbearable..."},
            {"$item_bolt_bile", "Bile bolt"}, {"$item_bolt_bile_description", "Handle with care."},
            {"$item_bolt_ice", "Ice bolt"}, {"$item_bolt_ice_description", "Heart of the frozen mountain."},

            {"$item_bolt_flametal", "Flametal bolt"}, {"$item_bolt_flametal_description", "Do not use indoors."},
        };

        private void AddDefaultLocalizations()
        {
            Jotunn.Logger.LogDebug("AddLocalizations called.");
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", DefaultEnglishLanguageStrings);
        }

        private void OnLocalizationAdded()
        {
            Jotunn.Logger.LogDebug("Localization Added received.");

            string pluginPath = Instance.GetType().Assembly.Location.Replace(Path.DirectorySeparatorChar + PluginName + ".dll", "");
            string pluginFolder = pluginPath;
            if (BepInEx.Paths.PluginPath.Equals(pluginFolder)) 
            {
                // If our parent folder and bepinex's plugin folder are the same, use a subdir instead.
                pluginFolder = Utility.CombinePaths(new string[] { BepInEx.Paths.PluginPath, PluginName });
            }

            string locFile = Utility.CombinePaths(new string[] { pluginFolder, "Translations", "English", "localization" + PluginVersion + ".json" });
            string locPath = Path.GetDirectoryName(locFile);

            if (!(Directory.Exists(locPath) && File.Exists(locFile)))
            {
                // Write defaults out to disk for end-users
                Directory.CreateDirectory(locPath);
                string fileContent = SimpleJson.SimpleJson.SerializeObject(DefaultEnglishLanguageStrings);
                File.WriteAllText(locFile, fileContent);

                Jotunn.Logger.LogInfo("Default localizations written to " + locFile);
            }

            LocalizationManager.OnLocalizationAdded -= OnLocalizationAdded;
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
                Damages = "Pierce:27",
                Knockback = 80
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
                Damages = "Pierce:42",
                Knockback = 100

            });

            _features.Add(new FeatureItem("CrossbowIron")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Iron Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowIron.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 2,
                Requirements = "Wood:10:5,ElderBark:4:2,Iron:20:10,Root:1",
                Damages = "Pierce:57",
                Knockback = 120
            });

            _features.Add(new FeatureItem("CrossbowSilver")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Silver Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowSilver.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 3,
                Requirements = "Wood:10:4,Silver:10:5,Iron:10:5,WolfHairBundle:6",
                Damages = "Pierce:72",
                Knockback = 140
            });

            _features.Add(new FeatureItem("CrossbowBlackmetal")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Blackmetal Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowBlackmetal.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 4,
                Requirements = "FineWood:10:5,BlackMetal:16:8,Iron:8:4,LoxPelt:2:1",
                Damages = "Pierce:92",
                Knockback = 160
            });

            _features.Add(new FeatureItem("BoltWood")
            {
                Category = "3 - Bolts",
                Description = "Adds new wood bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltWood.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:8",
                Damages = "Pierce:22",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltFire")
            {
                Category = "3 - Bolts",
                Description = "Adds new fire bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltFire.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:8,Resin:8,Feathers:2",
                Damages = "Pierce:11,Fire:22",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_fire.prefab"
                }
            });


            _features.Add(new FeatureItem("BoltSilver")
            {
                Category = "3 - Bolts",
                Description = "Adds new silver bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltSilver.prefab",

                Amount = 20,
                Table = CraftingTable.Forge,
                MinTableLevel = 3,
                Requirements = "Wood:8,Silver:1,Feathers:2",
                Damages = "Pierce:52,Spirit:20",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltPoison")
            {
                Category = "3 - Bolts",
                Description = "Adds new poison bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltPoison.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 3,
                Requirements = "Wood:8,Obsidian:4,Feathers:2,Ooze:2",
                Damages = "Pierce:26,Poison:52",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_poison.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltFrost")
            {
                Category = "3 - Bolts",
                Description = "Adds new frost bolts",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/BoltFrost.prefab",

                Amount = 20,
                Table = CraftingTable.Workbench,
                MinTableLevel = 4,
                Requirements = "Wood:8,Obsidian:4,Feathers:2,FreezeGland:1",
                Damages = "Pierce:26,Frost:52",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_frost.prefab"
                }
            });


            _features.Add(new FeatureItem("BoltLightning")
            {
                Category = "3 - Bolts",
                Description = "Adds new lightning bolts",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltLightning.prefab",

                Amount = 20,
                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Feathers:2,Eitr:1",
                Damages = "Pierce:36,Lightning:62",
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit.prefab",
                    "arbalest_projectile_lightning.prefab"
                }
            });

            _features.Add(new FeatureItem("ArrowLightning")
            {
                Category = "2 - Arrows",
                Description = "Adds new lightning arrows",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/ArrowLightning.prefab",

                Amount = 20,
                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Feathers:2,Eitr:1",
                Damages = "Pierce:36,Lightning:62",
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit.prefab",
                    "arbalest_projectile_lightning.prefab"
                }
            });


            // New AOE Bolts
            _features.Add(new FeatureItem("BoltOoze")
            {
                Category = "4 - Area Effect Bolts",
                Description = "Adds new Ooze bomb bolts.  These cause the same Ooze explosions as bombs. Damage set here is applied to both projectile & explosion.",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltOoze.prefab",

                Amount = 10,
                Table = CraftingTable.Workbench,
                MinTableLevel = 1,
                Requirements = "Wood:4,Feathers:1,LeatherScraps:5,Ooze:5",
                Damages = "Pierce:5,Poison:40",
                AoePrefabName = "oozebomb_explosion",

                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_ooze.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltSurtling")
            {
                Category = "4 - Area Effect Bolts",
                Description = "Adds new Surtling bolts.  Damage set here is applied to both projectile & explosion.",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltSurtling.prefab",

                Amount = 10,
                Table = CraftingTable.Forge,
                MinTableLevel = 4,
                Requirements = "Wood:4,Feathers:1,SurtlingCore:3,Iron:1",
                Damages = "Pierce:22,Fire:30",
                AoePrefabName = "firebolt_explosion",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_surtling.prefab",
                    "firebolt_explosion.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltBile")
            {
                Category = "4 - Area Effect Bolts",
                Description = "Adds new Bile bomb bolts.  These cause the same Bile explosions as bombs. Damage set here is applied to both projectile & explosion.",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltBile.prefab",

                Amount = 10,
                Table = CraftingTable.Workbench,
                MinTableLevel = 1,
                Requirements = "Wood:4,Feathers:1,Sap:3,Bilebag:3",
                Damages = "Pierce:22,Fire:15,Poison:30",
                AoePrefabName = "bilebomb_explosion",

                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_bile.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltIce")
            {
                Category = "4 - Area Effect Bolts",
                Description = "Adds new Ice bolts which strike an area with frost damage.  Damage set here is SPLIT between projectile & explosion.",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltIce.prefab",

                Amount = 10,
                Table = CraftingTable.Forge,
                MinTableLevel = 4,
                Requirements = "Wood:4,Feathers:1,FreezeGland:3,BlackMetal:1",
                Damages = "Pierce:22,Frost:62",
                AoePrefabName = "icebolt_explosion",

                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_ice.prefab",
                    "icebolt_explosion.prefab"
                }
            });

            _features.Add(new FeatureItem("BoltFlametal")
            {
                Category = "4 - Area Effect Bolts",
                Description = "Adds new Flametal bolts.  Hits all targets very hard.  Damage set here is applied ONLY to projectile.",
                EnabledByDefault = false,
                AssetPath = "Assets/PrefabInstance/BoltFlametal.prefab",

                Amount = 10,
                Table = CraftingTable.BlackForge,
                MinTableLevel = 2,
                Requirements = "Flametal:1,Feathers:1,Eitr:3,SurtlingCore:3",
                Damages = "Blunt:22,Pierce:22,Fire:102",
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_flametal.prefab",
                    "firebolt_explosion.prefab"
                }

            });


            // new workbench recipes for existing bolts
            _features.Add(new FeatureRecipe("BoltBone")
            {
                Category = "3 - Bolts",
                Description = "Enables bone bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "BoneFragments:8,Feathers:2",
                Amount = 20,
            });

            _features.Add(new FeatureRecipe("BoltIron")
            {
                Category = "3 - Bolts",
                Description = "Enables iron bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Forge,
                MinTableLevel = 2,
                Requirements = "Wood:8,Iron:1,Feathers:2",
                Amount = 20,
            });

            _features.Add(new FeatureRecipe("BoltBlackmetal")
            {
                Category = "3 - Bolts",
                Description = "Enables blackmetal bolts to be craftable earlier",
                EnabledByDefault = true,

                Table = CraftingTable.Forge,
                MinTableLevel = 4,
                Requirements = "Wood:8,BlackMetal:2,Feathers:2",
                Amount = 20,
            });


            // Must be last - easter egg
            //_features.Add(new FeatureItem("BoltExplosive")
            //{
            //    AssetPath = "Assets/PrefabInstance/BoltExplosive.prefab",
            //    // no enabled setting
            //    //Category = "3 - Bolts",
            //    //Description = "",
            //    //EnabledByDefault = false,

            //    Table = CraftingTable.BlackForge,
            //    MinTableLevel = 2,
            //    Requirements = "Wood:5,Feathers:2,Ooze:5,SurtlingCore:5,Coal:5,Eitr:5,Flametal:1",
            //    Damages = "Blunt:5,Pierce:5,Fire:120",
            //    Amount = 10,
            //});


            foreach (var f in _features)
            {
                f.Initialize();
            }
        }
    }
}

