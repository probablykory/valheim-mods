using BepInEx.Configuration;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreCrossbows
{

    public static class Extensions
    {
        // A workaround until ItemManager.i.RemoveItem gets fixed.
        public static bool Remove(this ObjectDB instance, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return false;
            }

            GameObject prefab = instance.GetItemPrefab(prefabName);
            if (prefab != null)
            {
                instance.m_items.Remove(prefab);
                return true;
            }

            return false;
        }
    }

    internal class Feature
    {
        public Feature(MoreCrossbows mc)
        {
            Plugin = mc;
            EnabledInConfig = false;
            LoadedInGame = false;
            DependencyNames = new List<string>();
        }

        public MoreCrossbows Plugin { get; set; }
        public bool EnabledInConfig { get; set; }
        public bool LoadedInGame { get; set; }
        public string Name { get; set; }
        public List<string> DependencyNames { get; set; }
        public ConfigEntry<bool> EnabledConfigEntry { get; set; }

        public virtual bool Load() { return false; }
        public virtual bool Unload() { return false; }
    }

    internal class FeatureItem : Feature
    {
        public FeatureItem(MoreCrossbows mc) : base(mc) { }

        public string AssetPath { get; set; }
        public ItemConfig ItemConfig { get; set; }

        public override bool Load()
        {
            var f = this;
            Jotunn.Logger.LogInfo("Adding item " + f.Name);
            ItemManager.Instance.AddItem(new CustomItem(Plugin._bundle, f.AssetPath, true, f.ItemConfig));
            f.LoadedInGame = true;

            return true;
        }

        public override bool Unload()
        {
            var f = this;
            Jotunn.Logger.LogInfo("Removing item " + f.Name);
            ItemManager.Instance.RemoveItem(f.Name);
            //PrefabManager.Instance.DestroyPrefab(f.Name); // this needs testing
            ObjectDB.instance.Remove(f.Name);
            f.LoadedInGame = false;

            return true;
        }
    }

    internal class FeatureRecipe : Feature
    {
        public FeatureRecipe(MoreCrossbows mc) : base(mc) { }

        public RecipeConfig RecipeConfig { get; set; }

        public override bool Load()
        {
            var f = this;
            Jotunn.Logger.LogInfo("Adding recipe for " + f.Name);
            ItemManager.Instance.AddRecipe(new CustomRecipe(f.RecipeConfig));
            f.LoadedInGame = true;

            return true;
        }


        public override bool Unload()
        {
            var f = this;
            Jotunn.Logger.LogInfo("Removing recipe for " + f.Name);
            ItemManager.Instance.RemoveRecipe("CraftEarly" + f.Name);
            f.LoadedInGame = false;

            return true;
        }
    }
}
