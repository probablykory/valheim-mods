using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
            global::Recipe r = ObjectDB.instance.m_recipes.GetRecipe(recipe.Recipe);
            if (r == null)
            {
                if (hashsetRecipes != null && hashsetRecipes.Contains(recipe))
                {
                    Jotunn.Logger.LogDebug($"Removing and re-adding recipe {recipe?.Recipe?.name} in ItemManager.");
                    ItemManager.Instance.RemoveRecipe(recipe);
                    ItemManager.Instance.AddRecipe(new CustomRecipe(newRecipe));
                    return true;
                }
                else
                {
                    Jotunn.Logger.LogError($"Error updating recipe {recipe?.Recipe?.name}, did not find existing recipe in ObjectDB or ItemManager");
                    return false;
                }
            }

            // Update existing recipe in place.
            Jotunn.Logger.LogDebug($"Updating recipe {recipe?.Recipe?.name} in place.");
            r.m_amount = newRecipe.Amount;
            r.m_minStationLevel = newRecipe.MinStationLevel;
            r.m_craftingStation = PrefabManager.Instance.GetPrefab(newRecipe.CraftingStation)?.GetComponent<CraftingStation>();
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
