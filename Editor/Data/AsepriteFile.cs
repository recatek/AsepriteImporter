using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AsepriteImporter.Data
{
    /// <summary>
    /// A serialized Aseprite data file.
    /// https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
    /// </summary>
    internal class AsepriteFile
    {
        public AsepriteHeader Header { get; }
        public AsepriteFrame[] Frames { get; }

        public AsepriteFile(BinaryReader reader)
        {
            Header = new AsepriteHeader(reader);

            if (Header.Size != reader.BaseStream.Length)
                throw new FormatException("aseprite file length invalid");

            Frames = new AsepriteFrame[Header.Frames];
            for (int i = 0; i < Header.Frames; ++i)
                Frames[i] = new AsepriteFrame(reader, Header.ColorDepth);

            if (reader.BaseStream.Position != reader.BaseStream.Length)
                throw new FormatException("failed to read entire aseprite file");
        }
    }

    internal static class AsepriteFileExtensions
    {
        public static string ReadAsepriteString(this BinaryReader reader)
        {
            var stringSize = reader.ReadUInt16();
            var stringBytes = reader.ReadBytes(stringSize);
            return Encoding.UTF8.GetString(stringBytes, 0, stringSize);
        }

        public static byte[] ReadAsepriteCompressedPixelData(this BinaryReader reader, AsepriteColorDepth depth, ushort width, ushort height, long dataSize)
        {
            if (reader.ReadByte() != 0x78)
                throw new NotSupportedException("unsupported zlib compression mode");
            if (reader.ReadByte() != 0x9C)
                throw new NotSupportedException("unsupported zlib compression mode");

            // Correct dataSize for the two zlib header bytes
            dataSize -= 2;

            byte[] compressed = reader.ReadBytes((int)dataSize);
            byte[] decompressed = new byte[width * height * depth.ToCapacity()];

            using (MemoryStream memory = new MemoryStream(compressed))
                using (DeflateStream deflate = new DeflateStream(memory, CompressionMode.Decompress))
                    deflate.Read(decompressed, 0, decompressed.Length);
            return decompressed;
        }
    }
}
