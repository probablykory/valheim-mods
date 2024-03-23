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
    public static class Aquatic
    {
        static Aquatic()
        {
            JcAPI.AddOnEffectRecalc(() =>
            {
                if (!JewelryManager.IsEffectItemEquipped(Player.m_localPlayer, Effects.Aquatic))
                {
                    SE_Stats aquatic = JcAPI.GetGemEffect<SE_Stats>(Effects.Aquatic);
                    if (aquatic != null)
                    {
                        Player.m_localPlayer.m_seman.RemoveStatusEffect(aquatic);
                    }
                }
            });
        }

        [HarmonyPatch(typeof(SE_Wet), nameof(SE_Wet.UpdateStatusEffect))]
        private static class ApplyAquatic
        {
            private static void Postfix(SE_Wet __instance)
            {
                if (__instance.m_character != Player.m_localPlayer || !JewelryManager.IsEffectItemEquipped(Player.m_localPlayer, Effects.Aquatic))
                {
                    return;
                }
                SE_Stats sesAquatic = JcAPI.GetGemEffect<SE_Stats>(Effects.Aquatic);
                if (sesAquatic != null)
                {
                    StatusEffect aquatic = Player.m_localPlayer.GetSEMan().GetStatusEffect(sesAquatic.name.GetStableHashCode());
                    if (aquatic == null)
                    {
                        var itemData = JewelryManager.GetEquippedItemByEffect(Player.m_localPlayer, Effects.Aquatic);
                        if (itemData != null)
                        {
                            sesAquatic.m_icon = itemData.GetIcon();
                        }
                        aquatic = Player.m_localPlayer.GetSEMan().AddStatusEffect(sesAquatic);
                    }
                    aquatic.m_ttl = __instance.m_ttl;
                    aquatic.m_time = __instance.m_time;
                }
            }
        }
    }
}