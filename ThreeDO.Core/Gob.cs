using System;
using System.Collections.Concurrent;
using System.Text;

namespace ThreeDO.Core
{
    public class Gob
    {
        readonly ConcurrentDictionary<string, byte[]> entries = new ();

        public Gob()
        {
        }

        public static Gob LoadFromFile(string gobPath)
        {
            var g = new Gob();
            using var stream = new FileStream(gobPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            var magic = reader.ReadBytes(4);
            var indexOffset = reader.ReadInt32();
            stream.Position = indexOffset;
            var numEntries = reader.ReadInt32();
            for (var i = 0; i < numEntries; i++)
            {
                stream.Position = indexOffset + 4 + 21 * i;
                var entryOffset = reader.ReadInt32();
                var entryLength = reader.ReadInt32();
                var nameBytes = reader.ReadBytes(13);
                var name = Encoding.ASCII.GetString(nameBytes).Trim('\0');
                stream.Position = entryOffset;
                var entryData = reader.ReadBytes(entryLength);
                g.entries.TryAdd(name.ToLowerInvariant(), entryData);
            }
            return g;
        }

        public byte[]? ReadFile(string name)
        {
            var key = name.ToLowerInvariant();
            if (entries.TryGetValue(key, out var d))
                return d;
            return null;
        }
    }
}

