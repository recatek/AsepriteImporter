using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AsepriteImporter.Sheet
{
    internal class AsepriteSheet
    {
        private static AsepriteSheetCell[] BuildCellGrid(Texture2D color)
        {
            int width = color.width;
            int height = color.height;
            Color32[] data = color.GetPixels32();

            AsepriteSheetCell[] cells = new AsepriteSheetCell[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // Need to flip the Texture2D on the Y axis
                    int yFlip = (height - 1) - y;
                    int toIndex = y * width + x;
                    int fromIndex = yFlip  * width + x;

                    cells[toIndex] = 
                        data[fromIndex].a > 0 
                            ? AsepriteSheetCell.Full 
                            : AsepriteSheetCell.Empty;
                }
            }

            return cells;
        }

        private readonly int width;
        private readonly int height;
        private readonly AsepriteSheetCell[] cells;

        public AsepriteSheet(Texture2D alphaTex)
        {
            width = alphaTex.width;
            height = alphaTex.height;
            cells = BuildCellGrid(alphaTex);
        }

        /// <summary>
        /// Generates meshes with size and UV mapping corresponding to the unique identified
        /// islands on the sprite sheet. This can only be done once for a given AsepriteSheet.
        /// Meshes are returned with their origin on the sprite sheet, for arrangement.
        /// </summary>
        public IEnumerable<(Mesh, Vector3)> ConvertToMeshes(string filename, float scale)
        {
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                    if (cells[y * width + x] == AsepriteSheetCell.Full)
                        yield return GenerateMeshFromIsland(x, y, filename, scale);
        }


        /// <summary>
        /// Given a starting (x, y), generates a mesh from the bounds of all connected pixels.
        /// </summary>
        private (Mesh, Vector3) GenerateMeshFromIsland(int startX, int startY, string filename, float scale)
        {
            var rect = new AsepriteSheetRect(startX, startY);
            var stack = new Stack<(int, int)>();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                (int x, int y) = stack.Pop();
                cells[y * width + x] = AsepriteSheetCell.Visited;
                rect.AddPixel(x, y);
                PushNeighbors(x, y, stack);
            }

            return rect.BuildQuad(filename, scale, width, height);
        }

        /// <summary>
        /// Pushes all the neighbors of the given cell on to the stack if they're full and unvisited.
        /// </summary>
        private void PushNeighbors(int x, int y, Stack<(int, int)> stack)
        {
            int minX = Math.Clamp(x - 1, 0, width  - 1);
            int minY = Math.Clamp(y - 1, 0, height - 1);
            int maxX = Math.Clamp(x + 1, 0, width  - 1);
            int maxY = Math.Clamp(y + 1, 0, height - 1);

            for (int yq = minY; yq <= maxY; ++yq)
                for (int xq = minX; xq <= maxX; ++xq)
                    if (xq != x || yq != y)
                        if (cells[yq * width + xq] == AsepriteSheetCell.Full)
                            stack.Push((xq, yq));
        }
    }
}
