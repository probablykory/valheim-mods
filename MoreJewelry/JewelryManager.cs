using ItemManager;
using Jewelcrafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Managers.Logger;

namespace MoreJewelry
{
    public enum JewelryKind
    {
        None,
        DefaultNecklace,
        CustomNecklace,
        LeatherNecklace,
        SilverNecklace,

        DefaultRing,
        CustomRing,
        StoneRing,
        BoneRing,
        SilverRing
    }

    // This mostly attempts to decouple various effects from specific items of Jewelry, thus tracks which effects are used where.
    public static class JewelryManager
    {
        public static readonly Dictionary<string, StatusEffect> AvailableEffects = new(StringComparer.InvariantCultureIgnoreCase);
        public static readonly Dictionary<string, List<string>> EffectItemMap = new(StringComparer.InvariantCultureIgnoreCase);

        public static readonly Dictionary<JewelryKind, GameObject> AvailablePrefabs = new();

        private static bool initialized = false;
        public static bool IsInitialized() { return initialized; }
        public static void Initialize()
        {
            if (initialized) return;

            List<string> originalJewelryNames = new List<string>()
                {
                    "JC_Necklace_Red",
                    "JC_Necklace_Green",
                    //"JC_Necklace_Blue", // intentionally omitted, aquatic 
                    "JC_Necklace_Yellow",
                    "JC_Necklace_Purple",
                    "JC_Ring_Purple",
                    // "JC_Ring_Green", // intentionally omitted, headhunter
                    "JC_Ring_Red",
                    "JC_Ring_Blue",
                    "JC_Ring_Black"
                };

            StatusEffect fetchEffectFor(string prefabName)
            {
                var shared = PrefabManager.GetPrefab(prefabName)?.GetComponent<ItemDrop>()?.m_itemData.m_shared;
                if (shared != null)
                {
                    return shared.m_equipStatusEffect;
                }
                return null!;
            }

            StatusEffect effect;
            string effectName;
            foreach (string jcName in originalJewelryNames)
            {
                effectName = Effects.GetEffectNameFromJcName(jcName);
                if (!AvailableEffects.TryGetValue(effectName, out effect))
                {
                    effect = fetchEffectFor(jcName);
                    if (effect != null)
                    {
                        AvailableEffects.Add(effectName, effect);
                        AddItemToEffectItemMap(jcName, effectName);
                        Logger.LogDebugOnly($"Found {jcName}, extracted {effectName}");
                    }
                    else
                    {
                        Logger.LogDebug($"Could not find {jcName} prefab for effect extraction.");
                    }
                }
            }

            AvailableEffects.Add(Effects.Perception, ScriptableObject.CreateInstance<Perception>());
            AddItemToEffectItemMap("JC_Necklace_Blue", Effects.Aquatic);
            AddItemToEffectItemMap("JC_Ring_Green", Effects.Headhunter);

            initialized = true;
        }

        public static bool IsEffectItemEquipped(Player player, string effectName)
        {
            if (!initialized) return false;

            bool result = false;
            if (EffectItemMap.TryGetValue(effectName, out var itemList) && itemList != null && itemList.Count > 0)
            {
                foreach (var item in itemList)
                {
                    result = result || JcAPI.IsJewelryEquipped(player, item);
                }
            }
            return result;
        }

        public static ItemDrop.ItemData GetEquippedItemByEffect(Player player, string effectName)
        {
            if (!initialized) return null;

            ItemDrop.ItemData item = null;
            if (EffectItemMap.TryGetValue(effectName, out var itemList) && itemList != null && itemList.Count > 0)
            {
                item = JcAPI.GetEquippedFingerItem(player);
                if (item is not null && itemList.Contains(item.m_dropPrefab?.name))
                {
                    return item;
                }
                else
                {
                    item = JcAPI.GetEquippedNeckItem(player);
                    if (item is not null && itemList.Contains(item.m_dropPrefab?.name))
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        public static bool IsItemMappedToEffect(GameObject prefab, string effectName)
        {
            if (!initialized) return false;

            if (EffectItemMap.TryGetValue(effectName, out List<string> itemList) && itemList == null)
            {
                return itemList.Contains(prefab.name);
            }

            return false;
        }

        private static void AddItemToEffectItemMap(string prefabName, string effectName)
        {
            if (!initialized) return;

            List<string> itemList;
            if (!EffectItemMap.TryGetValue(effectName, out itemList) || itemList == null)
            {
                itemList = new List<string>();
                EffectItemMap.Add(effectName, itemList);
            }
            if (!itemList.Contains(prefabName))
                itemList.Add(prefabName);
        }

        public static void RemoveItemFromEffectItemMap(string prefabName, string effectName)
        {
            if (!initialized) return;

            if (EffectItemMap.TryGetValue(effectName, out List<string> itemList) && itemList != null)
            {
                if (itemList.Contains(prefabName))
                    itemList.Remove(prefabName);

                if (itemList.Count == 0)
                    EffectItemMap.Remove(effectName);
            }
        }

        public static void AddEffectToItem(Item item, string effectName)
        {
            if (!initialized) return;

            AddItemToEffectItemMap(item.Prefab.name, effectName);
            if (Effects.Aquatic.Equals(effectName) || Effects.Headhunter.Equals(effectName))
            {
                var ses = JcAPI.GetGemEffect<SE_Stats>(effectName);
                if (ses is not null)
                {
                    // These effects always added into the player, so amend the item description with the SE's tooltip
                    item.Description.English(item.Description.English() +
                        "\n\n<color=orange>" + Localization.instance.Localize(ses.m_name) + "</color>\n" +
                        Localization.instance.Localize(ses.GetTooltipString()).Replace(".", "").Replace("\n", " ") +
                        Localization.instance.Localize(ses.m_name + "_effect"));
                }

                return;
            }

            if (AvailableEffects.TryGetValue(effectName, out StatusEffect se) && se != null)
            {
                item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = se;
            }
        }

        public static void ClearAllEffectsFromItem(Item item)
        {
            if (!initialized) return;

            item.Prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_equipStatusEffect = null;

            foreach (var effectName in EffectItemMap.Keys.ToList())
            {
                RemoveItemFromEffectItemMap(item.Prefab.name, effectName);
            }
        }
    }
}
