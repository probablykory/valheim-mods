using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    // A simplified wrapper for FileSystemWatcher
    public class Watcher
    {
        public event Action<object, FileSystemEventArgs>? FileChanged;

        public bool EnableRaisingEvents
        {
            get { return fileSystemWatcher == null ? false : fileSystemWatcher.EnableRaisingEvents; }
            set { if (fileSystemWatcher != null) { fileSystemWatcher.EnableRaisingEvents = value; } }
        }

        private FileSystemWatcher fileSystemWatcher = null!;

        public Watcher(string path, string filter)
        {
            if (path == null) { throw new ArgumentNullException("path"); }
            if (filter == null) { throw new ArgumentNullException("filter"); }

            Get.Plugin.LogDebugOnly($"Watcher created for {path}, {filter}");

            fileSystemWatcher = new FileSystemWatcher(path, filter);
            fileSystemWatcher.Changed += OnCreatedChangedOrRenamed;
            fileSystemWatcher.Created += OnCreatedChangedOrRenamed;
            fileSystemWatcher.Renamed += OnCreatedChangedOrRenamed;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnCreatedChangedOrRenamed(object sender, FileSystemEventArgs args)
        {
            Get.Plugin.LogDebugOnly($"OnCreatedChangedOrRenamed triggered {args.Name}, {args.ChangeType}");
            FileChanged?.Invoke(sender, args);
        }
    }
}
