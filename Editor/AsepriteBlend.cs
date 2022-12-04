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
        /// Functional stand-in for Pixman's MUL_UN8 helper macro.
        /// </summary>
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
                AsepriteBlendMode.Screen => BlendScreen,
                AsepriteBlendMode.Addition => BlendAddition,
                AsepriteBlendMode.Subtract => BlendSubtract,
                _ => throw new NotSupportedException("unsupported blend mode: " + mode),
            };
        }

        public static Color32 BlendNormal(Color32 backdrop, Color32 source, byte opacity)
        {
            if (backdrop.a == 0)
                return new(source.r, source.g, source.b, Blend(source.a, opacity));
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
            Color32 result = new(
                Blend(backdrop.r, source.r),
                Blend(backdrop.g, source.g),
                Blend(backdrop.b, source.b),
                source.a);
            return BlendNormal(backdrop, result, opacity);
        }

        public static Color32 BlendScreen(Color32 backdrop, Color32 source, byte opacity)
        {
            Color32 result = new(
                (byte)(backdrop.r + source.r - Blend(backdrop.r, source.r)),
                (byte)(backdrop.g + source.g - Blend(backdrop.g, source.g)),
                (byte)(backdrop.b + source.b - Blend(backdrop.b, source.b)),
                source.a);
            return BlendNormal(backdrop, result, opacity);
        }


        public static Color32 BlendAddition(Color32 backdrop, Color32 source, byte opacity)
        {
            Color32 result = new(
                (byte)Math.Min(backdrop.r + source.r, 255),
                (byte)Math.Min(backdrop.g + source.g, 255),
                (byte)Math.Min(backdrop.b + source.b, 255),
                source.a);
            return BlendNormal(backdrop, result, opacity);
        }

        public static Color32 BlendSubtract(Color32 backdrop, Color32 source, byte opacity)
        {
            Color32 result = new(
                (byte)Math.Max(backdrop.r - source.r, 0),
                (byte)Math.Max(backdrop.g - source.g, 0),
                (byte)Math.Max(backdrop.b - source.b, 0),
                source.a);
            return BlendNormal(backdrop, result, opacity);
        }
    }
}
