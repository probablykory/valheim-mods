using HarmonyLib;
using Jewelcrafting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreJewelry
{
    public enum GemStyle
    {
        None,
        Default,
        Color
    }

    public static class JcAPI
    {
        public static bool IsLoaded() => API.IsLoaded();
        public static GameObject GetGemcuttersTable() => API.GetGemcuttersTable();

        public static void AddOnEffectRecalc(Action action)
        {
            if (action != null)
            {
                API.OnEffectRecalc += action;
            }
        }

        public static void RemoveOnEffectRecalc(Action action)
        {
            if (action != null)
            {
                API.OnEffectRecalc -= action;
            }
        }

        public static T GetGemEffect<T>(string name) where T : UnityEngine.Object
        {
            return AccessTools.Field(typeof(API.GemInfo).Assembly.GetType("Jewelcrafting.GemEffectSetup"), name)?.GetValue(null)! as T;
        }

        public static bool IsJewelryEquipped(Player player, string prefabName) => API.IsJewelryEquipped(player, prefabName);

        public static GameObject CreateNecklaceFromTemplate(string colorName, Color color) => API.CreateNecklaceFromTemplate(colorName, color);
        public static GameObject CreateNecklaceFromTemplate(string colorName, Material material) => API.CreateNecklaceFromTemplate(colorName, material);
        public static GameObject CreateRingFromTemplate(string colorName, Color color) => API.CreateRingFromTemplate(colorName, color);
        public static GameObject CreateRingFromTemplate(string colorName, Material material) => API.CreateRingFromTemplate(colorName, material);

        public static GameObject CreateItemFromTemplate(GameObject template, string prefabName, string localizationName, Color color)
        {
            return CreateItemFromTemplate(template, prefabName, localizationName, GemStyle.Color, color);
        }

        public static GameObject CreateItemFromTemplate(GameObject template, string prefabName, string localizationName, GemStyle style = GemStyle.Default, Color? color = null)
        {
            prefabName = $"JC_{prefabName.Replace(" ", "_")}";
            localizationName = localizationName.Replace(" ", "_");

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(template, MoreJewelry.Instance.Container.transform);
            gameObject.name = prefabName;
            ItemDrop.ItemData.SharedData shared = gameObject.GetComponent<ItemDrop>().m_itemData.m_shared;
            shared.m_name = "$" + localizationName;
            shared.m_description = "$" + localizationName + "_description";

            SetItemStyle(gameObject, style, color);

            API.MarkJewelry(gameObject);
            return gameObject;
        }

        public static void SetItemStyle(GameObject gameObject, GemStyle style, Color? color)
        {
            if (style == GemStyle.None)
            {
                gameObject.transform.Find("attach/Custom_Color_Mesh").gameObject.SetActive(false);
            }
            else if (style == GemStyle.Default)
            {
                gameObject.transform.Find("attach/Custom_Color_Mesh").gameObject.SetActive(true);
            }
            else if (style == GemStyle.Color && color.HasValue)
            {
                MeshRenderer component = gameObject.transform.Find("attach/Custom_Color_Mesh").GetComponent<MeshRenderer>();
                int? shaderColorKey = AccessTools.Field(typeof(API.GemInfo).Assembly.GetType("Jewelcrafting.GemStoneSetup"), "ShaderColorKey")?.GetValue(null) as int?;
                int? emissionColorKey = AccessTools.Field(typeof(API.GemInfo).Assembly.GetType("Jewelcrafting.GemStoneSetup"), "EmissionColor")?.GetValue(null) as int?;
                if (shaderColorKey.HasValue && emissionColorKey.HasValue)
                {
                    component.material.SetColor(shaderColorKey.Value, color.Value);
                    component.material.SetColor(emissionColorKey.Value, color.Value);
                }
            }

            Icons.SnapshotItem(gameObject.GetComponent<ItemDrop>());
        }

        private static Type visualType = null;
        private static IDictionary visualsDict = null;
        private static object GetFieldFromVisualsDictByName(Player player, string fieldname)
        {
            object result = null;
            if (visualsDict == null)
            {
                visualType = typeof(API.GemInfo).Assembly.GetType("Jewelcrafting.Visual");
                visualsDict = AccessTools.Field(visualType, "visuals").GetValue(null) as IDictionary;
            }

            if (visualsDict.Contains(player.m_visEquipment))
            {
                object vis = visualsDict[player.m_visEquipment];
                result = AccessTools.Field(visualType, fieldname).GetValue(vis);
            }
            return result;
        }

        public static ItemDrop.ItemData GetEquippedFingerItem(Player player)
        {
            return GetFieldFromVisualsDictByName(player, "equippedFingerItem") as ItemDrop.ItemData;
        }

        public static ItemDrop.ItemData GetEquippedNeckItem(Player player)
        {
            return GetFieldFromVisualsDictByName(player, "equippedNeckItem") as ItemDrop.ItemData;
        }
    }
}
