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
                    try
                    {
                        if (Gob.GetGob(gp).ReadFile(name) is byte[] data)
                        {
                            return data;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            foreach (var gp in GobPaths)
            {
                try
                {
                    if (Gob.GetGob(gp).ReadFile(name) is byte[] data)
                    {
                        return data;
                    }
                }
                catch
                {
                }
            }
            return null;
        }
    }
}

