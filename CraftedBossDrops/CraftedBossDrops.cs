using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace CraftedBossDrops
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class CraftedBossDrops : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "CraftedBossDrops";
        public const string PluginVersion = "1.0.1.0";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        private Harmony harmony = null;
        internal static CraftedBossDrops Instance;

        public Entries HardAntlerEntry { get; protected set; }
        public Entries CryptKeyEntry { get; protected set; }
        public Entries WishboneEntry { get; protected set; }
        public Entries DragonTearEntry { get; protected set; }
        public Entries YagluthDropEntry { get; protected set; }

        private void Awake()
        {
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            Instance = this;

            InitializeFeatures();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
        }

        private void InitializeFeatures()
        {
            HardAntlerEntry = Entries.GetFromProps(Instance, "HardAntler", CraftingTable.Workbench, 3, 1, "TrophyDeer:20,Resin:2");
            CryptKeyEntry = Entries.GetFromProps(Instance, "CryptKey", CraftingTable.Forge, 3, 1, "AncientSeed:15,Bronze:2");
            WishboneEntry = Entries.GetFromProps(Instance, "Wishbone", CraftingTable.Forge, 7, 1, "WitheredBone:30,Iron:2");
            DragonTearEntry = Entries.GetFromProps(Instance, "DragonTear", CraftingTable.Workbench, 5, 2, "DragonEgg:6,Crystal:10");
            YagluthDropEntry = Entries.GetFromProps(Instance, "YagluthDrop", CraftingTable.ArtisanTable, 1, 2, "GoblinTotem:10,TrophyGoblin:3,TrophyGoblinShaman:1,TrophyGoblinBrute:1");
        }

        private void OnVanillaPrefabsAvailable()
        {
            Jotunn.Logger.LogDebug("Vanilla Prefabs Available received.");

            ItemManager.Instance.AddRecipe(getRecipeFromEntry(HardAntlerEntry));
            ItemManager.Instance.AddRecipe(getRecipeFromEntry(CryptKeyEntry));
            ItemManager.Instance.AddRecipe(getRecipeFromEntry(WishboneEntry));
            ItemManager.Instance.AddRecipe(getRecipeFromEntry(DragonTearEntry));
            ItemManager.Instance.AddRecipe(getRecipeFromEntry(YagluthDropEntry));

        }

        private CustomRecipe getRecipeFromEntry(Entries entry)
        {
            CustomRecipe recipe = new CustomRecipe(new RecipeConfig()
            {
                Item = entry.Name,
                CraftingStation = CraftingTable.GetInternalName(entry.Table.Value),
                RepairStation = CraftingTable.GetInternalName(entry.Table.Value),
                MinStationLevel = entry.MinTableLevel.Value,
                Amount = entry.Amount.Value,
                Requirements = RequirementsEntry.Deserialize(entry.Requirements.Value)
            });

            return recipe;
        }
    }
}