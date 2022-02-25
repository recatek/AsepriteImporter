using System;
using System.IO;
using UnityEngine;

namespace AsepriteImporter.Data
{
    internal class AsepriteCelChunk : AsepriteChunk
    {
        public const ushort CHUNK_TYPE = 0x2005;

        public ushort LayerIndex { get; }
        public short XPosition { get; }
        public short YPosition { get; }
        public byte Opacity { get; }
        public AsepriteCelType Type { get; }

        public ushort Width { get; }
        public ushort Height { get; }
        public byte[] RawPixelData { get; }

        public AsepriteCelChunk(BinaryReader reader, AsepriteColorDepth colorDepth, uint chunkSize)
        {
            long start = reader.BaseStream.Position;
            LayerIndex = reader.ReadUInt16();
            XPosition = reader.ReadInt16();
            YPosition = reader.ReadInt16();
            Opacity = reader.ReadByte();
            Type = (AsepriteCelType)reader.ReadUInt16();

            reader.ReadBytes(7); // Reserved

            switch (Type)
            {
                case AsepriteCelType.CompressedImage:
                    Width = reader.ReadUInt16();
                    Height = reader.ReadUInt16();
                    long dataSize = chunkSize - (HEADER_SIZE + (reader.BaseStream.Position - start));
                    RawPixelData = reader.ReadAsepriteCompressedPixelData(colorDepth, Width, Height, dataSize);
                    break;

                default:
                    throw new NotSupportedException("unsupported cel type: " + Type);
            }
        }
    }
}
