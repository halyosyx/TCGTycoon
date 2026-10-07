using System;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Dimensions of a booster pack, in metres: a pillow body between two machine-pressed crimp bands
    /// whose outer edges are zigzag cut, and a heat-sealed fin seam down the middle of the back, which is
    /// where the pack is torn open. Centred on the origin, front facing −Z, back facing +Z.
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
            toothDepth: 0.0025f,
            seamLipWidth: 0.003f,
            seamLipHeight: 0.0012f);

        public PackShape(float width, float height, float thickness, float crimpHeight, float crimpThickness, int teethCount, float toothDepth, float seamLipWidth, float seamLipHeight)
        {
            if (seamLipWidth < 0f || seamLipWidth * 2f >= width) throw new ArgumentOutOfRangeException(nameof(seamLipWidth), "The seam lip is narrower than half the pack.");
            if (seamLipHeight < 0f) throw new ArgumentOutOfRangeException(nameof(seamLipHeight));
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
            SeamLipWidth = seamLipWidth;
            SeamLipHeight = seamLipHeight;
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

        /// <summary>Width of each half of the fin seam, on the inner edge of each back flap.</summary>
        public float SeamLipWidth { get; }

        /// <summary>How far the fin seam stands proud of the back.</summary>
        public float SeamLipHeight { get; }

        /// <summary>Where the top crimp band meets the pillow (local Y); the bottom one is at its negative.</summary>
        public float CrimpLineY => Height * 0.5f - CrimpHeight;
    }
}
