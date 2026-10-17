using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-102: the map canvas is two rasters: the margins (paper N/E/S, sea W) under the whole canvas and the
    // terrain over the map itself. Both carry the water art; the margins are never hit tested.
    internal sealed class MapTerrainVisual
    {
        public Texture2D Texture { get; }
        public Texture2D Margins { get; }
        public MapLandmarks Landmarks { get; }
        private readonly MapCanvasLayout canvas;
        private readonly Vertex[] vertices = new Vertex[4];
        private static readonly ushort[] Indices = { 0, 1, 2, 2, 3, 0 };

        public MapTerrainVisual(MapCanvasLayout canvas)
        {
            this.canvas = canvas;
            Texture = Resources.Load<Texture2D>(canvas.TerrainTexture);
            Margins = Resources.Load<Texture2D>(canvas.MarginsTexture);
            Landmarks = MapLandmarks.Load();
        }

        public void Draw(MeshGenerationContext context, Vector2 size)
        {
            MapContourValidation.Require(Texture != null && Margins != null, "Missing map canvas textures.");
            var map = new Rect(Vector2.zero, size);
            Quad(context, Margins, canvas.Around(map));
            // The POT rasters sample their whole rectangle; each quad restores its aspect ratio.
            Quad(context, Texture, map);
            // UI-234: the fortress drawings are part of the terrain, in its board units.
            foreach (var landmark in Landmarks.Items)
                Quad(context, landmark.Texture, landmark.Rect);
        }

        private void Quad(MeshGenerationContext context, Texture2D texture, Rect rect)
        {
            var mesh = context.Allocate(4, 6, texture);
            Set(0, rect.xMin, rect.yMin, 0, 1);
            Set(1, rect.xMax, rect.yMin, 1, 1);
            Set(2, rect.xMax, rect.yMax, 1, 0);
            Set(3, rect.xMin, rect.yMax, 0, 0);
            mesh.SetAllVertices(vertices);
            mesh.SetAllIndices(Indices);
        }

        private void Set(int index, float x, float y, float u, float v)
        {
            vertices[index].position = new Vector3(x, y, Vertex.nearZ);
            vertices[index].tint = Color.white;
            vertices[index].uv = new Vector2(u, v);
        }
    }
}
