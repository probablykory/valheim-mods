using BepInEx;
using BepInEx.Configuration;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;


namespace MoreCrossbows
{
    public enum CraftingTable
    {
        Disabled,
        Inventory,
        [InternalName("piece_workbench")] Workbench,
        [InternalName("piece_cauldron")] Cauldron,
        [InternalName("forge")] Forge,
        [InternalName("piece_artisanstation")] ArtisanTable,
        [InternalName("piece_stonecutter")] StoneCutter,
        [InternalName("piece_magetable")] MageTable,
        [InternalName("blackforge")] BlackForge,
        Custom
    }

    public class InternalName : Attribute
    {
        public static string GetName<T>(T value) where T : struct => ((InternalName)typeof(T).GetMember(value.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName;


        public readonly string internalName;
        public InternalName(string internalName) => this.internalName = internalName;
    }

    public static class EnumExtensions
    {
        public static string InternalName(this CraftingTable value)
        {
            switch (value)
            {
                case CraftingTable.Workbench:
                case CraftingTable.Cauldron:
                case CraftingTable.Forge:
                case CraftingTable.ArtisanTable:
                case CraftingTable.StoneCutter:
                case CraftingTable.MageTable:
                case CraftingTable.BlackForge:
                    return ((InternalName)typeof(CraftingTable).GetMember(value.ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName;
            }
            return string.Empty;
        }
    }


    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class MoreCrossbows : BaseUnityPlugin
    {

#if DEBUG
        private bool _debug = true;
#else
        private bool _debug = false;
#endif

        public const string PluginAuthor = "probablykory";
        public const string PluginName = "MoreCrossbows";
        public const string PluginVersion = "1.0.0.0";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        public static string ConfigFileName
        {
            get
            {
                return PluginAuthor + "." + PluginName + ".cfg";
            }
        }

        internal AssetBundle _bundle = AssetUtils.LoadAssetBundleFromResources("crossbows");
        private bool _vanillaPrefabsAvailable = false;
        private List<Feature> _features = new List<Feature>();

        // Main entry point
        private void Awake()
        {
            base.Config.SaveOnConfigSet = true;

            var ct = CraftingTable.Workbench.InternalName();

#if DEBUG
            InitializeConfigWatcher();
#endif
            InitializeFeatures();
            this.AddLocalizations();

            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable; // handled only once
        }

        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            Jotunn.Logger.LogInfo("Configuration Sync recieved.");

            if (_debug)
            {
                Jotunn.Logger.LogInfo("DEBUG: config sync");
                Jotunn.Logger.LogInfo("DEBUG: " + sender + " " + e.ToString());
            }

            if (_vanillaPrefabsAvailable)
            {
                AddOrRemoveFeatures();
            }
        }

        private void OnVanillaPrefabsAvailable()
        {
            _vanillaPrefabsAvailable = true;
            AddOrRemoveFeatures();

            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private void OnDestroy()
        {
            SynchronizationManager.OnConfigurationSynchronized -= OnConfigurationSynchronized;

            base.Config.Save();
        }

        private bool PrefabExists(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            var result = PrefabManager.Instance.GetPrefab(name);
            return result != null;
        }

        // force references to fix for indirect dependencies
        private void RegisterCustomPrefab(AssetBundle bundle, string prefabName)
        {
            if (!String.IsNullOrEmpty(prefabName) && !PrefabExists(prefabName))
            {
                var prefab = new CustomPrefab(bundle, prefabName, true);
                Jotunn.Logger.LogInfo("Registering " + prefab.Prefab.name);
                if (prefab != null && prefab.IsValid())
                {
                    prefab.Prefab.FixReferences(true);
                    PrefabManager.Instance.AddPrefab(prefab.Prefab);
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

                if (_debug)
                {
                    Jotunn.Logger.LogInfo("DEBUG: allFeaturesEnabled = " + areAllFeaturesEnabled.ToString());
                    Jotunn.Logger.LogInfo("DEBUG: Feature " + f.Name + " is " + (isEnabled ? "enabled" : "disabled") + " and " + (f.LoadedInGame ? "Loaded" : "Unloaded"));
                }

                if (isEnabled != f.LoadedInGame)
                {
                    if (isEnabled)
                    {
                        if (f.DependencyNames.Count > 0)
                        {
                            foreach (string dep in f.DependencyNames)
                            {
                                RegisterCustomPrefab(_bundle, dep);
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

#if DEBUG
        private void InitializeConfigWatcher()
        {
            FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath, ConfigFileName);
            fileSystemWatcher.Changed += this.onConfigFileChangedCreatedOrRenamed;
            fileSystemWatcher.Created += this.onConfigFileChangedCreatedOrRenamed;
            fileSystemWatcher.Renamed += new RenamedEventHandler(this.onConfigFileChangedCreatedOrRenamed);
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void onConfigFileChangedCreatedOrRenamed(object sender, FileSystemEventArgs e)
        {
            string path = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar.ToString() + ConfigFileName;
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                Jotunn.Logger.LogInfo("Reading configuration.");
                base.Config.Reload();
            }
            catch
            {
                Jotunn.Logger.LogError("There was an issue with your " + ConfigFileName + " file.");
                Jotunn.Logger.LogError("Please check the format and spelling.");
            }

            if (_vanillaPrefabsAvailable)
            {
                AddOrRemoveFeatures();
            }
        }
#endif

        private void InitializeFeatures()
        {
            ConfigurationManagerAttributes isAdminOnly = new ConfigurationManagerAttributes { IsAdminOnly = true };

            var enableBoltBone = Config.Bind("Bolts", "EnableBoltBone", true, new ConfigDescription("Enables bone bolts to be craftable earlier", null, isAdminOnly));
            var enableBoltIron = Config.Bind("Bolts", "EnableBoltIron", true, new ConfigDescription("Enables iron bolts to be craftable earlier", null, isAdminOnly));
            var enableBoltBlackmetal = Config.Bind("Bolts", "EnableBoltBlackmetal", true, new ConfigDescription("Enables blackmetal bolts to be craftable earlier", null, isAdminOnly));
            var enableBoltWood = Config.Bind("Bolts", "EnableBoltWood", true, new ConfigDescription("Adds new wood bolts", null, isAdminOnly));
            var enableBoltFire = Config.Bind("Bolts", "EnableBoltFire", true, new ConfigDescription("Adds new fire bolts", null, isAdminOnly));
            var enableBoltSilver = Config.Bind("Bolts", "EnableBoltSilver", true, new ConfigDescription("Adds new spirit bolts", null, isAdminOnly));
            var enableBoltFrost = Config.Bind("Bolts", "EnableBoltFrost", true, new ConfigDescription("Adds new frost bolts", null, isAdminOnly));
            var enableBoltPoison = Config.Bind("Bolts", "EnableBoltPoison", true, new ConfigDescription("Adds new poison bolts", null, isAdminOnly));

            var enableBoltLightning = Config.Bind("Bolts", "EnableBoltLightning", false, new ConfigDescription("Adds new lightning bolts", null, isAdminOnly));
            var enableArrowLightning = Config.Bind("Arrows", "EnableArrowLightning", false, new ConfigDescription("Adds new lightning arrows", null, isAdminOnly));

            var enableCrossbowWood = Config.Bind("Crossbows", "EnableCrossbowWood", true, new ConfigDescription("Adds a new Wooden Crossbow weapon", null, isAdminOnly));
            var enableCrossbowBronze = Config.Bind("Crossbows", "EnableCrossbowBronze", true, new ConfigDescription("Adds a new Bronze Crossbow weapon", null, isAdminOnly));
            var enableCrossbowIron = Config.Bind("Crossbows", "EnableCrossbowIron", true, new ConfigDescription("Adds a new Iron Crossbow weapon", null, isAdminOnly));
            var enableCrossbowSilver = Config.Bind("Crossbows", "EnableCrossbowSilver", true, new ConfigDescription("Adds a new Silver Crossbow weapon", null, isAdminOnly));
            var enableCrossbowBlackmetal = Config.Bind("Crossbows", "EnableCrossbowBlackmetal", true, new ConfigDescription("Adds a new Blackmetal Crossbow weapon", null, isAdminOnly));


            _features.Add(new FeatureItem(this)
            {
                Name = "CrossbowWood",
                AssetPath = "Assets/PrefabInstance/CrossbowWood.prefab",
                EnabledConfigEntry = enableCrossbowWood,
                ItemConfig = new ItemConfig()
                {
                    CraftingStation = CraftingTable.Workbench.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 20, 8),
                        new RequirementConfig("Stone", 8, 2),
                        new RequirementConfig("LeatherScraps", 8, 2),
                    }
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "CrossbowBronze",
                AssetPath = "Assets/PrefabInstance/CrossbowBronze.prefab",
                EnabledConfigEntry = enableCrossbowBronze,
                ItemConfig = new ItemConfig()
                {
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 10, 4),
                        new RequirementConfig("FineWood", 4, 2),
                        new RequirementConfig("Bronze", 8, 2),
                        new RequirementConfig("DeerHide", 2, 1),
                    }
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "CrossbowIron",
                AssetPath = "Assets/PrefabInstance/CrossbowIron.prefab",
                EnabledConfigEntry = enableCrossbowIron,
                ItemConfig = new ItemConfig()
                {
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 10, 4),
                        new RequirementConfig("ElderBark", 4, 2),
                        new RequirementConfig("Iron", 8, 2),
                        new RequirementConfig("Root", 1, 0),
                    }
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "CrossbowSilver",
                AssetPath = "Assets/PrefabInstance/CrossbowSilver.prefab",
                EnabledConfigEntry = enableCrossbowSilver,
                ItemConfig = new ItemConfig()
                {
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 10, 5),
                        new RequirementConfig("Silver", 4, 2),
                        new RequirementConfig("Iron", 8, 4),
                        new RequirementConfig("WolfHairBundle", 6, 0),
                    }
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "CrossbowBlackmetal",
                AssetPath = "Assets/PrefabInstance/CrossbowBlackmetal.prefab",
                EnabledConfigEntry = enableCrossbowBlackmetal,
                ItemConfig = new ItemConfig()
                {
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("FineWood", 10, 5),
                        new RequirementConfig("BlackMetal", 10, 5),
                        new RequirementConfig("Iron", 8, 4),
                        new RequirementConfig("LoxPelt", 2, 1)
                    }
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltWood",
                AssetPath = "Assets/PrefabInstance/BoltWood.prefab",
                EnabledConfigEntry = enableBoltWood,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.Workbench.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8)
                    }
                },
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltFire",
                AssetPath = "Assets/PrefabInstance/BoltFire.prefab",
                EnabledConfigEntry = enableBoltFire,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.Workbench.InternalName(),

                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Resin", 8),
                        new RequirementConfig("Feathers", 2)
                    }
                },
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_fire.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltSilver",
                AssetPath = "Assets/PrefabInstance/BoltSilver.prefab",
                EnabledConfigEntry = enableBoltSilver,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    MinStationLevel = 3,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Silver", 1),
                        new RequirementConfig("Feathers", 2)
                    }
                },
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_wood.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltFrost",
                AssetPath = "Assets/PrefabInstance/BoltFrost.prefab",
                EnabledConfigEntry = enableBoltFrost,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.Workbench.InternalName(),
                    MinStationLevel = 4,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Obsidian", 4),
                        new RequirementConfig("Feathers", 2),
                        new RequirementConfig("FreezeGland", 1)
                    }
                },
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_frost.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltPoison",
                AssetPath = "Assets/PrefabInstance/BoltPoison.prefab",
                EnabledConfigEntry = enableBoltPoison,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.Workbench.InternalName(),
                    MinStationLevel = 3,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Obsidian", 4),
                        new RequirementConfig("Feathers", 2),
                        new RequirementConfig("Ooze", 2)
                    }
                },
                DependencyNames = new List<string>()
                {
                    "arbalest_projectile_poison.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "BoltLightning",
                AssetPath = "Assets/PrefabInstance/BoltLightning.prefab",
                EnabledConfigEntry = enableBoltLightning,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.BlackForge.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Feathers", 2),
                        new RequirementConfig("Eitr", 1)
                    }

                },
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit",
                    "arbalest_projectile_lightning.prefab"
                }
            });

            _features.Add(new FeatureItem(this)
            {
                Name = "ArrowLightning",
                AssetPath = "Assets/PrefabInstance/ArrowLightning.prefab",
                EnabledConfigEntry = enableArrowLightning,
                ItemConfig = new ItemConfig()
                {
                    Amount = 20,
                    CraftingStation = CraftingTable.BlackForge.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Feathers", 2),
                        new RequirementConfig("Eitr", 1)
                    }

                },
                DependencyNames = new List<string>()
                {
                    "sfx_lightning_hit",
                    "bow_projectile_lightning.prefab"
                }
            });

            // new workbench recipes for existing bolts
            _features.Add(new FeatureRecipe(this)
            {
                Name = "BoltBone",
                EnabledConfigEntry = enableBoltBone,
                RecipeConfig = new RecipeConfig()
                {
                    Name = "CraftEarlyBoltBone",
                    Item = "BoltBone",
                    Amount = 20,
                    CraftingStation = CraftingTable.Workbench.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("BoneFragments", 8),
                        new RequirementConfig("Feathers", 2)
                    }
                }
            });

            _features.Add(new FeatureRecipe(this)
            {
                Name = "BoltIron",
                EnabledConfigEntry = enableBoltIron,
                RecipeConfig = new RecipeConfig()
                {
                    Name = "CraftEarlyBoltIron",
                    Item = "BoltIron",
                    Amount = 20,
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("Iron", 1),
                        new RequirementConfig("Feathers", 2)
                    }

                }
            });

            _features.Add(new FeatureRecipe(this)
            {
                Name = "BoltBlackmetal",
                EnabledConfigEntry = enableBoltBlackmetal,
                RecipeConfig = new RecipeConfig()
                {
                    Name = "CraftEarlyBoltBlackmetal",
                    Item = "BoltBlackmetal",
                    Amount = 20,
                    CraftingStation = CraftingTable.Forge.InternalName(),
                    MinStationLevel = 4,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 8),
                        new RequirementConfig("BlackMetal", 2),
                        new RequirementConfig("Feathers", 2)
                    }

                }
            });

            // Must be last - easter egg
            _features.Add(new FeatureItem(this)
            {
                Name = "BoltExplosive",
                AssetPath = "Assets/PrefabInstance/BoltExplosive.prefab",
                // no enabled setting
                ItemConfig = new ItemConfig()
                {
                    Amount = 10,
                    CraftingStation = CraftingTable.BlackForge.InternalName(),
                    MinStationLevel = 2,
                    Requirements = new RequirementConfig[] {
                        new RequirementConfig("Wood", 5),
                        new RequirementConfig("Feathers", 2),
                        new RequirementConfig("Ooze", 5),
                        new RequirementConfig("SurtlingCore", 5),
                        new RequirementConfig("Coal", 5),
                        new RequirementConfig("Eitr", 5),
                        new RequirementConfig("Flametal", 1) //maybe 2, unsure
                    }
                }
            });
        }
    }
}

