using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Managers
{
    // A simplified wrapper for FileSystemWatcher, eats duplicate fsw events
    public class Watcher
    {
        public const long consumeThreshold = 100000; // 10ms

        public event Action<object, FileSystemEventArgs>? FileChanged;

        public bool EnableRaisingEvents
        {
            get { return fileSystemWatcher == null ? false : fileSystemWatcher.EnableRaisingEvents; }
            set { if (fileSystemWatcher != null) { fileSystemWatcher.EnableRaisingEvents = value; } }
        }

        private FileSystemWatcher fileSystemWatcher = null!;
        private DateTime lastRead = DateTime.MinValue;
        private WatcherChangeTypes lastChange = WatcherChangeTypes.All;

        public Watcher(string path, string filter)
        {
            if (path == null) { throw new ArgumentNullException("path"); }
            if (filter == null) { throw new ArgumentNullException("filter"); }

            Logger.LogDebugOnly($"Watcher created for {path}, {filter}");

            fileSystemWatcher = new FileSystemWatcher(path, filter);
            fileSystemWatcher.Changed += OnAnyFilesystemEvent;
            fileSystemWatcher.Created += OnAnyFilesystemEvent;
            fileSystemWatcher.Deleted += OnAnyFilesystemEvent;
            fileSystemWatcher.Renamed += OnAnyFilesystemEvent;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnAnyFilesystemEvent(object sender, FileSystemEventArgs args)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(args.FullPath);

            if (lastWriteTime.Ticks - lastRead.Ticks <= consumeThreshold && lastChange == args.ChangeType)
            {
                Logger.LogDebugOnly($"Consuming duplicate FileSystemEvent: {args.Name}, {args.ChangeType}");
            }
            else
            {
                lastRead = lastWriteTime;
                lastChange = args.ChangeType;

                Logger.LogDebugOnly($"OnAnyFilesystemEvent triggered: {args.Name}, {args.ChangeType}");
                FileChanged?.Invoke(sender, args);
            }
        }
    }
}
