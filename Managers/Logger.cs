using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    // Util to allow multiple alternate log sources for debugging, but only a single log source when Debug=false.
    public class Logger
    {
        private static Logger _instance;
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
            }
        }

        private static readonly Dictionary<string, ManualLogSource> logSources = new Dictionary<string, ManualLogSource>();

        private static IPlugin Mod { get { return Main.Mod; } }

        public static ManualLogSource GetLogger()
        {
            if (Mod.Debug)
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
            return Mod.LogSource;

        }
        public static void LogDebugOnly(object data)
        {
            if (Mod.Debug)
                GetLogger().LogDebug(data);
        }

        public static void LogDebug(object data)
        {
            if (Mod.Debug)
                GetLogger().LogDebug(data);
            else
                Mod.LogSource.LogDebug(data);
        }

        public static void LogInfo(object data)
        {
            if (Mod.Debug)
                GetLogger().LogInfo(data);
            else
                Mod.LogSource.LogInfo(data);
        }

        public static void LogMessage(object data)
        {
            if (Mod.Debug)
                GetLogger().LogMessage(data);
            else
                Mod.LogSource.LogMessage(data);
        }

        public static void LogWarning(object data)
        {
            if (Mod.Debug)
                GetLogger().LogWarning(data);
            else
                Mod.LogSource.LogWarning(data);
        }

        public static void LogError(object data)
        {
            if (Mod.Debug)
                GetLogger().LogError(data);
            else
                Mod.LogSource.LogError(data);
        }

        public static void LogFatal(object data)
        {
            if (Mod.Debug)
                GetLogger().LogFatal(data);
            else
                Mod.LogSource.LogFatal(data);
        }
    }
}
