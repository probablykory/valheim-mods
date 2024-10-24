﻿using BepInEx.Bootstrap;
using BepInEx;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using JetBrains.Annotations;

namespace Managers
{
    public static class VneiAPI
    {
        public const string DependencyString = "com.maxsch.valheim.vnei";

        public static event Action AfterDisableItems;
        private static Type vneiType = null;
        private static Type indexingType = null;
        private static MethodInfo disableItemMethod = null;
        private static BaseUnityPlugin instance = null;

        public static bool IsLoaded()
        {
            var isPresent = Chainloader.PluginInfos.ContainsKey(DependencyString);
            if (isPresent)
            {
                Initialize();
                return instance is not null;
            }
            return isPresent;
        }

        public static void DisableItem(string name, string context)
        {
            if (instance == null) return;
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(context)) throw new ArgumentNullException(nameof(context));

            disableItemMethod.Invoke(null, new object[] {name, context});
        }

        private static void Initialize()
        {
            if (instance is null)
            {
                PluginInfo vneiInfo;
                if (Chainloader.PluginInfos.TryGetValue(DependencyString, out vneiInfo) && vneiInfo.Instance)
                {
                    instance = vneiInfo.Instance;
                    vneiType = instance.GetType().Assembly.GetType("VNEI.Plugin");
                    indexingType = vneiType.Assembly.GetType("VNEI.Logic.Indexing");
                    disableItemMethod = indexingType.GetMethod("DisableItem", BindingFlags.Static | BindingFlags.Public);

                    Logger.LogDebugOnly("VNEI found, hooking AfterDisableItems");
                    EventInfo eventinfo = indexingType.GetEvent("AfterDisableItems");
                    if (eventinfo != null)
                    {
                        Action local = new Action(OnVneiAfterDisableItems);
                        Delegate converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);
                        eventinfo.AddEventHandler(instance, converted);
                    }
                }
            }
        }

        private static void OnVneiAfterDisableItems()
        {
            AfterDisableItems?.Invoke();
        }
    }
}
