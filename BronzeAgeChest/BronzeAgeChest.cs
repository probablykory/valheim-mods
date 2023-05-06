using BepInEx;
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
    internal class BronzeAgeChest : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "BronzeAgeChest";
        public const string PluginVersion = "1.0.2.0";  
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static BronzeAgeChest Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("bronzechest");

        public Entries Entries { get; protected set; }

        private void Awake()
        {
            Instance = this;

            InitializeFeatures();
            AddDefaultLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            LocalizationManager.OnLocalizationAdded += OnLocalizationAdded;
        }

        private void InitializeFeatures()
        {
            Entries = Entries.GetFromProps(Instance, "BronzeChest", CraftingTable.Workbench, "Wood:15,Bronze:1,BronzeNails:10");
        }

        private void OnVanillaPrefabsAvailable()
        {
            Jotunn.Logger.LogDebug("Vanilla Prefabs Available received.");

            PieceConfig bronzeChest = new PieceConfig() {
                Name = "$piece_chest_bronze",
                CraftingStation = CraftingTable.GetInternalName(Entries != null ? Entries.Table.Value : CraftingTable.Workbench),
                PieceTable = "Hammer",
                Category = "Furniture",
                Requirements = RequirementsEntry.Deserialize(Entries != null ? Entries.Requirements.Value : "Wood:15,Bronze:1,BronzeNails:10")
            };
            PieceManager.Instance.AddPiece(new CustomPiece(assetBundle, "piece_chest_bronze", true, bronzeChest));

            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private static Dictionary<string, string> DefaultEnglishLanguageStrings = new Dictionary<string, string>() {
            {"$piece_chest_bronze", "Rugged Chest"}
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

            string locFile = Utility.CombinePaths(new string[] { pluginFolder, "Translations", "English", "english.json" });
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
    }
}