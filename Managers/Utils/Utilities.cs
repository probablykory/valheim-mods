using HarmonyLib;
using ItemManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    public static class Utilities
    {
        private static readonly MethodInfo MemberwiseCloneMethod = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
        public static T Clone<T>(T input) where T : notnull => (T)MemberwiseCloneMethod.Invoke(input, Array.Empty<object>());


        //public static T ApplyCopyOf<T>(this GameObject target, GameObject source, bool includeBaseFields = false) where T : class
        //{
        //    return target.ApplyCopyOf(source, typeof(T), includeBaseFields) as T;
        //}

        //private static UnityEngine.Object ApplyCopyOf(this GameObject target, GameObject source, Type type, bool includeBaseFields = false)
        //{
        //    Component component = target.GetComponent(type);
        //    if (component != null)
        //    {
        //        UnityEngine.Object.Destroy(component);
        //    }
        //    Component componentInChildren = source.GetComponentInChildren(type);
        //    component = target.AddComponent(componentInChildren.GetType());
        //    BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        //    if (includeBaseFields)
        //    {
        //        bindingFlags &= ~BindingFlags.DeclaredOnly;
        //    }
        //    UnityEngine.Object obj;
        //    try
        //    {
        //        foreach (PropertyInfo propertyInfo in componentInChildren.GetType().GetProperties(bindingFlags))
        //        {
        //            if ((!includeBaseFields || !(propertyInfo.DeclaringType != null) || !propertyInfo.DeclaringType.ToString().Contains("UnityEngine")) && propertyInfo.CanWrite)
        //            {
        //                try
        //                {
        //                    propertyInfo.SetValue(component, propertyInfo.GetValue(componentInChildren, null), null);
        //                }
        //                catch
        //                {
        //                }
        //            }
        //        }
        //        foreach (FieldInfo fieldInfo in componentInChildren.GetType().GetFields(bindingFlags))
        //        {
        //            if (!includeBaseFields || !(fieldInfo.DeclaringType != null) || !fieldInfo.DeclaringType.ToString().Contains("UnityEngine"))
        //            {
        //                fieldInfo.SetValue(component, fieldInfo.GetValue(componentInChildren));
        //            }
        //        }
        //        obj = component;
        //    }
        //    catch
        //    {
        //        obj = component;
        //    }
        //    return obj;
        //}

        private static BindingFlags defaultBindings = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
        private static List<string> dontCopyNames = new()
        {
            "m_itemData",
            "m_shared",
            "m_name",
            "m_description",
        };

        public static bool Copy<T>(T source, ref T target) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            try
            {
                foreach (PropertyInfo propertyInfo in target.GetType().GetProperties(defaultBindings))
                {
                    if ((!(propertyInfo.DeclaringType != null) || !propertyInfo.DeclaringType.ToString().Contains("UnityEngine")) && propertyInfo.CanWrite && !dontCopyNames.Contains(propertyInfo.Name))
                    {
                        try
                        {
                            propertyInfo.SetValue(target, propertyInfo.GetValue(source, null), null);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                foreach (FieldInfo fieldInfo in target.GetType().GetFields(defaultBindings))
                {
                    if (!(fieldInfo.DeclaringType != null) || !fieldInfo.DeclaringType.ToString().Contains("UnityEngine") && !dontCopyNames.Contains(fieldInfo.Name))
                    {
                        fieldInfo.SetValue(target, fieldInfo.GetValue(source));
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }



        //public static Bounds GetBounds(GameObject obj)
        //{
        //    Bounds bounds;
        //    Renderer childRender;
        //    bounds = GetRenderBounds(obj);
        //    if (bounds.extents.x == 0)
        //    {
        //        bounds = new Bounds(obj.transform.position, Vector3.zero);
        //        foreach (Transform child in obj.transform)
        //        {
        //            childRender = child.GetComponent<Renderer>();
        //            if (childRender)
        //            {
        //                bounds.Encapsulate(childRender.bounds);
        //            }
        //            else
        //            {
        //                bounds.Encapsulate(GetBounds(child.gameObject));
        //            }
        //        }
        //    }
        //    return bounds;
        //}

        //public static Bounds GetRenderBounds(GameObject obj)
        //{
        //    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        //    Renderer render = obj.GetComponent<Renderer>();
        //    if (render != null)
        //    {
        //        return render.bounds;
        //    }
        //    return bounds;
        //}

        public static Recipe GetRecipe(this ObjectDB db, Recipe recipe)
        {
            int index = db.m_recipes.IndexOf(recipe);
            if (index > -1)
                return db.m_recipes[index];

            string name = recipe.ToString();
            return db.m_recipes.FirstOrDefault(r => name.Equals(r.ToString()));
        }

        public static Recipe GetRecipe(this ObjectDB db, string name)
        {
            foreach (Recipe recipe in db.m_recipes)
            {
                if (recipe.m_item != null && name.Equals(recipe.name))
                {
                    return recipe;
                }
            }
            return null;
        }


        public static void SetResources(this Recipe recipe, string reqs)
        {
            if (recipe != null)
            {
                recipe.m_resources = reqs.Split(',').Select(r =>
                {
                    string[] parts = r.Split(':');
                    return new Piece.Requirement
                    {
                        m_resItem = PrefabManager.GetPrefab(parts[0]).GetComponent<ItemDrop>(),
                        m_amount = parts.Length > 1 && int.TryParse(parts[1], out int amount) ? amount : 1,
                        m_amountPerLevel = parts.Length > 2 && int.TryParse(parts[2], out int apl) ? apl : 0,
                        m_recover = true // defaulting to true
                    };
                }).ToArray();
            }
        }

        public static void SetResources(this Piece piece, string reqs)
        {
            if (piece != null)
            {
                piece.m_resources = reqs.Split(',').Select(r =>
                {
                    string[] parts = r.Split(':');
                    return new Piece.Requirement
                    {
                        m_resItem = PrefabManager.GetPrefab(parts[0]).GetComponent<ItemDrop>(),
                        m_amount = parts.Length > 1 && int.TryParse(parts[1], out int amount) ? amount : 1,
                        m_amountPerLevel = parts.Length > 2 && int.TryParse(parts[2], out int apl) ? apl : 0,
                        m_recover = true // defaulting to true
                    };
                }).ToArray();
            }
        }

        public static string GetReqs(this Piece piece)
        {
            if (piece != null)
            {
                return string.Join(",", piece.m_resources.Select(r =>
                    r.m_amountPerLevel > 0 ?
                        $"{r.m_resItem.gameObject.name}:{r.m_amount}:{r.m_amountPerLevel}" :
                        $"{r.m_resItem.gameObject.name}:{r.m_amount}"));
            }
            return string.Empty;
        }

        public static void SetDrops(this CharacterDrop cd, string drops)
        {
            if (cd != null && cd.m_drops != null)
            {
                cd.m_drops.Clear();
                cd.m_drops.AddRange(drops.Split(',').Select(r =>
                {
                    string[] parts = r.Split(':');
                    return new CharacterDrop.Drop // name:min:max:chance:onePer:
                    {
                        m_prefab = PrefabManager.GetPrefab(parts[0]),
                        m_amountMin = parts.Length > 1 && int.TryParse(parts[1], out int min) ? min : 1,
                        m_amountMax = parts.Length > 2 && int.TryParse(parts[2], out int max) ? max : 1,
                        m_chance = parts.Length > 3 && float.TryParse(parts[3], out float weight) ? weight : 1f,

                        m_levelMultiplier = parts.Length > 4 && int.TryParse(parts[4], out int perStar) ? (perStar == 1 ? true : false) : true,
                        m_onePerPlayer = parts.Length > 5 && int.TryParse(parts[5], out int onePer) ? (onePer == 1? true: false) : false,
                        m_dontScale = parts.Length > 6 && int.TryParse(parts[6], out int dontScale) ? (dontScale == 1 ? true : false) : false
                    };
                }));
            }
        }

        public static void SetDrops(this DropOnDestroyed dod, string drops)
        {
            if (dod != null && dod.m_dropWhenDestroyed != null)
            {
                dod.m_dropWhenDestroyed.m_drops.Clear();
                dod.m_dropWhenDestroyed.m_drops.AddRange(drops.Split(',').Select(r =>
                {
                    string[] parts = r.Split(':');
                    return new DropTable.DropData
                    {
                        m_item = PrefabManager.GetPrefab(parts[0]),
                        m_stackMin = parts.Length > 1 && int.TryParse(parts[1], out int min) ? min : 1,
                        m_stackMax = parts.Length > 2 && int.TryParse(parts[2], out int max) ? max : 1,
                        m_weight = parts.Length > 3 && float.TryParse(parts[3], out float weight) ? weight : 1f,
                        m_dontScale = false // defaulting to false
                    };
                }));
            }
        }

        public static string GetDrops(this DropOnDestroyed dod)
        {
            if (dod != null && dod.m_dropWhenDestroyed != null)
            {
                return string.Join(",", dod.m_dropWhenDestroyed.m_drops.Select(r =>
                    r.m_weight != 1f ?
                        $"{r.m_item.name}:{r.m_stackMin}:{r.m_stackMax}:{r.m_weight}" :
                        $"{r.m_item.name}:{r.m_stackMin}:{r.m_stackMax}"));
            }
            return string.Empty;
        }

        public static void SetPickable(this Pickable p, string item, string drops = null)
        {
            DropTable.DropData getDropData(string d)
            {
                string[] parts = d.Split(':');
                return new DropTable.DropData
                {
                    m_item = PrefabManager.GetPrefab(parts[0]),
                    m_stackMin = parts.Length > 1 && int.TryParse(parts[1], out int min) ? min : 1,
                    m_stackMax = parts.Length > 2 && int.TryParse(parts[2], out int max) ? max : 1,
                    m_weight = parts.Length > 3 && float.TryParse(parts[3], out float weight) ? weight : 1f,
                    m_dontScale = false // defaulting to false
                };
            }

            if (p != null)
            {
                var id = getDropData(item);
                p.m_itemPrefab = id.m_item;
                p.m_amount = id.m_stackMax;
                p.m_minAmountScaled = id.m_stackMin;

                if (!string.IsNullOrEmpty(drops)) {
                    p.m_extraDrops.m_drops.Clear();
                    p.m_extraDrops.m_drops.AddRange(drops.Split(',').Select(r => getDropData(r)));
                }
            }
        }

        //public static List<Fermenter.ItemConversion> DeserializeConversions(string convs)
        //{
        //    return convs.Split(',').Select(r =>
        //    {
        //        string[] parts = r.Split(':');
        //        return new Fermenter.ItemConversion
        //        {
        //            m_from = PrefabManager.GetPrefab(parts[0]).GetComponent<ItemDrop>(),
        //            m_to = PrefabManager.GetPrefab(parts[1]).GetComponent<ItemDrop>(),
        //            m_producedItems = parts.Length > 2 && int.TryParse(parts[2], out int count) ? count : 1,
        //        };
        //    }).ToList();
        //}

        //public static List<DropTable.DropData> DeserializeDrops(string reqs)
        //{
        //    return reqs.Split(',').Select(r =>
        //    {
        //        string[] parts = r.Split(':');
        //        return new DropTable.DropData
        //        {
        //            m_item = PrefabManager.GetPrefab(parts[0]),
        //            m_stackMin = parts.Length > 1 && int.TryParse(parts[1], out int min) ? min : 1,
        //            m_stackMax = parts.Length > 2 && int.TryParse(parts[2], out int max) ? max : 1,
        //            m_weight = parts.Length > 3 && float.TryParse(parts[2], out float weight) ? weight : 1f,
        //            m_dontScale = false // defaulting to false
        //        };
        //    }).ToList();

        //    //public GameObject m_item;
        //    //public int m_stackMin;
        //    //public int m_stackMax;
        //    //public float m_weight;
        //    //public bool m_dontScale;
        //}

        //public static string SerializeDrops(List<DropTable.DropData> reqs)
        //{
        //    return string.Join(",", reqs.Select(r =>
        //        r.m_weight != 1f ?
        //            $"{r.m_item.name}:{r.m_stackMin}:{r.m_stackMax}:{r.m_weight}" :
        //            $"{r.m_item.name}:{r.m_stackMin}:{r.m_stackMax}"));
        //}


        public static GameObject CreateClonePrefab(GameObject prefab, string newName)
        {
            //var itemDrop = prefab.GetComponent<ItemDrop>();

            //if (itemDrop == null) { return null; }

            string newKey = "$" + newName.ToLower();

            var clone = UnityEngine.Object.Instantiate<GameObject>(prefab, Main.GetRootObject().transform);
            clone.SetActive(false);
            clone.name = newName;
            var oldItemDrop = clone.GetComponent<ItemDrop>();
            if (oldItemDrop != null)
            {
                var newItemDrop = clone.AddComponent<ItemDrop>();
                UnityEngine.Object.DestroyImmediate(oldItemDrop);
                newItemDrop.m_itemData = new ItemDrop.ItemData()
                {
                    m_shared = new ItemDrop.ItemData.SharedData()
                };
                Utilities.Copy(oldItemDrop.m_itemData, ref newItemDrop.m_itemData);
                Utilities.Copy(oldItemDrop.m_itemData.m_shared, ref newItemDrop.m_itemData.m_shared);
                newItemDrop.m_itemData.m_shared.m_name = newKey;
                newItemDrop.m_itemData.m_shared.m_description = newKey + "_description";
            }

            var oldPiece = clone.GetComponent<Piece>();
            if (oldPiece != null)
            {
                //var newPiece = clone.AddComponent<Piece>();
                //UnityEngine.Object.DestroyImmediate(oldPiece);
                //newPiece.m_itemData = new ItemDrop.ItemData()
                //{
                //    m_shared = new ItemDrop.ItemData.SharedData()
                //};
                //Utilities.Copy(itemDrop.m_itemData, ref newItemDrop.m_itemData);
                //Utilities.Copy(itemDrop.m_itemData.m_shared, ref newItemDrop.m_itemData.m_shared);
                oldPiece.m_name = newKey;
                oldPiece.m_description = newKey + "_description";
            }

            clone.SetActive(true);

            return clone;
        }
    }
}
