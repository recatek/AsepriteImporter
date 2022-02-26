using System;
using System.Collections.Generic;
using System.IO;

namespace AsepriteImporter.Data
{
    internal class AsepriteFrame
    {
        public uint Size { get; }
        public ushort DurationMs { get; }
        public IReadOnlyList<AsepriteCelChunk> CelChunks => celChunkList;
        public IReadOnlyList<AsepriteLayerChunk> LayerChunks => layerChunkList;
        public IReadOnlyList<ushort> SkippedChunkTypes => skippedChunkTypes;

        private readonly List<AsepriteCelChunk> celChunkList;
        private readonly List<AsepriteLayerChunk> layerChunkList;
        private readonly List<ushort> skippedChunkTypes;

        public AsepriteFrame(BinaryReader reader, AsepriteColorDepth colorDepth)
        {
            Size = reader.ReadUInt32();

            if (reader.ReadUInt16() != 0xF1FA)
                throw new FormatException("not a valid aseprite file");

            ushort oldChunkCount = reader.ReadUInt16();
            DurationMs = reader.ReadUInt16();
            reader.ReadBytes(2); // Reserved
            uint newChunkCount = reader.ReadUInt32();

            celChunkList = new List<AsepriteCelChunk>();
            layerChunkList = new List<AsepriteLayerChunk>();
            skippedChunkTypes = new List<ushort>();

            uint chunkCount = (newChunkCount == 0) ? oldChunkCount : newChunkCount;
            for (uint i = 0; i < chunkCount; ++i)
            {
                (uint chunkSize, ushort chunkType) = AsepriteChunk.ReadChunkHeader(reader);
                switch (chunkType)
                {
                    case AsepriteCelChunk.CHUNK_TYPE: celChunkList.Add(new AsepriteCelChunk(reader, colorDepth, chunkSize)); break;
                    case AsepriteLayerChunk.CHUNK_TYPE: layerChunkList.Add(new AsepriteLayerChunk(reader)); break;
                    default: AsepriteChunk.SkipChunk(reader, chunkSize); skippedChunkTypes.Add(chunkType); break;
                }
            }
        }
    }
}
