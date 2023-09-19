using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CraftedBossDrops
{
    public static class Extensions
    {
        public static Recipe GetRecipe(this List<Recipe> list, Recipe recipe)
        {
            int index = ObjectDB.instance.m_recipes.IndexOf(recipe);
            if (index > -1) {
                return list[index];
            }

            string name = recipe.ToString();
            return ObjectDB.instance.m_recipes.FirstOrDefault(r => name.Equals(r.ToString()));
        }

        // Workaround to ensure Recipe objects use the correct recipe settings
        private static HashSet<CustomRecipe> hashsetRecipes = null;
        public static bool Update(this CustomRecipe recipe, RecipeConfig newRecipe)
        {
            if (ZNetScene.instance != null)
            {
                global::Recipe r = ObjectDB.instance.m_recipes.GetRecipe(recipe.Recipe);
                if (r == null)
                {
                    Jotunn.Logger.LogError($"Error updating recipe {recipe?.Recipe?.name}, did not find existing recipe in ObjectDB");
                    return false;
                }

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
            else
            {
                ItemManager.Instance.RemoveRecipe(recipe);
                ItemManager.Instance.AddRecipe(new CustomRecipe(newRecipe));
            }

            return false;
        }
    }
}
