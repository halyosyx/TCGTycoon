using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Builds a booster pack's two meshes from a <see cref="PackShape"/>: the body (pillow plus the bottom
    /// crimp) and the top strip (the top crimp), split along the tear line. The zigzag is real geometry,
    /// so it reads in the silhouette at hand distance. Every face has its own vertices, so normals are
    /// flat, matching the flat-colour art style. Used by the card data generator, which saves the meshes
    /// as assets.
    /// </summary>
    public static class PackMeshBuilder
    {
        public static Mesh BuildBody(PackShape shape)
        {
            var builder = new MeshData();
            float halfWidth = shape.Width * 0.5f;
            float halfHeight = shape.Height * 0.5f;

            // Pillow: a box between the two crimps. Its straight top is the tear line.
            builder.AddBand(-halfWidth, halfWidth, -halfHeight + shape.CrimpHeight, shape.TearLineY, teeth: 1, toothDepth: 0f, areTeethOnTop: true, shape.Thickness);

            // Bottom crimp: straight on top where it meets the pillow, zigzag cut edge below.
            builder.AddBand(-halfWidth, halfWidth, -halfHeight, -halfHeight + shape.CrimpHeight, shape.TeethCount, shape.ToothDepth, areTeethOnTop: false, shape.CrimpThickness);
            return builder.ToMesh("BoosterPackBody");
        }

        public static Mesh BuildTopStrip(PackShape shape)
        {
            var builder = new MeshData();
            float halfWidth = shape.Width * 0.5f;

            // Top crimp: straight along the tear line, zigzag cut edge on top.
            builder.AddBand(-halfWidth, halfWidth, shape.TearLineY, shape.Height * 0.5f, shape.TeethCount, shape.ToothDepth, areTeethOnTop: true, shape.CrimpThickness);
            return builder.ToMesh("BoosterPackTopStrip");
        }

        private sealed class MeshData
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            /// <summary>
            /// A slab from <paramref name="bottom"/> to <paramref name="top"/>, centred on z = 0, with one
            /// edge cut into <paramref name="teeth"/> zigzag teeth: tips at the outer edge, valleys
            /// <paramref name="toothDepth"/> inside it.
            /// </summary>
            public void AddBand(float left, float right, float bottom, float top, int teeth, float toothDepth, bool areTeethOnTop, float thickness)
            {
                int pointCount = teeth * 2 + 1;
                var upper = new Vector2[pointCount];
                var lower = new Vector2[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    float x = Mathf.Lerp(left, right, i / (float)(pointCount - 1));
                    bool isValley = i % 2 == 1;
                    float upperY = areTeethOnTop && isValley ? top - toothDepth : top;
                    float lowerY = !areTeethOnTop && isValley ? bottom + toothDepth : bottom;
                    upper[i] = new Vector2(x, upperY);
                    lower[i] = new Vector2(x, lowerY);
                }

                float front = -thickness * 0.5f;
                float back = thickness * 0.5f;
                for (int i = 0; i < pointCount - 1; i++)
                {
                    // Front faces −Z (clockwise seen from the front), back faces +Z.
                    AddQuad(At(lower[i], front), At(upper[i], front), At(upper[i + 1], front), At(lower[i + 1], front));
                    AddQuad(At(lower[i + 1], back), At(upper[i + 1], back), At(upper[i], back), At(lower[i], back));
                }

                // The rim, walked clockwise as seen from the front so every face points outward.
                for (int i = 0; i < pointCount - 1; i++)
                {
                    AddRim(upper[i], upper[i + 1], front, back);
                    AddRim(lower[i + 1], lower[i], front, back);
                }

                AddRim(upper[pointCount - 1], lower[pointCount - 1], front, back);
                AddRim(lower[0], upper[0], front, back);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(_vertices);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }

            private void AddRim(Vector2 from, Vector2 to, float front, float back)
            {
                AddQuad(At(from, front), At(from, back), At(to, back), At(to, front));
            }

            private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
                _triangles.Add(start);
                _triangles.Add(start + 2);
                _triangles.Add(start + 3);
            }

            private static Vector3 At(Vector2 point, float z) => new Vector3(point.x, point.y, z);
        }
    }
}
