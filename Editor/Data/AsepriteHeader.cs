using System;
using System.IO;

namespace AsepriteImporter.Data
{
    internal class AsepriteHeader
    {
        public uint Size { get; }
        public ushort Frames { get; }
        public ushort Width { get; }
        public ushort Height { get; }
        public AsepriteColorDepth ColorDepth { get; }
        public uint Flags { get; }
        public ushort Speed { get; }
        public byte TransparentIndex { get; }
        public ushort NumColors { get; }
        public byte PixelWidth { get; }
        public byte PixelHeight { get; }
        public short GridX { get; }
        public short GridY { get; }
        public ushort GridWidth { get; }
        public ushort GridHeight { get; }

        public AsepriteHeader(BinaryReader reader)
        {
            Size = reader.ReadUInt32();

            if (reader.ReadUInt16() != 0xA5E0)
                throw new FormatException("not a valid aseprite file");

            Frames = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            ColorDepth = (AsepriteColorDepth)reader.ReadUInt16();
            Flags = reader.ReadUInt32();
            Speed = reader.ReadUInt16();

            reader.ReadUInt32(); // Zero
            reader.ReadUInt32(); // Zero

            TransparentIndex = reader.ReadByte();

            reader.ReadBytes(3); // Skip

            NumColors = reader.ReadUInt16();
            PixelWidth = reader.ReadByte();
            PixelHeight = reader.ReadByte();

            GridX = reader.ReadInt16();
            GridY = reader.ReadInt16();
            GridWidth = reader.ReadUInt16();
            GridHeight = reader.ReadUInt16();

            reader.ReadBytes(84); // Reserved
        }
    }
}
