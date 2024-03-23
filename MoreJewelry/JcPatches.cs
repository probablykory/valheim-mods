using HarmonyLib;
using JetBrains.Annotations;
using Jewelcrafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Logger = Managers.Logger;

namespace MoreJewelry
{
    // Local declaration of this enum.
    [Flags]
    public enum GemLocation : ulong
    {
        Head = 1 << 0,
        Cloak = 1 << 1,
        Legs = 1 << 2,
        Chest = 1 << 3,
        Sword = 1 << 5,
        Knife = 1 << 6,
        Club = 1 << 7,
        Polearm = 1 << 8,
        Spear = 1 << 9,
        Axe = 1 << 10,
        Bow = 1 << 11,
        Crossbow = 1 << 12,
        Weapon = 1 << 13,
        ElementalMagic = 1 << 14,
        BloodMagic = 1 << 15,
        Magic = 1 << 16,
        Tool = 1 << 17,
        Shield = 1 << 18,
        Utility = 1 << 19,
        All = 1 << 20,
    }

    public static class JcPatches
    {
        public static void DoPatches(Harmony harmony)
        {
            Assembly jc = typeof(API.GemInfo).Assembly;
            MethodInfo target;
            MethodInfo method;

            target = jc.GetType("Jewelcrafting.Utils").GetMethod("GetGemLocation");
            method = typeof(JcPatches).GetMethod(nameof(JcPatches.GetGemLocationPrefix), BindingFlags.Static | BindingFlags.NonPublic);
            if (target != null && method != null)
                harmony.Patch(target, prefix: new HarmonyMethod(method));
            else
                Logger.LogError($"Unable to patch Jewelcrafting.Utils.GetGemLocation:  target = {target}, method = {method}");

            target = jc.GetType("Jewelcrafting.GemEffects.Warmth+PreventColdNights").GetMethod("RemoveColdInColdNights", BindingFlags.Static | BindingFlags.NonPublic);
            method = typeof(JcPatches).GetMethod(nameof(JcPatches.RemoveColdInColdNightsPrefix), BindingFlags.Static | BindingFlags.NonPublic);
            if (target != null && method != null)
                harmony.Patch(target, prefix: new HarmonyMethod(method));
            else
                Logger.LogError($"Unable to patch Jewelcrafting.GemEffects.Warmth+PreventColdNights.RemoveColdInColdNights: target = {target}, method = {method}");

            target = jc.GetType("Jewelcrafting.Visual+EquipItem").GetMethod("Equip", BindingFlags.Static | BindingFlags.NonPublic);
            method = typeof(JcPatches).GetMethod(nameof(JcPatches.OnJCEquipPostfix), BindingFlags.Static | BindingFlags.NonPublic);
            if (target != null && method != null)
                harmony.Patch(target, postfix: new HarmonyMethod(method));
            else
                Logger.LogError($"Unable to patch Jewelcrafting.Visual+EquipItem.Equip: target = {target}, method = {method}");
        }

        // Patch for Lumberjack effect
        [UsedImplicitly]
        private static bool GetGemLocationPrefix(ItemDrop.ItemData.SharedData item, Player? player, ref object __result)
        {
            GemLocation location = item.m_itemType switch
            {
                ItemDrop.ItemData.ItemType.Helmet => GemLocation.Head,
                ItemDrop.ItemData.ItemType.Chest => GemLocation.Chest,
                ItemDrop.ItemData.ItemType.Legs => GemLocation.Legs,
                ItemDrop.ItemData.ItemType.Utility => GemLocation.Utility,
                ItemDrop.ItemData.ItemType.Shoulder => GemLocation.Cloak,
                ItemDrop.ItemData.ItemType.Tool => GemLocation.Tool,
                _ => item.m_skillType switch
                {
                    Skills.SkillType.Swords => GemLocation.Sword,
                    Skills.SkillType.Knives => GemLocation.Knife,
                    Skills.SkillType.Clubs => GemLocation.Club,
                    Skills.SkillType.Polearms => GemLocation.Polearm,
                    Skills.SkillType.Spears => GemLocation.Spear,
                    Skills.SkillType.Blocking => GemLocation.Shield,
                    Skills.SkillType.Axes => ((player != null && JewelryManager.IsEffectItemEquipped(player, Effects.Lumberjack)) ? GemLocation.Tool : GemLocation.Axe),
                    Skills.SkillType.Bows => GemLocation.Bow,
                    Skills.SkillType.Crossbows => GemLocation.Crossbow,
                    Skills.SkillType.Pickaxes => GemLocation.Tool,
                    Skills.SkillType.Unarmed when item.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon => GemLocation.Knife,
                    Skills.SkillType.BloodMagic => GemLocation.BloodMagic,
                    Skills.SkillType.ElementalMagic => GemLocation.ElementalMagic,
                    _ => GemLocation.Sword,
                },
            };

            __result = location;

            return false;
        }

        // Patch for Warmth effect
        [UsedImplicitly]
        private static bool RemoveColdInColdNightsPrefix(bool cold, Player player, ref bool __result)
        {
            if (cold && EnvMan.instance.GetCurrentEnvironment().m_isColdAtNight && JewelryManager.IsEffectItemEquipped(player, Effects.Warmth))
            {
                cold = false;
            }
            __result = cold;

            return false;
        }

        // Postfix for item equips
        [UsedImplicitly]
        private static void OnJCEquipPostfix(Humanoid humanoid, ItemDrop.ItemData item, ref bool __result)
        {
            if (__result) // JC's patch for Humanoid.EquipItem returned true -> we've equipped a ring/neck
            {
                if (item.m_shared?.m_equipStatusEffect != null && JewelryManager.AvailableEffects.ContainsValue(item.m_shared.m_equipStatusEffect))
                {
                    item.m_shared.m_equipStatusEffect.m_icon = item.GetIcon();
                }
            }
        }
    }
}


