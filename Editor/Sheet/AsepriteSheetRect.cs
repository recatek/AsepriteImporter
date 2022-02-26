using UnityEngine;

namespace AsepriteImporter.Sheet
{
    internal class AsepriteSheetRect
    {
        private static readonly int[] TRIANGLES = new[]
        {
            0, 3, 1,
            3, 0, 2,
        };

        private static readonly Vector3[] NORMALS = new[]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        private static readonly Vector4[] TANGENTS = new[]
        {
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
            new Vector4(1.0f, 0.0f, 0.0f, -1.0f),
        };

        private int minX;
        private int minY;
        private int maxX;
        private int maxY;

        public AsepriteSheetRect(int startX, int startY)
        {
            minX = startX;
            maxX = startX;
            minY = startY;
            maxY = startY;
        }

        /// <summary>
        /// Adds a pixel to the rect, expanding the bounds if necessary.
        /// </summary>
        public void AddPixel(int x, int y)
        {
            if (x < minX)
                minX = x;
            if (x > maxX)
                maxX = x;

            if (y < minY)
                minY = y;
            if (y > maxY)
                maxY = y;
        }

        /// <summary>
        /// Builds a quad mesh from the computed bounds of the rect along with the sheet origin.
        /// </summary>
        public (Mesh, Vector3) BuildQuad(string filename, float scale, int sheetWidth, int sheetHeight)
        {
            Mesh mesh = new Mesh()
            {
                name = $"{filename}_({minX}, {minY})_({maxX}, {maxY})",
                vertices = ComputeVertices(scale),
                uv = ComputeUVs(sheetWidth, sheetHeight),
                triangles = TRIANGLES,
                normals = NORMALS,
                tangents = TANGENTS,
            };

            Vector3 origin = new Vector3(
                (maxX + minX - sheetWidth) * scale * 0.5f,
                (-maxY - minY + sheetHeight) * scale * 0.5f,
                0.0f);

            return (mesh, origin);
        }

        private Vector3[] ComputeVertices(float scale)
        {
            Vector2 extents = new Vector2(
                (maxX - minX) * 0.5f * scale,
                (maxY - minY) * 0.5f * scale);
            Vector2 mirrorX = new Vector2(-extents.x, extents.y);

            return new Vector3[]
            {
                -extents,
                -mirrorX,
                 mirrorX,
                 extents,
            };
        }

        private Vector2[] ComputeUVs(int sheetWidth, int sheetHeight)
        {
            float widthInv = 1.0f / sheetWidth;
            float heightInv = 1.0f / sheetHeight;

            float minXf = minX * widthInv;
            float maxXf = (maxX + 1) * widthInv;
            float minYf = 1.0f - (minY * heightInv);
            float maxYf = 1.0f - ((maxY + 1) * heightInv);

            return new Vector2[]
            {
                  new Vector2(minXf, maxYf),
                  new Vector2(maxXf, maxYf),
                  new Vector2(minXf, minYf),
                  new Vector2(maxXf, minYf)
            };
        }
    }
}
