using System;
using System.Runtime.CompilerServices;
using UnityEngine;

using AsepriteImporter.Data;

namespace AsepriteImporter
{
    internal static class AsepriteBlend
    {
        public delegate Color32 BlendOp(Color32 backdrop, Color32 source, byte opacity);

        /// <summary>
        /// Computes (a/255) * (b/255) * 255 in a fast bitshift way.
        /// Inspired by Pixman's MUL_UN8 helper macro.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Blend(byte a, byte b)
        {
            int t = a * b + 0x80;
            return (byte)(((t >> 8) + t) >> 8);
        }

        public static BlendOp GetBlendOperation(AsepriteBlendMode mode)
        {
            return mode switch
            {
                AsepriteBlendMode.Normal => BlendNormal,
                AsepriteBlendMode.Multiply => BlendMultiply,
                _ => throw new NotSupportedException("unsupported blend mode: " + mode),
            };
        }

        public static Color32 BlendNormal(Color32 backdrop, Color32 source, byte opacity)
        {
            if (backdrop.a == 0)
                return new Color32(source.r, source.g, source.b, Blend(source.a, opacity));
            if (source.a == 0)
                return backdrop;

            byte sourceA = Blend(source.a, opacity);
            int resultA = sourceA + backdrop.a - Blend(backdrop.a, sourceA);

            return new Color32(
                (byte)(backdrop.r + (source.r - backdrop.r) * sourceA / resultA),
                (byte)(backdrop.g + (source.g - backdrop.g) * sourceA / resultA),
                (byte)(backdrop.b + (source.b - backdrop.b) * sourceA / resultA),
                (byte)resultA);
        }

        public static Color32 BlendMultiply(Color32 backdrop, Color32 source, byte opacity)
        {
            Color32 mult = new Color32(
                Blend(backdrop.r, source.r),
                Blend(backdrop.g, source.g),
                Blend(backdrop.b, source.b),
                source.a);
            return BlendNormal(backdrop, mult, opacity);
        }
    }
}
