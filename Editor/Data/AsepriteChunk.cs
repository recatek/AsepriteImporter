using System.IO;

namespace AsepriteImporter.Data
{
    internal abstract class AsepriteChunk
    {
        protected const int HEADER_SIZE = 6;

        public static (uint, ushort) ReadChunkHeader(BinaryReader reader)
        {
            uint size = reader.ReadUInt32();
            ushort type = reader.ReadUInt16();
            return (size, type);
        }

        public static void SkipChunk(BinaryReader reader, uint size)
        {
            reader.ReadBytes((int)size - HEADER_SIZE);
        }
    }
}
