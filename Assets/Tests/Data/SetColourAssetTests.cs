using System.Collections.Generic;
using Game.Unity.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Tests
{
    /// <summary>
    /// Enforces the set colour rule (CARD_DATA_AND_SETS §2) on the real sets: a set colour is used as a
    /// swatch on the light store pages and in the dark game UI, and carries white glyphs, so it must
    /// keep a 3:1 contrast (WCAG, graphics and large text) against all three. Sets must also differ from
    /// each other in hue, so two sets never read as one.
    /// </summary>
    public sealed class SetColourAssetTests
    {
        private const string GeneratedFolder = "Assets/_Project/Data/Generated";
        private const double MinimumContrast = 3.0;
        private const float MinimumHueGapDegrees = 60f;

        // The store's thumbnail background (--store-thumb) and the kit's dark surface (--color-surface).
        private static readonly Color s_lightBackground = Hex("#E6EEF7");
        private static readonly Color s_darkBackground = Hex("#3A3C40");

        private List<CardSetDefinition> _sets;

        [SetUp]
        public void LoadSets()
        {
            _sets = new List<CardSetDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CardSetDefinition), new[] { GeneratedFolder }))
            {
                _sets.Add(AssetDatabase.LoadAssetAtPath<CardSetDefinition>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            Assert.That(_sets, Is.Not.Empty, $"No card sets under {GeneratedFolder}.");
        }

        [Test]
        public void SetColour_EverySet_ReadsOnLightAndDarkAndUnderWhite()
        {
            foreach (CardSetDefinition set in _sets)
            {
                Assert.That(Contrast(set.Colour, s_lightBackground), Is.GreaterThanOrEqualTo(MinimumContrast), $"{set.Id} on the light store page");
                Assert.That(Contrast(set.Colour, s_darkBackground), Is.GreaterThanOrEqualTo(MinimumContrast), $"{set.Id} on the dark game UI");
                Assert.That(Contrast(set.Colour, Color.white), Is.GreaterThanOrEqualTo(MinimumContrast), $"{set.Id} under white glyphs");
            }
        }

        [Test]
        public void SetColour_TwoSets_FarApartInHue()
        {
            for (int i = 0; i < _sets.Count; i++)
            {
                for (int j = i + 1; j < _sets.Count; j++)
                {
                    Color.RGBToHSV(_sets[i].Colour, out float hueA, out _, out _);
                    Color.RGBToHSV(_sets[j].Colour, out float hueB, out _, out _);
                    float gap = Mathf.Abs(hueA - hueB) * 360f;
                    gap = Mathf.Min(gap, 360f - gap);
                    Assert.That(gap, Is.GreaterThanOrEqualTo(MinimumHueGapDegrees), $"{_sets[i].Id} and {_sets[j].Id}");
                }
            }
        }

        // WCAG 2 contrast ratio from relative luminance.
        private static double Contrast(Color a, Color b)
        {
            double la = Luminance(a);
            double lb = Luminance(b);
            return (System.Math.Max(la, lb) + 0.05) / (System.Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color colour)
        {
            return 0.2126 * Linear(colour.r) + 0.7152 * Linear(colour.g) + 0.0722 * Linear(colour.b);
        }

        private static double Linear(float channel)
        {
            return channel <= 0.04045 ? channel / 12.92 : System.Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color colour);
            return colour;
        }
    }
}
