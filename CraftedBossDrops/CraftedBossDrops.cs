using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace CraftedBossDrops
{

    public static class CraftingTable
    {
        public static string Inventory { get { return "Inventory"; } }
        public static string Workbench { get { return "Workbench"; } }
        public static string Cauldron { get { return "Cauldron"; } }
        public static string Forge { get { return "Forge"; } }
        public static string ArtisanTable { get { return "ArtisanTable"; } }
        public static string StoneCutter { get { return "StoneCutter"; } }
        public static string MageTable { get { return "MageTable"; } }
        public static string BlackForge { get { return "BlackForge"; } }

        public static string[] GetValues()
        {
            return new string[]
            {
                Inventory,
                Workbench,
                Cauldron,
                Forge,
                ArtisanTable,
                StoneCutter,
                MageTable,
                BlackForge
            };
        }

        public static string GetInternalName(string name)
        {
            switch (name)
            {
                case "Workbench":
                    return "piece_workbench";
                case "Cauldron":
                    return "piece_cauldron";
                case "Forge":
                    return "forge";
                case "ArtisanTable":
                    return "piece_artisanstation";
                case "StoneCutter":
                    return "piece_stonecutter";
                case "MageTable":
                    return "piece_magetable";
                case "BlackForge":
                    return "blackforge";
            }
            return string.Empty; // "Inventory" or error
        }
    }


    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.Minor)]
    internal class CraftedBossDrops : BaseUnityPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "CraftedBossDrops";
        public const string PluginVersion = "1.0.0.0";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        internal static CraftedBossDrops Instance;
        private Harmony harmony = null;

        private void Awake()
        {
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            Instance = this;


            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        }

        private void OnVanillaPrefabsAvailable()
        {
            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig()
            {
                Item = "HardAntler",
                CraftingStation = CraftingTable.GetInternalName(CraftingTable.Workbench),
                MinStationLevel = 3,
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("TrophyDeer", 20),
                    new RequirementConfig("Resin", 10)
                }
            }));

            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig()
            {
                Item = "CryptKey",
                CraftingStation = CraftingTable.GetInternalName(CraftingTable.Forge),
                MinStationLevel = 3,
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("AncientSeed", 15),
                    new RequirementConfig("Bronze", 2)
                }
            }));

            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig()
            {
                Item = "Wishbone",
                CraftingStation = CraftingTable.GetInternalName(CraftingTable.Forge),
                MinStationLevel = 7,
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("WitheredBone", 30),
                    new RequirementConfig("Iron", 2)
                }
            }));

            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig()
            {
                Item = "DragonTear",
                Amount = 2,
                CraftingStation = CraftingTable.GetInternalName(CraftingTable.Workbench),
                MinStationLevel = 5,
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("DragonEgg", 6),
                    new RequirementConfig("Crystal", 10)
                }
            }));

            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig()
            {
                Item = "YagluthDrop",
                CraftingStation = CraftingTable.GetInternalName(CraftingTable.ArtisanTable),
                MinStationLevel = 1,
                Requirements = new RequirementConfig[] {
                    new RequirementConfig("GoblinTotem", 15),
                    new RequirementConfig("TrophyGoblin", 1),
                    new RequirementConfig("TrophyGoblinShaman", 1),
                    new RequirementConfig("TrophyGoblinBrute", 1)
                }
            }));

            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }
    }
}