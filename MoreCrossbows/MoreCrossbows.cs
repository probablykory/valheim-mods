using BepInEx;
using System.Linq;
using Jotunn;
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
        Inventory,
        [InternalName("piece_workbench")] Workbench,
        [InternalName("piece_cauldron")] Cauldron,
        [InternalName("forge")] Forge,
        [InternalName("piece_artisanstation")] ArtisanTable,
        [InternalName("piece_stonecutter")] StoneCutter,
        [InternalName("piece_magetable")] MageTable,
        [InternalName("blackforge")] BlackForge
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
        public const string PluginVersion = "1.1.0.0";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        public static string ConfigFileName
        {
            get
            {
                return PluginAuthor + "." + PluginName + ".cfg";
            }
        }

        internal static MoreCrossbows Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("crossbows");
        private bool _vanillaPrefabsAvailable = false;
        private List<Feature> _features = new List<Feature>();

        // Main entry point
        private void Awake()
        {
            MoreCrossbows.Instance = this;
            base.Config.SaveOnConfigSet = true;
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
                Jotunn.Logger.LogDebug("DEBUG: config sync");
                Jotunn.Logger.LogDebug("DEBUG: " + sender + " " + e.ToString());
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

        //private WeakReference<Dictionary<string, CustomPrefab>> prefabsDict = null;
        private Dictionary<string, CustomPrefab> prefabsDict = null;
        private bool PrefabExists(string name)
        {
            bool result = false;

            if (String.IsNullOrEmpty(name))
            {
                return result;
            }

            //Dictionary<string, CustomPrefab> dict = null;
            //if (prefabsDict == null || !prefabsDict.TryGetTarget(out dict))
            if (prefabsDict == null)
            {
                var prefabsMember = PrefabManager.Instance.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Prefabs");
                var prefabsProp = prefabsMember as PropertyInfo;
                if (prefabsProp != null)
                {
                    var dict = prefabsProp.GetValue(PrefabManager.Instance) as Dictionary<string, CustomPrefab>;
                    if (dict != null)
                    {
                        prefabsDict = dict;
                    }
                }
            }
            if (prefabsDict != null)
            {
                result = prefabsDict.ContainsKey(name);
            }

            return result;
        }

        // force references to fix for indirect dependencies
        private void RegisterCustomPrefab(AssetBundle bundle, string assetName)
        {
            string prefabName = assetName.Replace(".prefab", "");
            if (!String.IsNullOrEmpty(prefabName) && !PrefabExists(prefabName))
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
                    Jotunn.Logger.LogDebug("DEBUG: allFeaturesEnabled = " + areAllFeaturesEnabled.ToString());
                    Jotunn.Logger.LogDebug("DEBUG: Feature " + f.Name + " is " + (isEnabled ? "enabled" : "disabled") + " and " + (f.LoadedInGame ? "Loaded" : "Unloaded"));
                }

                if (f.RequiresUnload)
                {
                    f.Unload();
                }

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

            _features.Add(new FeatureItem("CrossbowWood")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Wooden Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowWood.prefab",

                Table = CraftingTable.Workbench,
                MinTableLevel = 2,
                Requirements = "Wood:20:8,Stone:8:2,LeatherScraps:8:2",
            });

            _features.Add(new FeatureItem("CrossbowBronze")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Bronze Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowBronze.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:4,FineWood:4:2,Bronze:8:2,DeerHide:2:1",
            });

            _features.Add(new FeatureItem("CrossbowIron")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Iron Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowIron.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:4,ElderBark:4:2,Iron:8:2,Root:1",
            });

            _features.Add(new FeatureItem("CrossbowSilver")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Silver Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowSilver.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "Wood:10:4,Silver:4:2,Iron:8:4,WolfHairBundle:6",
            });

            _features.Add(new FeatureItem("CrossbowBlackmetal")
            {
                Category = "1 - Crossbows",
                Description = "Adds a new Blackmetal Crossbow weapon",
                EnabledByDefault = true,
                AssetPath = "Assets/PrefabInstance/CrossbowBlackmetal.prefab",

                Table = CraftingTable.Forge,
                MinTableLevel = 1,
                Requirements = "FineWood:10:4,BlackMetal:10:4,Iron:8:4,LoxPelt:2:1",
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

