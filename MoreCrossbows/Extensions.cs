using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

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

        // ensure we get the live recipe from ObjectDB
        public static Recipe GetRecipe(this List<Recipe> list, Recipe recipe)
        {
            int index = ObjectDB.instance.m_recipes.IndexOf(recipe);
            if (index > -1)
            {
                return list[index];
            }

            string name = recipe.ToString();
            return ObjectDB.instance.m_recipes.FirstOrDefault(r => name.Equals(r.ToString()));
        }

        // Workaround to ensure Recipe objects use the correct recipe settings
        private static HashSet<CustomRecipe> hashsetRecipes = null;
        public static bool Update(this CustomRecipe recipe, RecipeConfig newRecipe)
        {
            if (hashsetRecipes == null)
            {
                hashsetRecipes = AccessTools.Field(typeof(ItemManager), "Recipes").GetValue(ItemManager.Instance) as HashSet<CustomRecipe>;
                Get.Plugin.LogDebugOnly("Recipes Hashset value retrieved: {hashsetRecipes}");
                //var imMember = ItemManager.Instance.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(m => m.Name == "Recipes");
                //var imField = imMember as FieldInfo;
                //if (imField != null)
                //{
                //    var hset = imField.GetValue(ItemManager.Instance) as HashSet<CustomRecipe>;
                //    if (hset != null)
                //    {
                //        hashsetRecipes = hset;
                //        Get.Plugin.LogDebugOnly("Recipes Hashset value retrieved: {hashsetRecipes}"); // + ObjectDumper.Dump(hashsetRecipes));
                //    }
                //}
            }
            global::Recipe r = ObjectDB.instance.m_recipes.GetRecipe(recipe.Recipe);
            if (r == null)
            {
                if (hashsetRecipes != null && hashsetRecipes.Contains(recipe))
                {
                    Get.Plugin.LogDebugOnly($"Removing and re-adding recipe {recipe?.Recipe?.name} in ItemManager.");
                    ItemManager.Instance.RemoveRecipe(recipe);
                    ItemManager.Instance.AddRecipe(new CustomRecipe(newRecipe));
                    return true;
                }
                else
                {
                    Get.Plugin.LogError($"Error updating recipe {recipe?.Recipe?.name}, did not find existing recipe in ObjectDB or ItemManager");
                    return false;
                }
            }

            // Update existing recipe in place.
            Get.Plugin.LogDebugOnly($"Updating recipe {recipe?.Recipe?.name} in place.");
            r.m_amount = newRecipe.Amount;
            r.m_minStationLevel = newRecipe.MinStationLevel;
            r.m_craftingStation = PrefabManager.Instance.GetPrefab(newRecipe.CraftingStation)?.GetComponent<CraftingStation>();
            Get.Plugin.LogDebugOnly($"... setting cs to {r.m_craftingStation}.");
            r.m_resources = newRecipe.GetRequirements();

            foreach (var res in r.m_resources)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(res.m_resItem.name.Replace("JVLmock_", ""));
                if (prefab != null)
                {
                    res.m_resItem = prefab.GetComponent<ItemDrop>();
                }
            }
            if (hashsetRecipes != null)
            {
                hashsetRecipes.Remove(recipe);
                hashsetRecipes.Add(new CustomRecipe(r, false, false));
            }

            return true;
        }

        private static Dictionary<string, CustomPrefab> prefabsDict = null;
        public static bool PrefabExists(this PrefabManager instance, string name)
        {
            bool result = false;

            if (String.IsNullOrEmpty(name))
            {
                return result;
            }

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

        public static void ApplyToAll(this ItemDrop itemDrop, Action<ItemDrop.ItemData> callback)
        {
            callback(itemDrop.m_itemData);

            string itemName = itemDrop.m_itemData.m_shared.m_name;

            Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(UnityEngine.Object.FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c != null).ToArray();
            foreach (ItemDrop.ItemData itemdata in ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => i.m_itemData).Concat(inventories.SelectMany(i => i.GetAllItems())))
            {
                if (itemdata.m_shared.m_name == itemName)
                {
                    callback(itemdata);
                }
            }
        }

        public static object Cast(this Type Type, object data)
        {
            var DataParam = Expression.Parameter(typeof(object), "data");
            var Body = Expression.Block(Expression.Convert(Expression.Convert(DataParam, data.GetType()), Type));

            var Run = Expression.Lambda(Body, DataParam).Compile();
            var ret = Run.DynamicInvoke(data);
            return ret;
        }
    }
}
