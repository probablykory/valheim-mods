using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace BronzeEraChest
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BronzeEraChest : BaseUnityPlugin
    {

        public const string PluginAuthor = "probablykory";
        public const string PluginName = "BronzeEraChest";
        public const string PluginVersion = "1.0.0.0";  
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static BronzeEraChest Instance;
        internal AssetBundle assetBundle = AssetUtils.LoadAssetBundleFromResources("bronzechest");

        private void Awake()
        {
            Instance = this;

            AddLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        }

        private void OnVanillaPrefabsAvailable()
        {
            Jotunn.Logger.LogDebug("Vanilla Prefabs Available received.");

            PieceConfig bronzeChest = new PieceConfig() {
                Name = "$piece_chest_bronze",
                CraftingStation = "piece_workbench",
                PieceTable = "Hammer",
                Category = "Furniture",
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("Wood", 15, 0, true),
                    new RequirementConfig("Bronze", 1, 0, true),
                    new RequirementConfig("BronzeNails", 10, 0, true),
                }
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

        //private void OnDestroy()
        //{
        //    Jotunn.Logger.LogDebug("OnDestroy called.");

        //    PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        //}
    }
}