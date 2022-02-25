using System.IO;

namespace AsepriteImporter.Data
{
    internal class AsepriteLayerChunk : AsepriteChunk
    {
        public const ushort CHUNK_TYPE = 0x2004;

        public AsepriteLayerFlags Flags { get; }
        public AsepriteLayerType Type { get; }
        public ushort ChildLevel { get; }
        public AsepriteBlendMode BlendMode { get; } 
        public byte Opacity { get; }
        public string Name { get; }
        public ushort TilesetIndex { get; }

        public AsepriteLayerChunk(BinaryReader reader)
        {
            Flags = (AsepriteLayerFlags)reader.ReadUInt16();
            Type = (AsepriteLayerType)reader.ReadUInt16();
            ChildLevel = reader.ReadUInt16();

            reader.ReadUInt16(); // Ignored
            reader.ReadUInt16(); // Ignored

            BlendMode = (AsepriteBlendMode)reader.ReadUInt16();
            Opacity = reader.ReadByte();

            reader.ReadBytes(3); // Reserved

            Name = reader.ReadAsepriteString();

            // TilesetIndex only exists if this layer is a tilemap
            TilesetIndex = (Type == AsepriteLayerType.Tilemap) 
                ? reader.ReadUInt16() 
                : default;
        }
    }
}
