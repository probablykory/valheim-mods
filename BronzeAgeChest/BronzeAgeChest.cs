using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
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
        public const string PluginVersion = "1.0.0.0";  
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static BronzeAgeChest Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("bronzechest");

        public Entries Entries { get; protected set; }

        private void Awake()
        {
            Instance = this;

            InitializeFeatures();
            AddLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
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

        private void AddLocalizations()
        {
            CustomLocalization loc = new CustomLocalization();

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                {"$piece_chest_bronze", "Rugged Chest"}
            });

            LocalizationManager.Instance.AddLocalization(loc);
        }
    }
}