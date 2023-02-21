using System;
namespace ThreeDO
{
    public class Palette
    {
        public byte[] Data { get; set; }

        public Palette()
        {
            Data = new byte[256 * 3];
        }

        public Palette(byte[] data)
        {
            Data = data;
        }

        public static Palette LoadFromFile(string filePath)
        {
            using var s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var pal = new Palette();
            s.ReadExactly(pal.Data);
            return pal;
        }

        public static Palette Default { get; }

        static Palette()
        {
            var asm = typeof(Palette).Assembly;
            var names = asm.GetManifestResourceNames();
            using var s = asm.GetManifestResourceStream("ThreeDO.Core.DEFAULT.PAL");
            var pal = new Palette();
            s.ReadExactly(pal.Data);
            Default = pal;
        }
    }
}

