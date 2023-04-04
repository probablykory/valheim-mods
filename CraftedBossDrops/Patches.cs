using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftedBossDrops
{
    [HarmonyPatch]
    public static class TraderPatch
    {
        private static Dictionary<string, string> GlobalKeyTokenMap = new Dictionary<string, string>
        {
            { "defeated_eikthyr", "$item_hardantler" },
            { "defeated_gdking", "$item_cryptkey" },
            { "defeated_bonemass", "$item_wishbone" },
            { "defeated_dragon", "$item_dragontear" },
            { "defeated_goblinking", "$item_yagluththing" },
            { "defeated_queen", "$item_queen_drop" }
        };

        [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems)), HarmonyPostfix]
        public static List<Trader.TradeItem> TraderGetAvailableItems(List<Trader.TradeItem> values, Trader __instance)
        {
            List<Trader.TradeItem> list = new List<Trader.TradeItem>();
            foreach (Trader.TradeItem tradeItem in __instance.m_items)
            {
                if (string.IsNullOrEmpty(tradeItem.m_requiredGlobalKey) ||
                    ZoneSystem.instance.GetGlobalKey(tradeItem.m_requiredGlobalKey) ||
                    Player.m_localPlayer.IsKnownMaterial(GlobalKeyTokenMap[tradeItem.m_requiredGlobalKey]))
                {
                    list.Add(tradeItem);
                }
            }
            return list;
        }
    }
}


