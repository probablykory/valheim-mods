using HarmonyLib;
using Jewelcrafting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreJewelry
{
    // Local version of this effect, decoupling it from a single-item use.
    public static class Headhunter
    {
        static Headhunter()
        {
            JcAPI.AddOnEffectRecalc(() =>
            {
                if (!JewelryManager.IsEffectItemEquipped(Player.m_localPlayer, Effects.Headhunter))
                {
                    SE_Stats headhunter = JcAPI.GetGemEffect<SE_Stats>(Effects.Headhunter);
                    if (headhunter != null)
                    {
                        Player.m_localPlayer.m_seman.RemoveStatusEffect(headhunter);
                    }
                }
            });
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class ApplyHeadHunter
        {
            private static void Postfix(Character __instance, HitData hit)
            {
                if (__instance.IsBoss() && hit.GetAttacker() is Player player && JewelryManager.IsEffectItemEquipped(player, Effects.Headhunter))
                {
                    if (__instance.m_nview.GetZDO().GetBool($"Jewelcrafting HeadHunter {player.GetPlayerID()}"))
                    {
                        return;
                    }

                    SE_Stats headhunter = JcAPI.GetGemEffect<SE_Stats>(Effects.Headhunter);
                    if (headhunter != null)
                    {
                        var itemData = JewelryManager.GetEquippedItemByEffect(Player.m_localPlayer, Effects.Headhunter);
                        if (itemData != null)
                        {
                            headhunter.m_icon = itemData.GetIcon();
                        }
                        player.m_seman.AddStatusEffect(headhunter, true);
                        __instance.m_nview.GetZDO().Set($"Jewelcrafting HeadHunter {player.GetPlayerID()}", true);
                    }
                }
            }
        }
    }
}