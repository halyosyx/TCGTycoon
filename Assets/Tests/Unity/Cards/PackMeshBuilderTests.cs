using Game.Unity.Cards;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests.Cards
{
    public sealed class PackMeshBuilderTests
    {
        private const float Epsilon = 0.00001f;

        private Mesh _body;
        private Mesh _strip;

        [SetUp]
        public void SetUp()
        {
            _body = PackMeshBuilder.BuildBody(PackShape.Default);
            _strip = PackMeshBuilder.BuildTopStrip(PackShape.Default);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_body);
            Object.DestroyImmediate(_strip);
        }

        [Test]
        public void Default_RealPackProportions()
        {
            PackShape shape = PackShape.Default;

            Assert.That(shape.Width, Is.EqualTo(0.07f).Within(0.005f));
            Assert.That(shape.Height, Is.EqualTo(0.12f).Within(0.005f));
        }

        [Test]
        public void BodyAndStrip_TogetherSpanTheWholePack()
        {
            PackShape shape = PackShape.Default;
            Bounds bounds = _body.bounds;
            bounds.Encapsulate(_strip.bounds);

            Assert.That(bounds.size.x, Is.EqualTo(shape.Width).Within(Epsilon));
            Assert.That(bounds.size.y, Is.EqualTo(shape.Height).Within(Epsilon));
            Assert.That(bounds.size.z, Is.EqualTo(shape.Thickness).Within(Epsilon));
            Assert.That(bounds.center.sqrMagnitude, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void Split_StripSitsAboveTheTearLineAndBodyBelowIt()
        {
            PackShape shape = PackShape.Default;

            Assert.That(_strip.bounds.min.y, Is.EqualTo(shape.TearLineY).Within(Epsilon));
            Assert.That(_body.bounds.max.y, Is.EqualTo(shape.TearLineY).Within(Epsilon));
            Assert.That(_strip.bounds.size.z, Is.EqualTo(shape.CrimpThickness).Within(Epsilon));
        }

        [Test]
        public void Strip_TopEdgeIsAZigzagInTheSilhouette()
        {
            PackShape shape = PackShape.Default;
            float peakY = shape.Height * 0.5f;
            float valleyY = peakY - shape.ToothDepth;

            int peaks = CountDistinctX(_strip, peakY);
            int valleys = CountDistinctX(_strip, valleyY);

            Assert.That(peaks, Is.EqualTo(shape.TeethCount + 1));
            Assert.That(valleys, Is.EqualTo(shape.TeethCount));
        }

        [Test]
        public void Body_BottomEdgeIsAZigzagInTheSilhouette()
        {
            PackShape shape = PackShape.Default;
            float peakY = -shape.Height * 0.5f;

            Assert.That(CountDistinctX(_body, peakY), Is.EqualTo(shape.TeethCount + 1));
            Assert.That(CountDistinctX(_body, peakY + shape.ToothDepth), Is.GreaterThanOrEqualTo(shape.TeethCount));
        }

        [Test]
        public void Meshes_HaveNormalsAndValidTriangles()
        {
            foreach (Mesh mesh in new[] { _body, _strip })
            {
                Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0));
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
            }
        }

        // Distinct x positions of vertices at height y (each corner is duplicated per face for flat normals).
        private static int CountDistinctX(Mesh mesh, float y)
        {
            var xs = new System.Collections.Generic.List<float>();
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (Mathf.Abs(vertex.y - y) > Epsilon)
                {
                    continue;
                }

                bool isKnown = false;
                foreach (float x in xs)
                {
                    if (Mathf.Abs(x - vertex.x) < Epsilon)
                    {
                        isKnown = true;
                        break;
                    }
                }

                if (!isKnown)
                {
                    xs.Add(vertex.x);
                }
            }

            return xs.Count;
        }
    }
}
