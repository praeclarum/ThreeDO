using System;
namespace ThreeDO
{
    public class Palette
    {
        public byte[] Data { get; set; } = new byte[256 * 3];

        public static Palette LoadFromFile(string filePath)
        {
            using var s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var pal = new Palette();
            s.ReadExactly(pal.Data);
            return pal;
        }
    }
}

