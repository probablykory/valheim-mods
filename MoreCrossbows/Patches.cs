using HarmonyLib;
using System;
using System.CodeDom;
using System.Reflection;
using System.Text;
using UnityEngine;
using static Skills;

namespace MoreCrossbows
{
    [HarmonyPatch]
    public static class PlayerPatches
    {
        public static event Action<Player> OnPlayerSpawned;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        private static void PlayerOnSpawn(Player __instance)
        {
            OnPlayerSpawned?.Invoke(__instance);
        }
    }

    public static class TooltipHelper
    {
        public static string Damage(float damage)
        {
            return string.Concat(new string[]
            {
                "<color=orange>", Mathf.RoundToInt(damage).ToString(), "</color>"
            });
        }

        public static string GetAoeTooltipForItem(ItemDrop.ItemData item, Aoe aoe)
        {
            StringBuilder stringBuilder = new StringBuilder(256);
            stringBuilder.Append( Environment.NewLine + Environment.NewLine + "$area_of_effect ");

            int intInterval = Mathf.RoundToInt(aoe.m_hitInterval);

            if (intInterval * 2f < aoe.m_ttl) // if aoe only ticks once, skip interval
            {
                if (intInterval == 1)
                {
                    stringBuilder.Append("($per_second) ");
                }
                else if (intInterval >= 2)
                {
                    // TODO: remake this into a format call and stick the format into localization
                    stringBuilder.Append("($every " + intInterval.ToString() + " $seconds) ");
                }
            }

            if (aoe.m_damage.m_damage != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_damage: " + Damage(aoe.m_damage.m_damage));
            }
            if (aoe.m_damage.m_blunt != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_blunt: " + Damage(aoe.m_damage.m_blunt));
            }
            if (aoe.m_damage.m_slash != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_slash: " + Damage(aoe.m_damage.m_slash));
            }
            if (aoe.m_damage.m_pierce != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_pierce: " + Damage(aoe.m_damage.m_pierce));
            }
            if (aoe.m_damage.m_fire != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_fire: " + Damage(aoe.m_damage.m_fire));
            }
            if (aoe.m_damage.m_frost != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_frost: " + Damage(aoe.m_damage.m_frost));
            }
            if (aoe.m_damage.m_lightning != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_lightning: " + Damage(aoe.m_damage.m_lightning));
            }
            if (aoe.m_damage.m_poison != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_poison: " + Damage(aoe.m_damage.m_poison));
            }
            if (aoe.m_damage.m_spirit != 0f)
            {
                stringBuilder.Append(Environment.NewLine + "$inventory_spirit: " + Damage(aoe.m_damage.m_spirit));
            }

            return stringBuilder.ToString();
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
    public static class GetTooltipPatch
    {
        public static string Postfix(string __result, ItemDrop.ItemData item)
        {
            StringBuilder stringBuilder = new StringBuilder(256);
            stringBuilder.Append(__result);

            // If and only if this item has a projectile component which in turn has spawnOnHit aoe component,
            // reach down to the aoe grandchild to render its aoe stats.
            if (item.m_shared.m_attack.m_attackProjectile)
            {
                Projectile p = item.m_shared.m_attack.m_attackProjectile.GetComponent<Projectile>();
                if (p != null && p.m_spawnOnHit)
                {
                    Aoe aoe = p.m_spawnOnHit.GetComponent<Aoe>();

                    if (aoe != null)
                    {
                        if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon ||   //bombs
                            item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)                //bolts
                        {
                            stringBuilder.Append(TooltipHelper.GetAoeTooltipForItem(item, aoe));
                        }
                    }
                }
            }

            return stringBuilder.ToString();
        }
    }
}
