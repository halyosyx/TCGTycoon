using Game.Unity.Cards;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests.Cards
{
    /// <summary>
    /// F2e back-seam model: a front body (front half-shell and both zigzag crimps) and two back flaps
    /// that meet at the seam down the centre of the back.
    /// </summary>
    public sealed class PackMeshBuilderTests
    {
        private const float Epsilon = 0.00001f;

        private Mesh _body;
        private Mesh _left;
        private Mesh _right;

        [SetUp]
        public void SetUp()
        {
            _body = PackMeshBuilder.BuildBody(PackShape.Default);
            _left = PackMeshBuilder.BuildBackFlap(PackShape.Default, isLeft: true);
            _right = PackMeshBuilder.BuildBackFlap(PackShape.Default, isLeft: false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_body);
            Object.DestroyImmediate(_left);
            Object.DestroyImmediate(_right);
        }

        [Test]
        public void Default_RealPackProportions()
        {
            PackShape shape = PackShape.Default;

            Assert.That(shape.Width, Is.EqualTo(0.07f).Within(0.005f));
            Assert.That(shape.Height, Is.EqualTo(0.12f).Within(0.005f));
        }

        [Test]
        public void Pieces_TogetherSpanTheWholePack()
        {
            PackShape shape = PackShape.Default;
            Bounds bounds = _body.bounds;
            bounds.Encapsulate(_left.bounds);
            bounds.Encapsulate(_right.bounds);

            Assert.That(bounds.size.x, Is.EqualTo(shape.Width).Within(Epsilon));
            Assert.That(bounds.size.y, Is.EqualTo(shape.Height).Within(Epsilon));
            Assert.That(bounds.min.z, Is.EqualTo(-shape.Thickness * 0.5f).Within(Epsilon), "Front face");
            Assert.That(bounds.max.z, Is.EqualTo(shape.Thickness * 0.5f + shape.SeamLipHeight).Within(Epsilon), "Back, with the seam lip");
        }

        [Test]
        public void Flaps_MeetAtTheSeamDownTheMiddleOfTheBack()
        {
            Assert.That(_left.bounds.max.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(_right.bounds.min.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(_left.bounds.min.z, Is.EqualTo(0f).Within(Epsilon), "Flaps cover the back half.");
            Assert.That(_body.bounds.max.z, Is.LessThan(PackShape.Default.Thickness * 0.5f), "The body holds the front half.");
        }

        [Test]
        public void Flaps_SitBetweenTheCrimps()
        {
            PackShape shape = PackShape.Default;
            float crimpLine = shape.Height * 0.5f - shape.CrimpHeight;

            foreach (Mesh flap in new[] { _left, _right })
            {
                Assert.That(flap.bounds.max.y, Is.EqualTo(crimpLine).Within(Epsilon));
                Assert.That(flap.bounds.min.y, Is.EqualTo(-crimpLine).Within(Epsilon));
            }
        }

        [Test]
        public void Flaps_HaveAFinSeamLipOnTheirInnerEdge()
        {
            PackShape shape = PackShape.Default;
            float lipTop = shape.Thickness * 0.5f + shape.SeamLipHeight;

            Assert.That(CountDistinctX(_left, lipTop, zAxis: true), Is.GreaterThan(0), "The lip stands proud of the back.");
            Assert.That(MaxXAtZ(_left, lipTop), Is.EqualTo(0f).Within(Epsilon));
            Assert.That(MinXAtZ(_left, lipTop), Is.EqualTo(-shape.SeamLipWidth).Within(Epsilon));
        }

        [Test]
        public void Body_ZigzagCrimpsTopAndBottomInTheSilhouette()
        {
            PackShape shape = PackShape.Default;
            float peakY = shape.Height * 0.5f;

            Assert.That(CountDistinctX(_body, peakY, zAxis: false), Is.EqualTo(shape.TeethCount + 1), "Top teeth");
            Assert.That(CountDistinctX(_body, peakY - shape.ToothDepth, zAxis: false), Is.EqualTo(shape.TeethCount), "Top valleys");
            Assert.That(CountDistinctX(_body, -peakY, zAxis: false), Is.EqualTo(shape.TeethCount + 1), "Bottom teeth");
        }

        [Test]
        public void Meshes_HaveNormalsAndValidTriangles()
        {
            foreach (Mesh mesh in new[] { _body, _left, _right })
            {
                Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0));
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0));
            }
        }

        // Distinct x positions of vertices at height y (or at depth z), each corner duplicated per face.
        private static int CountDistinctX(Mesh mesh, float value, bool zAxis)
        {
            var xs = new System.Collections.Generic.List<float>();
            foreach (Vector3 vertex in mesh.vertices)
            {
                float coordinate = zAxis ? vertex.z : vertex.y;
                if (Mathf.Abs(coordinate - value) > Epsilon)
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

        private static float MaxXAtZ(Mesh mesh, float z)
        {
            float max = float.MinValue;
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (Mathf.Abs(vertex.z - z) < Epsilon) max = Mathf.Max(max, vertex.x);
            }

            return max;
        }

        private static float MinXAtZ(Mesh mesh, float z)
        {
            float min = float.MaxValue;
            foreach (Vector3 vertex in mesh.vertices)
            {
                if (Mathf.Abs(vertex.z - z) < Epsilon) min = Mathf.Min(min, vertex.x);
            }

            return min;
        }
    }
}
