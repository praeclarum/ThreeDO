using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

using MvvmHelpers;

using ThreeDO.Core;

namespace ThreeDO
{
    public class SearchDirectories: ObservableObject
    {
        public ObservableCollection<string> Directories { get; set; } = new();
        public ObservableCollection<string> GobPaths { get; set; } = new();

        public byte[]? ReadFile(string name, string startDir)
        {
            var startPath = Path.Combine(startDir, name);
            if (File.Exists(startPath))
                return File.ReadAllBytes (startPath);
            if (File.Exists(name))
                return File.ReadAllBytes(Path.GetFullPath(name));
            var justName = Path.GetFileName(name);
            if (File.Exists(justName))
                return File.ReadAllBytes(Path.GetFullPath(justName));
            foreach (var d in Directories)
            {
                var dname = Path.Combine(d, justName);
                if (File.Exists(dname))
                    return File.ReadAllBytes(dname);
                var gobPaths = Directory.GetFiles(d, "*.gob", SearchOption.AllDirectories);
                foreach (var gp in gobPaths)
                {
                    if (GetGob(gp).ReadFile(name) is byte[] data)
                    {
                        return data;
                    }
                }
            }
            foreach (var gp in GobPaths)
            {
                if (GetGob(gp).ReadFile(name) is byte[] data)
                {
                    return data;
                }
            }
            return null;
        }

        static readonly ConcurrentDictionary<string, Gob> gobs = new();

        static Gob GetGob(string gobPath)
        {
            var key = gobPath;
            if (gobs.TryGetValue(gobPath, out var g))
                return g;
            g = Gob.LoadFromFile(gobPath);
            gobs.TryAdd(key, g);
            return g;
        }
    }
}

