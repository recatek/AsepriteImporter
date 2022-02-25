using System;

namespace AsepriteImporter.Data
{
    [Flags]
    internal enum AsepriteLayerFlags : ushort
    {
        None = 0,
        Visible = 1,
        Editable = 2,
        LockMovement = 4,
        Background = 8,
        PreferLinkedCels = 16,
        DisplayCollapsed = 32,
        Reference = 64,
    };

    internal static class AsepriteLayerFlagsExtensions
    {
        public static bool IsVisible(this AsepriteLayerFlags flags)
        {
            return (flags & AsepriteLayerFlags.Visible) != 0;
        }

        public static bool IsReference(this AsepriteLayerFlags flags)
        {
            return (flags & AsepriteLayerFlags.Reference) != 0;
        }
    }
}
