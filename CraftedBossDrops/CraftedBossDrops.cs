using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine.PlayerLoop;
using Common;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace CraftedBossDrops
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    [BepInDependency("com.bepis.bepinex.configurationmanager", BepInDependency.DependencyFlags.SoftDependency)]
    internal class CraftedBossDrops : BaseUnityPlugin, IPlugin
    {
        public const string PluginAuthor = "probablykory";
        public const string PluginName = "CraftedBossDrops";
        public const string PluginVersion = "1.0.7";
        public const string PluginGUID = PluginAuthor + "." + PluginName;

        private bool settingsUpdated = false;
        private Harmony harmony = null;
        internal static CraftedBossDrops Instance;

        public new ManualLogSource Logger { get; private set; } = BepInEx.Logging.Logger.CreateLogSource(PluginName);
        public bool Debug { get { return isDebugEnabled is not null ? isDebugEnabled.Value : true; } }
        private static ConfigEntry<bool> isDebugEnabled = null!;

        public Entries HardAntlerEntry { get; protected set; }
        public Entries CryptKeyEntry { get; protected set; }
        public Entries WishboneEntry { get; protected set; }
        public Entries DragonTearEntry { get; protected set; }
        public Entries YagluthDropEntry { get; protected set; }
        public Entries QueenDropEntry { get; protected set; }

        private CustomRecipe hardAntlerRecipe;
        private CustomRecipe cryptKeyRecipe;
        private CustomRecipe wishboneRecipe;
        private CustomRecipe dragonTearRecipe;
        private CustomRecipe yagluthDropRecipe;
        private CustomRecipe queenDropRecipe;

        private void Awake()
        {
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            isDebugEnabled = this.Config("1 - General", "Debugging Enabled", false, "If on, mod will output alot more information in the debug log level.");
            Instance = this;
            InitializeFeatures();
            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;
            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;
            Config.ConfigReloaded += OnConfigReloaded;

            var _ = new ConfigWatcher(this);
        }

        private void InitializeFeatures()
        {
            HardAntlerEntry = Entries.GetFromProps(Instance, "HardAntler", nameof(CraftingStations.Workbench), 3, 1, "TrophyDeer:10,Resin:20");
            CryptKeyEntry = Entries.GetFromProps(Instance, "CryptKey", nameof(CraftingStations.Forge), 3, 1, "AncientSeed:6,Bronze:20");
            WishboneEntry = Entries.GetFromProps(Instance, "Wishbone", nameof(CraftingStations.Forge), 7, 1, "WitheredBone:15,Iron:10,TrophyDraugr:3");
            DragonTearEntry = Entries.GetFromProps(Instance, "DragonTear", nameof(CraftingStations.Workbench), 5, 2, "DragonEgg:4,Crystal:16");
            YagluthDropEntry = Entries.GetFromProps(Instance, "YagluthDrop", nameof(CraftingStations.ArtisanTable), 1, 2, "GoblinTotem:10,TrophyGoblin:3,TrophyGoblinShaman:1,TrophyGoblinBrute:1");
            QueenDropEntry = Entries.GetFromProps(Instance, "QueenDrop", nameof(CraftingStations.BlackForge), 2, 1, "DvergrKeyFragment:10,Mandible:5,TrophySeeker:3,TrophySeekerBrute:1");

            HardAntlerEntry.AddSettingsChangedHandler(OnSettingsChanged);
            CryptKeyEntry.AddSettingsChangedHandler(OnSettingsChanged);
            WishboneEntry.AddSettingsChangedHandler(OnSettingsChanged);
            DragonTearEntry.AddSettingsChangedHandler(OnSettingsChanged);
            YagluthDropEntry.AddSettingsChangedHandler(OnSettingsChanged);
            QueenDropEntry.AddSettingsChangedHandler(OnSettingsChanged);
        }

        private void UpdateFeatures()
        {
            settingsUpdated = false;
            hardAntlerRecipe.Update(getRecipeFromEntry(HardAntlerEntry));
            cryptKeyRecipe.Update(getRecipeFromEntry(CryptKeyEntry));
            wishboneRecipe.Update(getRecipeFromEntry(WishboneEntry));
            dragonTearRecipe.Update(getRecipeFromEntry(DragonTearEntry));
            yagluthDropRecipe.Update(getRecipeFromEntry(YagluthDropEntry));
            queenDropRecipe.Update(getRecipeFromEntry(QueenDropEntry));
        }

        private void OnVanillaPrefabsAvailable()
        {
            this.LogDebugOnly("Vanilla Prefabs Available received.");

            hardAntlerRecipe = new CustomRecipe(getRecipeFromEntry(HardAntlerEntry));
            cryptKeyRecipe = new CustomRecipe(getRecipeFromEntry(CryptKeyEntry));
            wishboneRecipe = new CustomRecipe(getRecipeFromEntry(WishboneEntry));
            dragonTearRecipe = new CustomRecipe(getRecipeFromEntry(DragonTearEntry));
            yagluthDropRecipe = new CustomRecipe(getRecipeFromEntry(YagluthDropEntry));
            queenDropRecipe = new CustomRecipe(getRecipeFromEntry(QueenDropEntry));

            ItemManager.Instance.AddRecipe(hardAntlerRecipe);
            ItemManager.Instance.AddRecipe(cryptKeyRecipe);
            ItemManager.Instance.AddRecipe(wishboneRecipe);
            ItemManager.Instance.AddRecipe(dragonTearRecipe);
            ItemManager.Instance.AddRecipe(yagluthDropRecipe);
            ItemManager.Instance.AddRecipe(queenDropRecipe);

            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private void OnSettingsChanged(object sender, System.EventArgs e)
        {
            settingsUpdated = true;
        }

        private void OnConfigReloaded(object sender, System.EventArgs e)
        {
            if (settingsUpdated)
            {
                UpdateFeatures();
            }
        }

        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            UpdateFeatures();
        }

        private RecipeConfig getRecipeFromEntry(Entries entry)
        {
            RecipeConfig recipe = new RecipeConfig()
            {
                Item = entry.Name,
                CraftingStation = CraftingStations.GetInternalName(entry.Table.Value),
                RepairStation = CraftingStations.GetInternalName(entry.Table.Value),
                MinStationLevel = entry.MinTableLevel.Value,
                Amount = entry.Amount.Value,
                Requirements = RequirementsEntry.Deserialize(entry.Requirements.Value)
            };

            return recipe;
        }
    }
}