using UnityEditor.AssetImporters;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AsepriteImporter.Data;
using AsepriteImporter.Sheet;

namespace AsepriteImporter
{
    [ScriptedImporter(1, new[] { "ase", "aseprite" })]
    public class AsepriteImporter : ScriptedImporter
    {
        /// <summary>
        /// Generates a Texture2D with the 16x16 Aseprite file icon.
        /// This texture is cached for subsequent calls.
        /// </summary>
        private static Texture2D CreateFileIcon()
        {
            if (fileIcon == null)
            {
                Color t = new Color(0.00f, 0.00f, 0.00f, 0.0f); // Transparent
                Color b = new Color(0.40f, 0.33f, 0.38f, 1.0f); // Border
                Color s = new Color(0.49f, 0.57f, 0.62f, 1.0f); // Shadow
                Color w = new Color(1.00f, 1.00f, 1.00f, 1.0f); // White

                fileIcon = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                fileIcon.name = "Aseprite File Icon";
                fileIcon.SetPixels(new[]
                {
                    t, t, t, t, t, t, t, t, t, t, t, t, t, t, t, t,
                    t, t, b, b, b, b, b, b, b, b, b, b, b, b, t, t,
                    t, t, b, s, s, s, s, s, s, s, s, s, s, b, t, t,
                    t, t, b, w, w, w, w, w, w, w, w, w, w, b, t, t,
                    t, t, b, w, w, w, w, w, w, w, w, w, w, b, t, t,
                    t, t, b, w, w, w, w, w, w, w, w, w, w, b, t, t,
                    t, t, b, w, w, w, w, w, w, w, w, w, w, b, t, t,
                    t, t, b, w, w, w, w, w, w, w, w, w, w, b, t, t,
                    t, t, b, w, w, b, w, w, w, w, b, w, w, b, t, t,
                    t, t, b, w, w, b, w, w, w, w, b, w, w, b, t, t,
                    t, t, b, w, w, b, w, w, w, s, b, s, s, b, t, t,
                    t, t, b, w, w, b, w, w, w, b, b, b, b, b, t, t,
                    t, t, b, w, w, w, w, w, w, b, w, w, w, b, t, t,
                    t, t, b, w, w, w, w, w, w, b, w, w, b, t, t, t,
                    t, t, b, w, w, w, w, w, w, b, w, b, t, t, t, t,
                    t, t, b, b, b, b, b, b, b, b, b, t, t, t, t, t,
                });
                fileIcon.Apply(false, true);
            }

            return fileIcon;
        }

        /// <summary>
        /// Helper class for building the collection of cels and layers for each top-level texture group.
        /// </summary>
        private class TextureGroup
        {
            public string Name { get; }
            public IEnumerable<(AsepriteCelChunk, AsepriteLayerChunk)> Members => members;

            private List<(AsepriteCelChunk, AsepriteLayerChunk)> members = new List<(AsepriteCelChunk, AsepriteLayerChunk)>();

            public TextureGroup(string name) { Name = name; }
            public void Add(AsepriteCelChunk chunk, AsepriteLayerChunk layer) { members.Add((chunk, layer)); }
        }

        private static Texture2D fileIcon = null;

        // TODO: Make project preferences?
        [SerializeField, Tooltip("Top-level layers beginning with any of these prefixes will be ignored along with their children.")]
        private string[] ignorePrefixes = new[] { "@", "." };
        [SerializeField, Tooltip("Layers containing any of the following strings (not case-sensitive) in their name will be imported as Linear rather than sRGB.")]
        private string[] linearKeywords = new[] { "norm", "metal", "rough", "smooth" };
        [SerializeField, Tooltip("If true, will generate quads with the given UVs from all the sprite islands in this sprite (determined by color alpha).")]
        private bool quadGeneration = false;
        [SerializeField, Tooltip("The keyword (partial match) of the layer with the alpha channel to use for determining UV islands for quad generation.")]
        private string quadAlphaKeyword = "color";
        [SerializeField, Tooltip("Size of each pixel, in meters.")]
        private float quadPixelScale = 0.1f;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            AsepriteFile file = ReadFile(ctx);
            AsepriteHeader header = file.Header;

            if ((header.PixelWidth != 1) || (header.PixelHeight != 1))
                throw new NotSupportedException("pixel size ratio unsupported");
            if (header.ColorDepth != AsepriteColorDepth.RGBA)
                throw new NotSupportedException("unsupported color depth: " + header.ColorDepth);
            if (header.Frames > 1)
                throw new NotSupportedException("multiple frames unsupported");

            AsepriteAsset asset = ScriptableObject.CreateInstance<AsepriteAsset>();
            ctx.AddObjectToAsset("icon", asset, CreateFileIcon());
            ctx.SetMainObject(asset);

            Texture2D meshGenerationTex = null;
            string filename = Path.GetFileNameWithoutExtension(ctx.assetPath);
            foreach (Texture2D tex in BuildTextures(file, filename))
            {
                ctx.AddObjectToAsset(tex.name, tex, tex);
                if (tex.name.ToLower().Contains(quadAlphaKeyword.ToLower()))
                    meshGenerationTex = tex;
            }

            if (quadGeneration)
            {
                GameObject prefab = new GameObject($"{filename}_SheetPrefab");
                foreach ((Mesh mesh, Vector3 origin) in GenerateMeshes(meshGenerationTex, filename))
                {
                    ctx.AddObjectToAsset(mesh.name, mesh);
                    AddMeshToPrefab(mesh, origin, prefab);
                }
                ctx.AddObjectToAsset(prefab.name, prefab);
            }
        }

        /// <summary>
        /// Reads the Aseprite serialization information from the given import context.
        /// </summary>
        private AsepriteFile ReadFile(AssetImportContext ctx)
        {
            using FileStream stream = File.Open(ctx.assetPath, FileMode.Open);
            using BinaryReader reader = new BinaryReader(stream);
            return new AsepriteFile(reader);
        }

        /// <summary>
        /// Builds each of the textures for this file by combining file layers.
        /// </summary>
        private IEnumerable<Texture2D> BuildTextures(AsepriteFile file, string filename)
        {
            AsepriteHeader header = file.Header;
            AsepriteColorDepth depth = header.ColorDepth;
            int dataSize = header.Width * header.Height;

            foreach (TextureGroup group in BuildGroups(file))
            {
                Color32[] currentColors = new Color32[dataSize];

                foreach ((var celChunk, var layerChunk) in group.Members)
                {
                    var blendOp = AsepriteBlend.GetBlendOperation(layerChunk.BlendMode);
                    byte opacity = AsepriteBlend.Blend(layerChunk.Opacity, celChunk.Opacity);
                    Color32[] celColors = ConvertCelToColors(celChunk, header.ColorDepth);
                    int celYOffset = header.Height - celChunk.YPosition - 1;

                    for (int h = 0; h < celChunk.Height; ++h)
                    {
                        for (int w = 0; w < celChunk.Width; ++w)
                        {
                            Color32 source = celColors[(h * celChunk.Width) + w];

                            int x = celChunk.XPosition + w;
                            int y = celYOffset - h;
                            int index = (y * header.Height) + x;
                            Color32 backdrop = currentColors[index];

                            currentColors[index] = blendOp(backdrop, source, opacity);
                        }
                    }
                }

                yield return CreateTexture(
                    file, 
                    currentColors, 
                    filename + "_" + group.Name, 
                    ShouldMarkLinear(group.Name));
            }
        }

        /// <summary>
        /// Checks if a group name contains any of the names flagged as linear layers.
        /// </summary>
        private bool ShouldMarkLinear(string groupName)
        {
            foreach (string match in linearKeywords)
                if (groupName.ToLower().Contains(match.ToLower()))
                    return true;
            return false;
        }

        /// <summary>
        /// Converts a cel chunk's raw data into a linear array of Color32 structs.
        /// </summary>
        private Color32[] ConvertCelToColors(AsepriteCelChunk cel, AsepriteColorDepth depth)
        {
            Color32[] result = new Color32[cel.Width * cel.Height];
            switch (depth)
            {
                case AsepriteColorDepth.RGBA:
                    for (int idx = 0; idx < result.Length; ++idx)
                    {
                        int byteIdx = idx * 4;
                        result[idx] = new Color32(
                            cel.RawPixelData[byteIdx + 0],
                            cel.RawPixelData[byteIdx + 1],
                            cel.RawPixelData[byteIdx + 2],
                            cel.RawPixelData[byteIdx + 3]);
                    }
                    break;

                default:
                    throw new NotImplementedException("unsupported color depth: " + depth);
            }

            return result;
        }

        /// <summary>
        /// Creates a named texture based on the file dimensions and properties and the given pixels.
        /// </summary>
        private Texture2D CreateTexture(AsepriteFile file, Color32[] pixels, string name, bool linear)
        {
            Texture2D texture = 
                new Texture2D(
                    file.Header.Width,
                    file.Header.Height, 
                    TextureFormat.RGBA32,
                    false, 
                    linear);

            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// Builds a list of layers and cells for each top-level texture group in the file.
        /// Filters groups by name prefix markers, visibility, and hierarchy for inclusion.
        /// </summary>
        private IEnumerable<TextureGroup> BuildGroups(AsepriteFile file)
        {
            AsepriteFrame firstFrame = file.Frames[0];
            var celChunks = BuildCelLookup(firstFrame);
            var layerChunks = firstFrame.LayerChunks;

            ushort lastExcluded = ushort.MaxValue;
            TextureGroup current = null;

            for (int idx = 0; idx < layerChunks.Count; ++idx)
            {
                // The celChunk may be null if the layer has no cel
                AsepriteCelChunk celChunk = celChunks[idx];
                AsepriteLayerChunk layerChunk = layerChunks[idx];

                if (layerChunk.ChildLevel > lastExcluded)
                {
                    continue;
                }

                if (ShouldSkipLayer(layerChunk))
                {
                    lastExcluded = layerChunk.ChildLevel;
                    continue;
                }

                if (layerChunk.ChildLevel == 0)
                {
                    if (current != null)
                        yield return current;
                    current = new TextureGroup(layerChunk.Name);
                }

                if (celChunk != null)
                {
                    current.Add(celChunk, layerChunk);
                }

                lastExcluded = ushort.MaxValue;
            }

            if (current != null)
            {
                yield return current;
            }
        }

        /// <summary>
        /// Builds a sparse array of cels mapped to their layer index.
        /// Not every layer will have a cel, so some entries may be null.
        /// </summary>
        private AsepriteCelChunk[] BuildCelLookup(AsepriteFrame frame)
        {
            AsepriteCelChunk[] result = new AsepriteCelChunk[frame.LayerChunks.Count];
            foreach (AsepriteCelChunk celChunk in frame.CelChunks)
                result[celChunk.LayerIndex] = celChunk;
            return result;
        }

        /// <summary>
        /// Returns true if a layer should be skipped for its individual properties.
        /// Does not take into account any properties inherited from any parent layers.
        /// </summary>
        private bool ShouldSkipLayer(AsepriteLayerChunk layer)
        {
            if (layer.Flags.IsReference())
                return true; // Ignore reference layers
            if (ignorePrefixes.Any(s => layer.Name.StartsWith(s)))
                return true; // Ignore layers with any of the skipped prefixes
            if ((layer.Flags.IsVisible() == false) && (layer.ChildLevel != 0))
                return true; // Ignore invisible layers, unless it's a top-level layer
            return false;
        }

        /// <summary>
        /// Generates meshes with size and UV mapping corresponding to the unique identified
        /// islands on the sprite sheet. Requires alpha information from a selected key texture.
        /// </summary>
        private IEnumerable<(Mesh, Vector3)> GenerateMeshes(Texture2D alphaTex, string filename)
        {
            if (alphaTex != null)
                return new AsepriteSheet(alphaTex).ConvertToMeshes(filename, quadPixelScale);

            Debug.LogWarning($"Couldn't find a key texture for generating meshes for {filename}.");
            return Enumerable.Empty<(Mesh, Vector3)>();
        }

        /// <summary>
        /// Adds a mesh to the sheet collection prefab, setting the origin correctly
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="prefab"></param>
        private void AddMeshToPrefab(Mesh mesh, Vector3 origin, GameObject prefab)
        {
            GameObject meshObject = new GameObject(mesh.name);
            meshObject.AddComponent<MeshFilter>().mesh = mesh;
            meshObject.AddComponent<MeshRenderer>();
            meshObject.transform.parent = prefab.transform;
            meshObject.transform.position = origin;
        }
    }
}
