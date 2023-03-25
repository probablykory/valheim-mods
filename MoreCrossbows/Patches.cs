using HarmonyLib;
using System;

namespace MoreCrossbows
{
    [HarmonyPatch]
    public static class Patches
    {
        public static event Action<Player> OnPlayerSpawned;

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        private static void PlayerOnSpawn(Player __instance)
        {
            OnPlayerSpawned?.Invoke(__instance);
        }
    }
}
