using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    public static class CmAPI
    {
        public const string DependencyString = "com.bepis.bepinex.configurationmanager";

        public static Texture2D EntryBackground
        {
            get
            {
                var tex = GetField<Texture2D>("<EntryBackground>k__BackingField");
                if (tex != null)
                    return tex;
                else
                    return GUI.skin.box.normal.background;                
            }
        }
        public static Texture2D WidgetBackground
        {
            get
            {
                var tex = GetField<Texture2D>("<WidgetBackground>k__BackingField");
                if (tex != null)
                    return tex;
                else
                    return GUI.skin.box.normal.background;

            }
        }
        public static Texture2D WindowBackground { get { return GetField<Texture2D>("<WindowBackground>k__BackingField"); } }

        public static int fontSize { get { return GetField<int>("fontSize"); } }

        private static Color __entryBackgroundColor;
        public static Color _entryBackgroundColor
        {
            get
            {
                __entryBackgroundColor = Color.black;
                ConfigEntry<Color> entry = GetField<ConfigEntry<Color>>("_entryBackgroundColor");
                if (entry != null)
                    __entryBackgroundColor = entry.Value;
                else if (instance != null)
                    __entryBackgroundColor = GUI.backgroundColor;
                return __entryBackgroundColor;
            }
        }

        private static Color __fontColor;
        public static Color _fontColor
        {
            get
            {
                __fontColor = Color.white;
                ConfigEntry<Color> entry = GetField<ConfigEntry<Color>>("_fontColor");
                if (entry != null)
                    __fontColor = entry.Value;
                else if (instance != null)
                    __fontColor = GUI.color;
                return __fontColor;
            }
        }

        //_fontColor
        private static Color __widgetBackgroundColor;
        public static Color _widgetBackgroundColor
        {
            get
            {
                __widgetBackgroundColor = Color.black;
                ConfigEntry<Color> entry = GetField<ConfigEntry<Color>>("_widgetBackgroundColor");
                if (entry != null)
                    __widgetBackgroundColor = entry.Value;
                else if (instance != null)
                    __widgetBackgroundColor = GUI.backgroundColor;
                return __widgetBackgroundColor;
            }
        }

        private static Color __windowBackgroundColor;
        public static Color _windowBackgroundColor
        {
            get
            {
                __windowBackgroundColor = Color.black;
                ConfigEntry<Color> entry = GetField<ConfigEntry<Color>>("_windowBackgroundColor");
                if (entry != null)
                    __windowBackgroundColor = entry.Value;
                else if (instance != null)
                    __windowBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                return __windowBackgroundColor;
            }
        }

        public static bool DisplayingWindow { get { return GetField<bool>("_displayingWindow"); } }
        public static int LeftColumnWidth { get { return GetField<int>("<LeftColumnWidth>k__BackingField"); } }
        public static int RightColumnWidth { get { return GetField<int>("<RightColumnWidth>k__BackingField"); } }


        public static event EventHandler<EventArgs> OnDisplayingWindowChanged;
        private static Type cmType = null;
        private static BaseUnityPlugin instance = null;

        public static bool IsLoaded()
        {
            var cm = GetConfigManager();
            return cm != null;
        }

        // initializer
        private static BaseUnityPlugin GetConfigManager()
        {
            if (instance is null)
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue(DependencyString, out configManagerInfo) && configManagerInfo.Instance)
                {
                    instance = configManagerInfo.Instance;
                    cmType = instance.GetType().Assembly.GetType("ConfigurationManager.ConfigurationManager");

                    Logger.LogDebugOnly("Configuration manager found, hooking DisplayingWindowChanged");
                    EventInfo eventinfo = cmType.GetEvent("DisplayingWindowChanged");
                    if (eventinfo != null)
                    {
                        Action<object, object> local = new Action<object, object>(OnConfigManagerDisplayingWindowChanged);
                        Delegate converted = Delegate.CreateDelegate(eventinfo.EventHandlerType, local.Target, local.Method);
                        eventinfo.AddEventHandler(instance, converted);
                    }

                }
            }

            return instance;
        }

        private static void OnConfigManagerDisplayingWindowChanged(object sender, object e)
        {
            OnDisplayingWindowChanged?.Invoke(sender, e as EventArgs);
        }

        // TODO - profile to see if a cache table may be required here
        private static T GetField<T>(string fieldName)
        {
            var cm = GetConfigManager();
            if (cmType is null) return default(T);

            if (!AccessTools.GetFieldNames(cmType).Contains(fieldName))
                return default(T);

            object result = AccessTools.Field(cmType, fieldName).GetValue(cm);
            if (result is T)
            {
                return (T)result;
            }
            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (InvalidCastException)
            {
                return default(T);
            }
        }

        public static void ReloadConfigDisplay()
        {
            if (instance is not null && DisplayingWindow is true)
            {
                cmType.GetMethod("BuildSettingList").Invoke(instance, Array.Empty<object>());
            }
        }

    }
}
