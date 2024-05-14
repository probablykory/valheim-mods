using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Managers
{
    // Implement this interface in your Mod to enable the logger & config functionality.
    public interface IPlugin
    {
        string GUID { get; }
        ConfigFile Config { get; }
        bool Debug { get; }
        Harmony Harmony { get; }
        ManualLogSource LogSource { get; }

        Coroutine StartCoroutine(string methodName);
        Coroutine StartCoroutine(IEnumerator routine);
        Coroutine StartCoroutine(string methodName, object value);

    }

    public static class Main
    {
        private static GameObject rootObject;
        private static IPlugin cachedModRef = null;
        static Main()
        {
            if (cachedModRef == null)
            {
                Type mod = (new StackFrame(0).GetMethod().DeclaringType.Assembly.GetTypes()).Where(p => typeof(IPlugin).IsAssignableFrom(p)).FirstOrDefault();
                cachedModRef = AccessTools.Field(mod, "Instance").GetValue(null) as IPlugin;
                isHeadless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
                Logger.LogDebugOnly($"Cached static Mod reference from {mod.Assembly.GetName()}, IsHeadless={isHeadless}");

            }
            Main.Mod.Harmony.PatchAll(typeof(Patches));
        }

        public static event Action OnFjedStartupAwake;
        public static event Action OnVanillaPrefabsAvailable;
        public static event Action OnPiecesRegistered;
        public static event Action OnLocationsSetUp;
        public static IPlugin Mod { get { return cachedModRef; } }

        public static GameObject GetRootObject()
        {
            if (rootObject)
            {
                return rootObject;
            }

            // create root container for GameObjects in the DontDestroyOnLoad scene
            rootObject = new GameObject("_ManagerRoot");
            rootObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(rootObject);
            return rootObject;
        }

        private static bool isHeadless;
        public static bool IsHeadless
        {
            get { return isHeadless; }
        }

        private static class Patches
        {
            [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake)), HarmonyPrefix, UsedImplicitly]
            private static void InvokeOnFjedStartupAwake()
            {
                OnFjedStartupAwake?.Invoke();
            }

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPrefix, UsedImplicitly]
            private static void InvokeOnVanillaPrefabsAvailable()
            {
                OnVanillaPrefabsAvailable?.Invoke();
            }

            [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix, UsedImplicitly]
            private static void InvokeOnPiecesRegistered()
            {
                if (SceneManager.GetActiveScene().name == "main")
                {
                    OnPiecesRegistered?.Invoke();
                }
            }

            [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations)), HarmonyPostfix, UsedImplicitly]
            private static void InvokeOnLocationsSetUp()
            {
                OnLocationsSetUp?.Invoke();
            }
        }
    }
}
