using System;
using System.IO;

namespace Managers
{
    public class ConfigWatcher
    {
        private IPlugin plugin;

        public ConfigWatcher(IPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            this.plugin = plugin;
            CheckForConfigManager();
        }

        private void InitializeWatcher()
        {
            string file = Path.GetFileName(plugin.Config.ConfigFilePath);
            string path = Path.GetDirectoryName(plugin.Config.ConfigFilePath);

            var watcher = new Watcher(path, file);
            watcher.FileChanged += OnFileChanged;
        }

        private void CheckForConfigManager()
        {
            if (Main.IsHeadless)
            {
                InitializeWatcher();
            }
            else
            {
                if (CmAPI.IsLoaded())
                {
                    CmAPI.OnDisplayingWindowChanged += OnCmDisplayingWindowChanged;
                }
                else
                {
                    InitializeWatcher();
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            string path = plugin.Config.ConfigFilePath;

            if (!File.Exists(path))
            {
                return;
            }
            try
            {
                TriggerReload();
            }
            catch
            {
                Logger.LogError("There was an issue with your " + Path.GetFileName(path) + " file.");
                Logger.LogError("Please check the format and spelling.");
                return;
            }
        }
        private void OnCmDisplayingWindowChanged(object sender, EventArgs e)
        {
            if (!CmAPI.DisplayingWindow)
            {
                TriggerReload();
            }
        }

        private void TriggerReload()
        {
            bool prev = plugin.Config.SaveOnConfigSet;
            plugin.Config.SaveOnConfigSet = false;
            plugin.Config.Reload();
            plugin.Config.SaveOnConfigSet = prev;
        }
    }
}
