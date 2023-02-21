using System;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.Intrinsics.Arm;
using System.Text;

using Crc32 = System.IO.Hashing.Crc32;

namespace ThreeDO
{
    public class Bitmap
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public BitmapTransparency Transparency { get; set; }
        public byte[] Columns { get; set; } = Array.Empty<byte>();

        public static Bitmap FromFile(string bmPath)
        {
            var name = Path.GetFileName(bmPath);
            if (File.Exists(bmPath))
            {
                using var stream = new FileStream(bmPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new BinaryReader(stream);
                var magic = reader.ReadUInt32();
                var sizeX = reader.ReadUInt16();
                var sizeY = reader.ReadUInt16();
                if (sizeX == 1 && sizeY != 1)
                    throw new NotSupportedException("Multiple BM not supported");
                var idemX = reader.ReadUInt16();
                var idemY = reader.ReadUInt16();
                var transparent = (BitmapTransparency)reader.ReadByte();
                var logSizeY = reader.ReadByte();
                var compression = (BitmapCompression)reader.ReadUInt16();
                if (compression != BitmapCompression.NotCompressed)
                    throw new NotSupportedException($"Bitmap compression: {compression} not supported");
                var dataSize = reader.ReadInt32();
                reader.ReadBytes(12);
                return new Bitmap
                {
                    Name = name,
                    Width = sizeX,
                    Height = sizeY,
                    Transparency = transparent,
                    Columns = reader.ReadBytes(dataSize),
                };
            }
            return new Bitmap { Name = name };
        }

        public void SavePngInDir(string outputDir, Palette palette)
        {
            var outputPath = Path.Combine(outputDir, Path.ChangeExtension(Name, ".png"));
            if (Width == 0 || Height == 0)// || File.Exists(outputPath))
            {
                return;
            }
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new BinaryWriter(outputStream, Encoding.ASCII);

            // Write PNG header
            writer.Write((byte)137);
            writer.Write(Encoding.ASCII.GetBytes("PNG"));
            writer.Write((byte)13);
            writer.Write((byte)10);
            writer.Write((byte)26);
            writer.Write((byte)10);

            static uint Hton(uint v) => BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(v) : v;

            void WriteChunk(string type, Action<BinaryWriter> write) {
                using var mem = new MemoryStream();
                using var memWriter = new BinaryWriter(mem);
                using var cmem = new MemoryStream();
                using var cmemWriter = new BinaryWriter(cmem);
                write(cmemWriter);
                cmemWriter.Flush();
                var chunkData = cmem.ToArray();
                memWriter.Write(Encoding.ASCII.GetBytes(type));
                memWriter.Write(chunkData);
                memWriter.Flush();
                var chunkBytes = mem.ToArray();
                writer.Write(Hton((uint)chunkData.Length));
                writer.Write(chunkBytes);
                var crc = Crc32.Hash(chunkBytes);
                for (var i = 3; i >= 0; i--)
                {
                    writer.Write(crc[i]);
                }
            }

            // Write IHDR chunk
            WriteChunk("IHDR", w => {
                w.Write(Hton((uint)Width));
                w.Write(Hton((uint)Height));
                w.Write((byte)8); // Bit depth
                w.Write((byte)3); // 3 == palette index
                w.Write((byte)0); // 0 == deflate compression
                w.Write((byte)0); // 0 == no filter
                w.Write((byte)0); // 0 == no interlace
            });

            // Write PLTE chunk
            WriteChunk("PLTE", w => {
                w.Write(palette.Data);
            });

            // Write IDAT chunk
            var idat = new byte[(Width + 1) * Height];
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var idatIndex = y * (Width + 1) + x + 1;
                    var bmIndex = x * Height + (Height - 1 - y);
                    idat[idatIndex] = Columns[bmIndex];
                }
            }
            
            byte[] idatDeflated;
            {
                using var deflateMem = new MemoryStream();
                using var deflateStream = new System.IO.Compression.ZLibStream(deflateMem, System.IO.Compression.CompressionLevel.Optimal, true);
                deflateStream.Write(idat);
                deflateStream.Flush();
                idatDeflated = deflateMem.ToArray();
            }
            WriteChunk("IDAT", w => {
                w.Write(idatDeflated);
            });

            // Write IEND chunk
            WriteChunk("IEND", w => { });
        }
    }

    public enum BitmapTransparency
    {
        Normal = 0x36,
        Transparent = 0x3E,
        Weapon = 0x08,
    }

    public enum BitmapCompression
    {
        NotCompressed = 0,
        Rle = 1,
        Rle0 = 2,
    }
}

