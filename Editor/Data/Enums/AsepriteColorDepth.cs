namespace AsepriteImporter.Data
{
    internal enum AsepriteColorDepth : uint
    {
        Indexed = 8,    // (Index)
        Grayscale = 16, // (Value, Alpha)
        RGBA = 32,      // (Red, Green, Blue, Alpha)
    };

    internal static class AsepriteColorDepthExtensions
    {
        public static uint ToCapacity(this AsepriteColorDepth depth) => (uint)depth / 8;
    }
}
