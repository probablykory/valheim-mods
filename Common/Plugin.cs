using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    // Implement this interface in your mod to enable the logger & config functionality.
    public interface IPlugin
    {
        ConfigFile Config { get; }
        bool Debug { get; }
        ManualLogSource Logger { get; }
    }

    // Extensions to allow multiple alternate log sources for debugging, but only a single log source when Debug=false.
    public static class LoggingExtensions
    {
        private static readonly Dictionary<string, ManualLogSource> logSources = new Dictionary<string, ManualLogSource>();

        internal static ManualLogSource GetLogger(this IPlugin mod)
        {
            if (mod.Debug)
            {
                var type = new StackFrame(2).GetMethod().DeclaringType;

                ManualLogSource result;
                if (!logSources.TryGetValue(type.FullName, out result))
                {
                    result = BepInEx.Logging.Logger.CreateLogSource(type.FullName);
                    logSources.Add(type.FullName, result);
                }

                return result;
            }
            return mod.Logger;

        }
        public static void LogDebugOnly(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogDebug(data);
        }

        public static void LogDebug(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogDebug(data);
            else
                mod.Logger.LogDebug(data);
        }

        public static void LogInfo(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogInfo(data);
            else
                mod.Logger.LogInfo(data);
        }

        public static void LogMessage(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogMessage(data);
            else
                mod.Logger.LogMessage(data);
        }

        public static void LogWarning(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogWarning(data);
            else
                mod.Logger.LogWarning(data);
        }

        public static void LogError(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogError(data);
            else
                mod.Logger.LogError(data);
        }

        public static void LogFatal(this IPlugin mod, object data)
        {
            if (mod.Debug)
                mod.GetLogger().LogFatal(data);
            else
                mod.Logger.LogFatal(data);
        }

    }

    // I hate this.  Figure out a better way to generically retreive the hosting BaseUnityPlugin 
    public static class Get
    {
        private static IPlugin cachedModRef = null;
        public static IPlugin Plugin
        {
            get
            {
                if (cachedModRef == null)
                {
                    Type mod = (new StackFrame(0).GetMethod().DeclaringType.Assembly.GetTypes()).Where(p => typeof(IPlugin).IsAssignableFrom(p)).FirstOrDefault();
                    cachedModRef = AccessTools.Field(mod, "Instance").GetValue(null) as IPlugin;
                    cachedModRef.LogDebugOnly($"Caching static mod reference");
                }
                return cachedModRef;
            }
        }
    }
}
