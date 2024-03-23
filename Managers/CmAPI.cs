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
    public class ConfigManagerStyle
    {
        public Color entryBgColor;
        public Color fontBgColor;
        public Color widgetBgColor;
        public Color windowBgColor;

        public Texture2D entryBgTexture = null!;
        public Texture2D widgetBgTexture = null!;
        public Texture2D windowBgTexture = null!;
    }

    public static class CmAPI
    {
        public static event EventHandler<EventArgs> OnDisplayingWindowChanged;

        private static Type cmType = null;
        private static BaseUnityPlugin instance = null;

        public static bool IsLoaded()
        {
            var cm = GetConfigManager();
            return cm != null;
        }

        public static Texture2D EntryBackground
        {
            get
            {
                var tex = GetConfigManagerField<Texture2D>("<EntryBackground>k__BackingField");
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
                var tex = GetConfigManagerField<Texture2D>("<WidgetBackground>k__BackingField");
                if (tex != null)
                    return tex;
                else
                    return GUI.skin.box.normal.background;

            }
        }
        public static Texture2D WindowBackground { get { return GetConfigManagerField<Texture2D>("<WindowBackground>k__BackingField"); } }

        public static int fontSize { get { return GetConfigManagerField<int>("fontSize"); } }

        private static Color __entryBackgroundColor;
        public static Color _entryBackgroundColor
        {
            get
            {
                __entryBackgroundColor = Color.black;
                ConfigEntry<Color> entry = GetConfigManagerField<ConfigEntry<Color>>("_entryBackgroundColor");
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
                ConfigEntry<Color> entry = GetConfigManagerField<ConfigEntry<Color>>("_fontColor");
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
                ConfigEntry<Color> entry = GetConfigManagerField<ConfigEntry<Color>>("_widgetBackgroundColor");
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
                ConfigEntry<Color> entry = GetConfigManagerField<ConfigEntry<Color>>("_windowBackgroundColor");
                if (entry != null)
                    __windowBackgroundColor = entry.Value;
                else if (instance != null)
                    __windowBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                return __windowBackgroundColor;
            }
        }

        public static bool DisplayingWindow { get { return GetConfigManagerField<bool>("_displayingWindow"); } }

        public static int LeftColumnWidth { get { return GetConfigManagerField<int>("<LeftColumnWidth>k__BackingField"); } }
        public static int RightColumnWidth { get { return GetConfigManagerField<int>("<RightColumnWidth>k__BackingField"); } }

        // initializer
        private static BaseUnityPlugin GetConfigManager()
        {
            if (instance is null)
            {
                PluginInfo configManagerInfo;
                if (Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out configManagerInfo) && configManagerInfo.Instance)
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
        private static T GetConfigManagerField<T>(string fieldName)
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

        public static ConfigManagerStyle GetConfigManagerStyle()
        {
            if (instance is null) return null;

            ConfigManagerStyle result = null;

            var tex = GetConfigManagerField<Texture2D>("<EntryBackground>k__BackingField");
            if (tex is not null)
                result = new ConfigManagerStyle() { entryBgTexture = tex };
            tex = GetConfigManagerField<Texture2D>("<WidgetBackground>k__BackingField");
            if (result is not null && tex is not null)
                result.widgetBgTexture = tex;
            tex = GetConfigManagerField<Texture2D>("<WindowBackground>k__BackingField");
            if (result is not null && tex is not null)
                result.windowBgTexture = tex;

            var entry = GetConfigManagerField<ConfigEntry<Color>>("_entryBackgroundColor");
            if (result is not null && entry is not null)
                result.entryBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_widgetBackgroundColor");
            if (result is not null && entry is not null)
                result.widgetBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_windowBackgroundColor");
            if (result is not null && entry is not null)
                result.windowBgColor = entry.Value;
            entry = GetConfigManagerField<ConfigEntry<Color>>("_fontColor");
            if (result is not null && entry is not null)
                result.fontBgColor = entry.Value;

            return result;
        }

        public static void ReloadConfigDisplay()
        {
            if (instance is not null && DisplayingWindow is true)
            {
                cmType.GetType().GetMethod("BuildSettingList").Invoke(instance, Array.Empty<object>());
            }
        }

    }
}
