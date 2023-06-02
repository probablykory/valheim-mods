using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreCrossbows
{
    // Local declaration of this enum.
    // Converted and cast to Jewelcrafting.GemEffects.VisualEffectCondition via reflection
    [Flags]
    public enum VisualEffectCondition : uint
    {
        IsSkill = 0xFFF,
        Swords = 1,
        Knives = 2,
        Clubs = 3,
        Polearms = 4,
        Spears = 5,
        Blocking = 6,
        Axes = 7,
        Bows = 8,
        Unarmed = 11,
        Pickaxes = 12,
        WoodCutting = 13,
        Crossbows = 14,

        IsItem = 0xFF << 12,
        Helmet = 6 << 12,
        Chest = 7 << 12,
        Legs = 11 << 12,
        Hands = 12 << 12,
        Shoulder = 17 << 12,
        Tool = 19 << 12,

        GenericExtraAttributes = 0xFFu << 24,
        Blackmetal = 1 << 30,
        TwoHanded = 1u << 31,

        SpecificExtraAttributes = 0xF << 20,
        Hammer = (1 << 20) | Tool,
        Hoe = (2 << 20) | Tool,
        Buckler = (1 << 20) | Blocking,
        Towershield = (2 << 20) | Blocking,
        FineWoodBow = (1 << 20) | Bows,
        BowHuntsman = (2 << 20) | Bows,
        BowDraugrFang = (3 << 20) | Bows,
        Arbalest = (1 << 20) | Crossbows,
        CrossbowWood = (2 << 20) | Crossbows,
        CrossbowBronze = (3 << 20) | Crossbows,
        CrossbowIron = (4 << 20) | Crossbows,
        CrossbowSilver = (5 << 20) | Crossbows,
        CrossbowBlackmetal = (6 << 20) | Crossbows,
        PickaxeIron = (1 << 20) | Pickaxes,
        Club = (1 << 20) | Clubs
    }

    public static class JewelcraftingPatches
    {
        public static void Initialize() {

            if (MoreCrossbows.Instance == null || MoreCrossbows.Instance.harmony == null || MoreCrossbows.Instance.jewelcrafting == null)
            {
                throw new ArgumentNullException("Attempted to initialize without harmony, jewelcrafting or both.");
            }

            Assembly jc = MoreCrossbows.Instance.jewelcrafting.GetType().Assembly;
            MethodInfo method = jc.GetType("Jewelcrafting.GemEffects.VisualEffects").GetMethod("prefabDict");
            MethodInfo postfix = typeof(JewelcraftingPatches).GetMethod("VisualEffectsPrefabDictPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null && postfix != null)
            {
                MoreCrossbows.Instance.harmony.Patch(method, postfix: new HarmonyMethod(postfix));
            }
            else
            {
                Jotunn.Logger.LogDebug($"Unable to patch. method = {method}, postfix = {postfix}");
            }

            List<string[]> prefabsToLoad = new List<string[]>()
            {
                new string[] { "Perfect_Yellow_Socket", VisualEffectCondition.CrossbowIron.ToString(), "jc_echo_ironxbow"},
                new string[] { "Perfect_Red_Socket", VisualEffectCondition.CrossbowIron.ToString(), "jc_endlessarrows_ironxbow"},
                new string[] { "Perfect_Purple_Socket", VisualEffectCondition.CrossbowIron.ToString(), "jc_masterarcher_ironxbow"},
                new string[] { "Perfect_Green_Socket", VisualEffectCondition.CrossbowIron.ToString(), "jc_necromancer_ironxbow"},

                new string[] { "Perfect_Yellow_Socket", VisualEffectCondition.CrossbowSilver.ToString(), "jc_echo_silverxbow"},
                new string[] { "Perfect_Red_Socket", VisualEffectCondition.CrossbowSilver.ToString(), "jc_endlessarrows_silverxbow"},
                new string[] { "Perfect_Purple_Socket", VisualEffectCondition.CrossbowSilver.ToString(), "jc_masterarcher_silverxbow"},
                new string[] { "Perfect_Green_Socket", VisualEffectCondition.CrossbowSilver.ToString(), "jc_necromancer_silverxbow"},

                new string[] { "Perfect_Yellow_Socket", VisualEffectCondition.CrossbowBlackmetal.ToString(), "jc_echo_blackmetalxbow"},
                new string[] { "Perfect_Red_Socket", VisualEffectCondition.CrossbowBlackmetal.ToString(), "jc_endlessarrows_blackmetalxbow"},
                new string[] { "Perfect_Purple_Socket", VisualEffectCondition.CrossbowBlackmetal.ToString(), "jc_masterarcher_blackmetalxbow"},
                new string[] { "Perfect_Green_Socket", VisualEffectCondition.CrossbowBlackmetal.ToString(), "jc_necromancer_blackmetalxbow"},
            };

            Type vfxType = jc?.GetType("Jewelcrafting.GemEffects.VisualEffects");
            Type vecType = jc?.GetType("Jewelcrafting.VisualEffectCondition");
            FieldInfo attachEffectPrefabInfo = vfxType?.GetField("attachEffectPrefabs", BindingFlags.Static | BindingFlags.Public);
            var attachEffectPrefabs = attachEffectPrefabInfo?.GetValue(null) as IDictionary;

            if (attachEffectPrefabs != null)
            {
                foreach (string[] entry in prefabsToLoad)
                {
                    string prefabSocketKey = entry[0];
                    string prefabName = entry[2];
                    VisualEffectCondition displayCondition = (VisualEffectCondition)Enum.Parse(typeof(VisualEffectCondition), entry[1]);


                    GameObject effect = MoreCrossbows.Instance.assetBundle.LoadAsset<GameObject>(prefabName);
                    if (effect == null)
                    {
                        Jotunn.Logger.LogWarning($"Prefab {prefabName} did not load correctly");
                    }
                    if (attachEffectPrefabs.Contains(prefabSocketKey))
                    {
                        var innerDict = attachEffectPrefabs[prefabSocketKey] as IDictionary;
                        if (innerDict != null && !innerDict.Contains(vecType.Cast(displayCondition)))
                        {
                            Jotunn.Logger.LogDebug($"Adding: {displayCondition} {effect} to attachEffectPrefabs[{prefabSocketKey}]");
                            innerDict.Add(vecType.Cast(displayCondition), effect);
                        }
                    }
                    else
                    {
                        Jotunn.Logger.LogWarning($"attachEffectPrefabs does not contain {prefabSocketKey} key; Aborting.");
                        break;
                    }
                }
            }
            else
            {
                Jotunn.Logger.LogDebug("Unable to invoke attachEffectPrefabs, not found.");
            }
        }

        // Cached reflection info
        private static FieldInfo effectPrefabsByTypeInfo = null;
        private static Type vecType = null;

        // manually patched postfix
        private static void VisualEffectsPrefabDictPostfix(ref Dictionary<string, GameObject[]> __result, ItemDrop.ItemData.SharedData shared)
        {
            if (__result != null && shared.m_skillType == Skills.SkillType.Crossbows)
            {
                VisualEffectCondition key = SkillKey(shared);
                int count = __result != null ? __result.Count : 0;

                // perform reflection nonsense only once
                if (effectPrefabsByTypeInfo == null || vecType == null)
                {
                    Assembly jc = MoreCrossbows.Instance.jewelcrafting?.GetType().Assembly;
                    Type vfxType = jc?.GetType("Jewelcrafting.GemEffects.VisualEffects");
                    vecType = jc?.GetType("Jewelcrafting.VisualEffectCondition");
                    effectPrefabsByTypeInfo = vfxType?.GetField("effectPrefabsByType", BindingFlags.Static | BindingFlags.NonPublic);
                }

                var effectPrefabsByType = effectPrefabsByTypeInfo?.GetValue(null) as IDictionary;
                if (effectPrefabsByType != null)
                {
                    Dictionary<string, GameObject[]> prefabs = effectPrefabsByType[vecType.Cast(key)] as Dictionary<string, GameObject[]>; // Dictionary<string, GameObject[]>;
                    if (prefabs != null && prefabs.Count > 0)
                    {
                        //Jotunn.Logger.LogDebug($"prefabs not null and count > 0 {prefabs.Count}");
                        Jotunn.Logger.LogDebug($"Patched visual effects lookup by skill, type = {shared.m_itemType}, key = {key}, results = {count}");
                        __result = prefabs;
                    }
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Failed to find Jewelcrafting.GemEffects.VisualEffects.effectPrefabsByType via reflection.");
                }
            }
        }

        // TODO
        //private static VisualEffectCondition ItemKey(ItemDrop.ItemData.SharedData shared) => 
        //    (VisualEffectCondition)((int)shared.m_itemType << 12)
        //    | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hammer") ? VisualEffectCondition.Hammer : 0)
        //    | (shared.m_itemType is ItemDrop.ItemData.ItemType.Tool && shared.m_name.Contains("$item_hoe") ? VisualEffectCondition.Hoe : 0);

        // Custom effects for only these crossbows
        private static VisualEffectCondition SkillKey(ItemDrop.ItemData.SharedData shared) =>
            (VisualEffectCondition)shared.m_skillType
            | (shared.m_skillType is Skills.SkillType.Crossbows && shared.m_name.Contains("$item_crossbow_iron") ? VisualEffectCondition.CrossbowIron : 0)
            | (shared.m_skillType is Skills.SkillType.Crossbows && shared.m_name.Contains("$item_crossbow_silver") ? VisualEffectCondition.CrossbowSilver : 0)
            | (shared.m_skillType is Skills.SkillType.Crossbows && shared.m_name.Contains("$item_crossbow_blackmetal") ? VisualEffectCondition.CrossbowBlackmetal: 0);
    }


}
