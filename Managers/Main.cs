using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Managers
{
    // Implement this interface in your Mod to enable the logger & config functionality.
    public interface IPlugin
    {
        ConfigFile Config { get; }
        bool Debug { get; }
        Harmony Harmony { get; }
        ManualLogSource LogSource { get; }
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
        }

        public static IPlugin Mod { get { return cachedModRef; } }

        public static GameObject GetRootObject()
        {
            if (rootObject)
            {
                return rootObject;
            }

            // create root container for GameObjects in the DontDestroyOnLoad scene
            rootObject = new GameObject("_ManagerRoot");
            UnityEngine.Object.DontDestroyOnLoad(rootObject);
            return rootObject;
        }

        private static bool isHeadless;
        public static bool IsHeadless
        {
            get { return isHeadless; }
        }
    }
}
