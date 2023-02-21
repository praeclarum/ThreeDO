using System;
namespace ThreeDO
{
    public class SearchDirectories
    {
        public List<string> Directories { get; set; } = new();

        public string? FindFile(string name, string startDir)
        {
            var startPath = Path.Combine(startDir, name);
            if (File.Exists(startPath))
                return startPath;
            if (File.Exists(name))
                return Path.GetFullPath(name);
            var justName = Path.GetFileName(name);
            if (File.Exists(justName))
                return Path.GetFullPath(justName);
            foreach (var d in Directories)
            {
                var dname = Path.Combine(d, justName);
                if (File.Exists(dname))
                    return dname;
            }
            return null;
        }
    }
}

