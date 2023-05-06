using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                instance.m_itemByHash.Remove(prefab.name.GetStableHashCode());

                return true;
            }

            return false;
        }

        // Workaround to ensure Recipe objects use the correct recipe settings
        private static HashSet<CustomRecipe> hashsetRecipes = null;
        public static bool Update(this CustomRecipe recipe, RecipeConfig newRecipe)
        {
            if (ZNetScene.instance != null)
            {
                global::Recipe r = recipe.Recipe;

                // Update existing recipe in place.
                r.m_amount = newRecipe.Amount;
                r.m_minStationLevel = newRecipe.MinStationLevel;
                r.m_craftingStation = ZNetScene.instance.GetPrefab(newRecipe.CraftingStation).GetComponent<CraftingStation>();
                r.m_resources = newRecipe.GetRequirements();

                foreach (var res in r.m_resources)
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(res.m_resItem.name.Replace("JVLmock_", ""));
                    if (prefab != null)
                    {
                        res.m_resItem = prefab.GetComponent<ItemDrop>();
                    }
                }

                // cache refernce to ItemManager.Instance.Recipes
                if (hashsetRecipes == null)
                {
                    var imMember = ItemManager.Instance.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Recipes");
                    var imField = imMember as FieldInfo;
                    if (imField != null)
                    {
                        var hset = imField.GetValue(ItemManager.Instance) as HashSet<CustomRecipe>;
                        if (hset != null)
                        {
                            hashsetRecipes = hset;
                            //Jotunn.Logger.LogDebug("Recipes Hashset value retrieved: " + ObjectDumper.Dump(hashsetRecipes));
                        }
                    }
                }
                if (hashsetRecipes != null)
                {
                    hashsetRecipes.Remove(recipe);
                    hashsetRecipes.Add(new CustomRecipe(r, false, false));
                }

                return true;
            }

            return false;
        }

        //private WeakReference<Dictionary<string, CustomPrefab>> prefabsDict = null;
        private static Dictionary<string, CustomPrefab> prefabsDict = null;
        public static bool PrefabExists(this PrefabManager instance, string name)
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
    }
}
