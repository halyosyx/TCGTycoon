using System;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Dimensions of a booster pack, in metres: a pillow body between two machine-pressed crimp bands
    /// whose outer edges are zigzag cut. The top band is the strip that comes away when the pack is torn,
    /// split from the body along the tear line. Centred on the origin, front facing −Z.
    /// </summary>
    public readonly struct PackShape
    {
        /// <summary>About 7 × 12 cm, like a real pack.</summary>
        public static readonly PackShape Default = new PackShape(
            width: 0.07f,
            height: 0.12f,
            thickness: 0.007f,
            crimpHeight: 0.009f,
            crimpThickness: 0.0012f,
            teethCount: 14,
            toothDepth: 0.0025f);

        public PackShape(float width, float height, float thickness, float crimpHeight, float crimpThickness, int teethCount, float toothDepth)
        {
            if (width <= 0f || height <= 0f || thickness <= 0f) throw new ArgumentOutOfRangeException(nameof(width), "A pack has a positive size.");
            if (crimpHeight <= toothDepth || crimpHeight * 2f >= height) throw new ArgumentOutOfRangeException(nameof(crimpHeight), "Each crimp band is deeper than its teeth and the two fit inside the pack.");
            if (crimpThickness <= 0f || crimpThickness > thickness) throw new ArgumentOutOfRangeException(nameof(crimpThickness), "The crimp is flattened: thinner than the body.");
            if (teethCount < 1) throw new ArgumentOutOfRangeException(nameof(teethCount), "A crimp has at least one tooth.");
            if (toothDepth < 0f) throw new ArgumentOutOfRangeException(nameof(toothDepth));

            Width = width;
            Height = height;
            Thickness = thickness;
            CrimpHeight = crimpHeight;
            CrimpThickness = crimpThickness;
            TeethCount = teethCount;
            ToothDepth = toothDepth;
        }

        public float Width { get; }

        public float Height { get; }

        /// <summary>The pillow body's depth (front to back).</summary>
        public float Thickness { get; }

        /// <summary>Height of each crimp band, measured to the tips of its teeth.</summary>
        public float CrimpHeight { get; }

        /// <summary>Depth of the flattened crimp bands.</summary>
        public float CrimpThickness { get; }

        /// <summary>Zigzag teeth across each crimp's cut edge.</summary>
        public int TeethCount { get; }

        public float ToothDepth { get; }

        /// <summary>Where the top strip splits from the body (local Y).</summary>
        public float TearLineY => Height * 0.5f - CrimpHeight;
    }
}
