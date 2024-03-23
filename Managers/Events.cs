using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemManager;

namespace Managers
{
    public static class Events
    {
        public static event Action OnVanillaPrefabsAvailable;

        static Events()
        {
            Main.Mod.Harmony.PatchAll(typeof(Patches));
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB"), HarmonyPrefix, UsedImplicitly]
            private static void InvokeOnVanillaPrefabsAvailable()
            {
                OnVanillaPrefabsAvailable?.Invoke();
            }
        }
    }
}
