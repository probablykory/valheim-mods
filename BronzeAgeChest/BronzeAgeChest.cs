using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BronzeAgeChest
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    internal class BronzeAgeChest : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "BronzeAgeChest";
        public const string PluginVersion = "1.0.6";  
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static BronzeAgeChest Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("bronzechest");

        public new ManualLogSource Logger { get; private set; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);
        public bool Debug { get { return isDebugEnabled is not null ? isDebugEnabled.Value : true; } }
        private static ConfigEntry<bool> isDebugEnabled = null!;

        public Entries Entry { get; protected set; }
        private CustomPiece bronzeChest;

        private const string Name = "$piece_chest_bronze";
        private const string PieceTable = "Hammer";
        private const string Category = "Furniture";

        private void Awake()
        {
            Instance = this;

            isDebugEnabled = this.Config("1 - General", "Debugging Enabled", false, "If on, mod will output alot more information in the debug log level.");

            InitializeFeatures();
            AddDefaultLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            LocalizationManager.OnLocalizationAdded += OnLocalizationAdded;
            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            Config.ConfigReloaded += OnConfigReloaded;

            var _ = new ConfigWatcher(this);
        }

        private void InitializeFeatures()
        {
            Entry = Entries.GetFromProps(Instance, "BronzeChest", nameof(CraftingStations.Workbench), "Wood:15,Bronze:1,BronzeNails:10");
        }

        private void UpdateFeatures()
        {
            bronzeChest.Update(getPieceFromEntry(Entry));
        }

        private void OnVanillaPrefabsAvailable()
        {
            this.LogDebugOnly("Vanilla Prefabs Available received.");
            bronzeChest = new CustomPiece(assetBundle, "piece_chest_bronze", true, getPieceFromEntry(Entry));
            PieceManager.Instance.AddPiece(bronzeChest);

            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }


        private void OnConfigReloaded(object sender, System.EventArgs e)
        {
            UpdateFeatures();
        }

        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            UpdateFeatures();
        }

        private PieceConfig getPieceFromEntry(Entries entry)
        {
            PieceConfig piece = new PieceConfig()
            {
                Name = Name,
                CraftingStation = CraftingStations.GetInternalName(entry.Table.Value),
                PieceTable = PieceTable,
                Category = Category,
                Requirements = RequirementsEntry.Deserialize(entry.Requirements.Value)
            };

            return piece;
        }


        private static Dictionary<string, string> DefaultEnglishLanguageStrings = new Dictionary<string, string>() {
            {"$piece_chest_bronze", "Rugged Chest"}
        };

        private void AddDefaultLocalizations()
        {
            this.LogDebugOnly("AddLocalizations called.");
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();
            loc.AddTranslation("English", DefaultEnglishLanguageStrings);
        }

        private void OnLocalizationAdded()
        {
            this.LogDebugOnly("Localization Added received.");

            string pluginPath = Instance.GetType().Assembly.Location.Replace(Path.DirectorySeparatorChar + PluginName + ".dll", "");
            string pluginFolder = pluginPath;
            if (BepInEx.Paths.PluginPath.Equals(pluginFolder))
            {
                // If our parent folder and bepinex's plugin folder are the same, use a subdir instead.
                pluginFolder = Utility.CombinePaths(new string[] { BepInEx.Paths.PluginPath, PluginName });
            }

            string locFile = Utility.CombinePaths(new string[] { pluginFolder, "Translations", "English", "english.json" });
            string locPath = Path.GetDirectoryName(locFile);

            if (!(Directory.Exists(locPath) && File.Exists(locFile)))
            {
                // Write defaults out to disk for end-users
                Directory.CreateDirectory(locPath);
                string fileContent = SimpleJson.SimpleJson.SerializeObject(DefaultEnglishLanguageStrings);
                File.WriteAllText(locFile, fileContent);

                this.LogInfo("Default localizations written to " + locFile);
            }

            LocalizationManager.OnLocalizationAdded -= OnLocalizationAdded;
        }
    }
}