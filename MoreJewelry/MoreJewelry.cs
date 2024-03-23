using ItemManager;
using LocalizationManager;
using Managers;
using MockManager;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using System.Collections.Generic;
using MoreJewelry.Data;
using Logger = Managers.Logger;

namespace MoreJewelry
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("org.bepinex.plugins.jewelcrafting", BepInDependency.DependencyFlags.SoftDependency)]
    public class MoreJewelry : BaseUnityPlugin, IPlugin
    {
        internal const string PluginName = "MoreJewelry";
        internal const string PluginVersion = "1.0.0";
        internal const string PluginAuthor = "probablykory";
        internal const string PluginGUID = PluginAuthor + "." + PluginName;

        public new Managers.Logger Logger { get { return Managers.Logger.Instance; } } // occlude BaseUnityPlugin.Logger
        public ManualLogSource LogSource { get; private set; } = BepInEx.Logging.Logger.CreateLogSource(PluginName); // non-debug logger

        public bool Debug { get { return isDebugEnabled is not null ? isDebugEnabled.Value == Toggle.On : false; } }

        internal static string ConnectionError = "";

        internal static MoreJewelry Instance = null;
        private readonly Harmony harmony = new(PluginGUID);
        public Harmony Harmony { get { return harmony; } }

        private AssetBundle assetBundle;
        private GameObject rootContainer;
        internal GameObject Container { get { return rootContainer; } }


        internal static readonly ConfigSync ConfigSync = new(PluginGUID)
        { DisplayName = PluginName, CurrentVersion = PluginVersion, MinimumRequiredVersion = PluginVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            Instance = this;

            if (!JcAPI.IsLoaded())
            {
                Logger.LogFatal($"Jewelcrafting mod is missing, unable to load {PluginName}.");
                return;
            }

            yamlConfig = new YamlConfig(Path.GetDirectoryName(Config.ConfigFilePath), PluginName);
            yamlConfig.YamlConfigChanged += OnYamlConfigChanged;
            yamlEditor = gameObject.AddComponent<YamlConfigEditor>();
            yamlEditor.Initialize(PluginName, ConfigSync, yamlConfig, YamlConfig.ErrorCheck);


            int order = 0;
            serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(serverConfigLocked);

            isDebugEnabled = config("1 - General", "Debugging Enabled", Toggle.Off,
                new ConfigDescription("If on, mod will output alot more information in the debug log level.", null,
                new ConfigurationManagerAttributes() { IsAdvanced = true })); ;

            useExternalYaml = ConfigSync.AddConfigEntry(Config.Bind("2 - Jewelry", "Use External YAML", Toggle.Off,
                new ConfigDescription("If set to on, the YAML file from your config folder will be used to configure custom jewelry.", null,
                new ConfigurationManagerAttributes() { Order = --order })));
            useExternalYaml.SourceConfig.SettingChanged += OnExternalYamlSettingChanged;
            yamlAnchor = config("2 - Jewelry", "YAML Editor Anchor", 0, new ConfigDescription("Just ignore this.", null, new ConfigurationManagerAttributes {
                    HideSettingName = true, HideDefaultButton = true, CustomDrawer = yamlEditor.DrawYamlEditorButton, Order = --order, Browsable = false }), false);

            perceptionLocations = config("2 - Jewelry", "Perception Locations", "Vendor_BlackForest:Hildir_camp",
                new ConfigDescription("The list of locations Perception will lead the wearer towards.", new AcceptableValueConfigNote("You must use valid location names, separated by a colon."), 
                new ConfigurationManagerAttributes() { CustomDrawer = ConfigDrawers.DrawLocationsConfigTable(), Order = --order }));
            perceptionLocations.SettingChanged += (_, _) => Perception.SetLocations(perceptionLocations.Value);
            perceptionCooldown = config("2 - Jewelry", "Perception Cooldown", 30f,
                new ConfigDescription("The interval in seconds the effect will trigger.", null,
                new ConfigurationManagerAttributes() { Order = --order }));
            perceptionMinDistance = config("2 - Jewelry", "Perception Minimum Distance", 1000f,
                new ConfigDescription("The minimum distance to a location required to trigger the effect.", null,
                new ConfigurationManagerAttributes() { Order = --order }));

            Localizer.Load();
            harmony.PatchAll();
            JcPatches.DoPatches(harmony);

            rootContainer = new GameObject("jewelryPrefabs");
            rootContainer.transform.parent = Main.GetRootObject().transform;
            rootContainer.SetActive(false);

            assetBundle = PrefabManager.RegisterAssetBundle("jewelry");
            Logger.LogDebugOnly($"assetbundle loaded {assetBundle}");


            yamlConfig.LoadInitialConfig(useExternalYaml.Value == Toggle.On);
            OnExternalYamlSettingChanged();
            LoadAssets();

            Events.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            _ = new ConfigWatcher(this);
        }

        private void OnExternalYamlSettingChanged(object sender = null, EventArgs e = null)
        {
            bool isLocal = true;
            if (useExternalYaml.SynchronizedConfig && useExternalYaml.LocalBaseValue is not null)
                isLocal = ((Toggle)useExternalYaml.LocalBaseValue) == useExternalYaml.Value;

            bool enabled = useExternalYaml.Value == Toggle.On;
            yamlConfig.EnableRaisingFileEvents = enabled;
            yamlConfig.UseBuiltinConfig = !enabled;
            var cma = yamlAnchor.Description.Tags.Where(o => o is ConfigurationManagerAttributes).FirstOrDefault() as ConfigurationManagerAttributes;
            cma.Browsable = enabled;
            ConfigDrawers.ReloadConfigDisplay();

            if (sender != null)
                yamlConfig.ReloadConfig(isLocal);
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        // 1 - General
        private static ConfigEntry<Toggle> serverConfigLocked = null!;
        private static ConfigEntry<Toggle> isDebugEnabled = null!;
        private static SyncedConfigEntry<Toggle> useExternalYaml = null!;
        private static ConfigEntry<int> yamlAnchor = null!;
        private static YamlConfig yamlConfig;
        private static YamlConfigEditor yamlEditor;
        private static ConfigEntry<string> perceptionLocations = null!;
        internal static ConfigEntry<float> perceptionCooldown = null!;
        internal static ConfigEntry<float> perceptionMinDistance = null!;


        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);

            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public bool? HideSettingName;
            [UsedImplicitly] public bool? HideDefaultButton;
            [UsedImplicitly] public bool? IsAdvanced;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }


        private void OnConfigReloaded(object sender, EventArgs e)
        {
            Logger.LogDebugOnly("Config reloaded received.");

            //TODO
        }

        private void OnVanillaPrefabsAvailable()
        {
            Logger.LogDebugOnly($"OnVanillaPrefabsAvailable fired.");
            
            JewelryManager.Initialize();
            JewelryManager.AvailablePrefabs.Values.FixReferences(true);
            loadedItems.Values.Do((i) => {
                i.Prefab.FixReferences(true);
                Icons.SnapshotItem(i.Prefab.GetComponent<ItemDrop>());
            });
            ApplyYamlConfig();
        }

        private bool yamlParsed = false;
        private void OnYamlConfigChanged()
        {
            Logger.LogDebugOnly($"YamlConfigChanged fired, contains {yamlConfig.Config}");
            yamlParsed = true;

            ApplyYamlConfig();
        }

        private bool assetsLoaded = false;
        private void LoadAssets()
        {
            var defaultNeckGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Necklace_Default_PK");
            var wroughtNeckGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Necklace_Wrought_PK");
            var leatherNeckGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Necklace_Leather_PK");
            var silverNeckGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Necklace_Silver_PK");
            var defaultRingGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Ring_Default_PK.prefab");
            var wroughtRingGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Ring_Wrought_PK");
            var stoneRingGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Ring_Stone_PK.prefab");
            var boneRingGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Ring_Bone_PK.prefab");
            var silverRingGO = PrefabManager.RegisterPrefab(assetBundle, "Custom_Ring_Silver_PK.prefab");


            JewelryManager.AvailablePrefabs.Add(JewelryKind.DefaultNecklace, defaultNeckGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.CustomNecklace, wroughtNeckGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.LeatherNecklace, leatherNeckGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.SilverNecklace, silverNeckGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.DefaultRing, defaultRingGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.CustomRing, wroughtRingGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.StoneRing, stoneRingGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.BoneRing, boneRingGO);
            JewelryManager.AvailablePrefabs.Add(JewelryKind.SilverRing, silverRingGO);
            assetsLoaded = true;

            ApplyYamlConfig();
        }

        // ==================================================

        private static string SanitizeName(string name)
        {
            return name.Replace("  ", " ").Trim().Replace(" ", "_");
        }

        private static readonly Dictionary<string, string> stationMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "None", string.Empty },
            { "Workbench", "Workbench" },
            { "Forge", "Forge" },
            { "Stonecutter", "Stonecutter" },
            { "Cauldron", "Cauldron" },
            { "ArtisanTable", "ArtisanTable" },
            { "BlackForge", "BlackForge" },
            { "GaldrTable", "GaldrTable" },
            { "GemcutterTable", JcAPI.GetGemcuttersTable().name},
            { "Gemcutter", JcAPI.GetGemcuttersTable().name}
        };
        private static string getStation(string station)
        {
            if (stationMap.TryGetValue(station, out var value))
                return value;
            return station;
        }

        private static JewelryKind mapNameToJewelryKind(string prefab)
        {
            StringComparison sc = StringComparison.InvariantCultureIgnoreCase;
            return prefab switch
            {
                string _ when prefab.IndexOf("jc_ring_", sc) >= 0         => JewelryKind.DefaultRing,
                string _ when prefab.IndexOf("defaultring", sc) >= 0      => JewelryKind.DefaultRing,
                string _ when prefab.IndexOf("jc_necklace_", sc) >= 0     => JewelryKind.DefaultNecklace,
                string _ when prefab.IndexOf("defaultneck", sc) >= 0      => JewelryKind.DefaultNecklace,
                string _ when prefab.IndexOf("defaultnecklace", sc) >= 0  => JewelryKind.DefaultNecklace,

                string _ when prefab.IndexOf("jc_custom_ring", sc) >= 0       => JewelryKind.CustomRing,
                string _ when prefab.IndexOf("customring", sc) >= 0           => JewelryKind.CustomRing,
                string _ when prefab.IndexOf("jc_custom_necklace", sc) >= 0   => JewelryKind.CustomNecklace,
                string _ when prefab.IndexOf("customneck", sc) >= 0           => JewelryKind.CustomNecklace,
                string _ when prefab.IndexOf("customnecklace", sc) >= 0       => JewelryKind.CustomNecklace,

                string _ when prefab.IndexOf("leathernecklace", sc) >= 0 => JewelryKind.LeatherNecklace,
                string _ when prefab.IndexOf("leatherneck", sc) >= 0 => JewelryKind.LeatherNecklace,
                string _ when prefab.IndexOf("leather", sc) >= 0 => JewelryKind.LeatherNecklace,

                string _ when prefab.IndexOf("silvernecklace", sc) >= 0 => JewelryKind.LeatherNecklace,
                string _ when prefab.IndexOf("silverneck", sc) >= 0 => JewelryKind.SilverNecklace,

                string _ when prefab.IndexOf("stonering", sc) >= 0    => JewelryKind.StoneRing,
                string _ when prefab.IndexOf("bonering", sc) >= 0     => JewelryKind.BoneRing,
                string _ when prefab.IndexOf("silverring", sc) >= 0   => JewelryKind.SilverRing,

                _ => JewelryKind.None
            };
        }

        private GemStyle determineStyle(VisualData config)
        {
            GemStyle style = GemStyle.None;
            if (config.Visible && config.Color != null)
                style = GemStyle.Color;
            else if (config.Visible)
                style = GemStyle.Default;
            return style;
        }

        private string getItemName(JewelryData data)
        {
            string name = SanitizeName(data.Name);
            JewelryKind kind = mapNameToJewelryKind(data.Prefab);

            if (JewelryManager.AvailablePrefabs.ContainsKey(kind))
            {
                if (kind is JewelryKind.DefaultNecklace or JewelryKind.CustomNecklace
                    or JewelryKind.LeatherNecklace or JewelryKind.SilverNecklace)
                    name = "Necklace_" + name;
                else
                    name = "Ring_" + name;
                return $"JC_{name}";
            }

            return null!;
        }

        private GameObject getItemPrefab(JewelryData data)
        {
            string name = SanitizeName(data.Name);
            JewelryKind kind = mapNameToJewelryKind(data.Prefab);
            GemStyle style = determineStyle(data.Gem);

            if (JewelryManager.AvailablePrefabs.TryGetValue(kind, out GameObject go))
            {
                string localized;
                if (kind is JewelryKind.DefaultNecklace or JewelryKind.CustomNecklace
                    or JewelryKind.LeatherNecklace or JewelryKind.SilverNecklace)
                {
                    localized = "jc_necklace_" + name.ToLower();
                    name = "Necklace_" + name;
                }
                else
                {
                    localized = "jc_ring_" + name.ToLower();
                    name = "Ring_" + name;
                }

                return JcAPI.CreateItemFromTemplate(go, name, localized, style, data.Gem.Color);
            }

            return null!;
        }

        private static readonly Dictionary<string, Item> loadedItems = new();
        private void ApplyYamlConfig()
        {
            if (!assetsLoaded || !yamlParsed)
                return;

            HashSet<string> activeTrinkets = new();

            foreach (var config in yamlConfig.Config.Jewelry)
            {
                var name = getItemName(config);
                if (name == null)
                {
                    Logger.LogWarning($"Name {config.Prefab} could not be matched to a valid prefab.  Skipping.");
                    continue;
                }

                activeTrinkets.Add(name);

                if (!loadedItems.TryGetValue(name, out Item item))
                {
                    if (PrefabManager.GetPrefab(config.Name) is not null)
                    {
                        Logger.LogWarning($"Could not add {config.Name} as an internal entity named such already exists.");
                        continue;
                    }


                    var gameobject = getItemPrefab(config);
                    if (gameobject == null)
                    {
                        Logger.LogWarning($"Name {item.Prefab} could not be matched to a valid prefab.  Skipping.");
                        continue;
                    }

                    item = loadedItems[name] = new Item(gameobject)
                    {
                        Configurable = Configurability.Disabled,
                        Prefab = { name = name },
                    };
                }

                item.Name.English(config.Name);
                item.Description.English(config.Description ?? "A random trinket.");

                var kind = mapNameToJewelryKind(config.Prefab);
                JcAPI.SetItemStyle(item.Prefab, determineStyle(config.Gem), config.Gem.Color);

                var shared = item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared;
                if (JewelryManager.IsInitialized())
                {
                    JewelryManager.ClearAllEffectsFromItem(item);
                    if ((config.effect?.NameToResolve ?? null) is { } effectName)
                    {
                        if (JewelryManager.AvailableEffects.ContainsKey(effectName))
                            JewelryManager.AddEffectToItem(item, effectName);
                        else if (effectName.Equals(Effects.Aquatic) || effectName.Equals(Effects.Headhunter))
                            JewelryManager.AddEffectToItem(item, effectName);
                        else
                        {
                            Logger.LogWarning($"{effectName} isn't among the built-in effects, & StatusEffect lookup isn't supported yet. ");
                            // TODO - lookup effect name
                            // Need to delay this lookup until later than ObjectDB.CopyOtherDB.
                            // Whatever solution this has will also need to be able to deal with mid-game config updates
                        }
                    }
                    else if (config.effect?.SeData is { } seData) // adhoc SE_Stats effect
                    {
                        var seName = item.Prefab.name.Replace("JC_", "JC_Se_");
                        if (shared.m_equipStatusEffect == null || !seName.Equals(shared.m_equipStatusEffect.name))
                        {
                            StatusEffect se = seData.ToStatusEffect();
                            se.m_name = string.IsNullOrWhiteSpace(seData.Name) ? config.Name : seData.Name;
                            se.name = seName;
                            se.m_icon = shared.m_icons[0];
                            shared.m_equipStatusEffect = se;
                        }
                    }
                }


                item.Crafting.Stations.Clear();
                item.RequiredItems.Requirements.Clear();
                item.RequiredUpgradeItems.Requirements.Clear();

                item.Crafting.Add(getStation(config.Crafting.CraftingStation), config.Crafting.StationLevel);

                if (config.Crafting.Costs.Count > 0)
                {
                    item.RequiredItems.Requirements.AddRange(config.Crafting.Costs.Select(c =>
                    {
                        return new Requirement()
                        {
                            itemName = c.Name,
                            amount = c.Amount,
                            quality = 0
                        };
                    }).ToArray());
                }

                int maxQuality = 1;
                if (config.Upgrade?.Costs?.Count > 0)
                {
                    foreach (var kvp in config.Upgrade.Costs)
                    {
                        item.RequiredUpgradeItems.Requirements.AddRange(kvp.Value.Select(c =>
                        {
                            return new Requirement()
                            {
                                itemName = c.Name,
                                amount = c.Amount,
                                quality = kvp.Key + 1
                            };
                        }));
                    }
                    maxQuality = (config.Crafting.Costs.Count > 0 ? 1 : 0) + config.Upgrade.Costs.Count;
                }
                else if (config.Crafting.MaxQuality > 1 && config.Crafting.MaxQuality <= 10 && config.Crafting.Costs.Any(c => c.AmountPerLevel > 0))
                {   // we have the data, fill upgrade items with vanilla-style upgrades
                    maxQuality = config.Crafting.MaxQuality;
                    
                    for (int i = 2; i <= maxQuality; i++)
                    {
                        item.RequiredUpgradeItems.Requirements.AddRange(config.Crafting.Costs.Select(c =>
                        {
                            return new Requirement()
                            {
                                itemName = c.Name,
                                amount = c.AmountPerLevel * (i - 1),
                                quality = i
                            };
                        }).ToArray());
                    }
                }
                shared.m_maxQuality = maxQuality;

                if (ZNetScene.instance?.GetPrefab("_ZoneCtrl") != null)
                {
                    item.ReloadCraftingConfiguration();
                    item.ApplyToAllInstances(item =>
                    {
                        item.m_shared = shared;
                        item.m_shared.m_maxQuality = maxQuality;
                    });
                }
            }

            foreach (KeyValuePair<string, Item> kv in loadedItems)
            {
                if (!activeTrinkets.Contains(kv.Key) && kv.Value is not null)
                {
                    // still looking for a better way to deactivate items/recipes..
                    var result = kv.Value.GetActiveRecipesEnabled();
                    if (result == Item.RecipesEnabled.True || result == Item.RecipesEnabled.Mixed)
                    {
                        kv.Value.ToggleAllActiveRecipes(false);
                    }
                    JewelryManager.ClearAllEffectsFromItem(kv.Value);
                }
            }
        }

        private void testCreateJewelry()
        {
            GameObject gameObject;
            Item item;

            gameObject = JcAPI.CreateItemFromTemplate(JewelryManager.AvailablePrefabs[JewelryKind.LeatherNecklace], "Necklace Leather", "jc_necklace_leather_one");
            item = new Item(gameObject);
            item.Name.English("Leather necklace");
            item.Description.English("Que?");
            item.Crafting.Add(JcAPI.GetGemcuttersTable().name, 1);
            item.RequiredItems.Add("Stone", 1, 0);
            item.MaximumRequiredStationLevel = 3;
            JewelryManager.AddEffectToItem(item, Effects.Aquatic);

            gameObject = JcAPI.CreateItemFromTemplate(JewelryManager.AvailablePrefabs[JewelryKind.SilverRing], "Ring Silver", "jc_ring_silver_one");
            item = new Item(gameObject);
            item.Name.English("Silver ring");
            item.Description.English("Que?");

            item.Crafting.Add(JcAPI.GetGemcuttersTable().name, 1);
            item.RequiredItems.Add("Stone", 1, 0);
            item.MaximumRequiredStationLevel = 3;
            JewelryManager.AddEffectToItem(item, Effects.Headhunter);
        }
    }
}