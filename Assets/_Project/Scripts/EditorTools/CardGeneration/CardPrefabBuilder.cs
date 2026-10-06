using Game.Core.Content;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Builds the booster pack and world card prefabs into a root GameObject, either a fresh one or
    /// loaded prefab contents. Every property is set only when it differs and changes are reported
    /// back, so rebuilding an up-to-date prefab changes nothing and it isn't re-saved.
    /// </summary>
    internal static class CardPrefabBuilder
    {
        public const string PackLabelText = "Mythbound Booster Pack";

        // Real-world sizes in metres: a trading card is 63 x 88 mm; the booster pack is PackShape.Default.
        private static readonly Vector2 s_cardSize = new Vector2(0.063f, 0.088f);

        // The face-down card stack inside a pack: seven cards' depth, sitting just under the tear line.
        private const float PackCardsDepth = 0.004f;
        private const float PackCardsGap = 0.002f;
        private const float CardFrameWidth = 0.0035f;
        private const float TextMargin = 0.0055f;

        // Offsets toward the viewer (-Z, the side a Quad faces) keep layers from z-fighting.
        private const float FaceOffset = -0.0005f;
        private const float TextOffset = -0.001f;
        private const float PackLabelOffset = -0.0005f;

        // Labels are built at 1/100 scale with font and rect sizes ×100. TextMesh Pro's auto-sizing uses
        // fixed step thresholds tuned for point-sized text; at raw metre-scale font sizes it silently
        // stops resizing. The values below are world sizes, converted in ChangeTracker.Text.
        private const float TextScale = 0.01f;

        // Font sizes as world size at scale 1: one unit is about 0.11 m of text height (measured), so 0.055 ≈ 6 mm.
        private const float CardNameMaxFontSize = 0.055f;
        private const float CardNameMinFontSize = 0.03f;
        // The tier name is the point of the placeholder pass, so it's nearly as large as the card name.
        private const float CardTierMaxFontSize = 0.044f;
        private const float CardTierMinFontSize = 0.026f;
        private const float CardIdFontSize = 0.02f;
        private const float PackLabelMaxFontSize = 0.075f;
        private const float PackLabelMinFontSize = 0.04f;

        // Vertical layout of the card face, in metres from the centre.
        private const float CardNameY = 0.008f;
        private const float CardNameHeight = 0.034f;
        private const float CardTierY = -0.021f;
        private const float CardTierHeight = 0.012f;
        private const float CardIdY = 0.036f;
        private const float CardIdHeight = 0.006f;
        private const float PackLabelHeightShare = 0.5f;

        /// <summary>
        /// The pack: a body and a top strip (two meshes split along the crimp, the strip on a hinge at the
        /// back seam so it can be torn away), the face-down cards inside, the printed label, and a
        /// <see cref="BoosterPackView"/> that animates them.
        /// </summary>
        public static bool BuildBoosterPack(GameObject root, RarityPaletteDefinition palette, PackShape shape, Mesh bodyMesh, Mesh stripMesh)
        {
            var tracker = new ChangeTracker();

            // The single-cube pack from before F2d.
            tracker.RemoveChild(root.transform, "Box");

            Transform body = tracker.Child(root.transform, "Body", null);
            tracker.Place(body, Vector3.zero, Vector3.one);
            Renderer bodyRenderer = tracker.MeshPart(body.gameObject, bodyMesh, palette.PackMaterial);

            // The hinge sits on the back seam at the tear line; the strip mesh is in pack space, so it is
            // offset back by the hinge's position.
            var hingePosition = new Vector3(0f, shape.TearLineY, shape.CrimpThickness * 0.5f);
            Transform hinge = tracker.Child(root.transform, "TopStripHinge", null);
            tracker.Place(hinge, hingePosition, Vector3.one);
            Transform strip = tracker.Child(hinge, "TopStrip", null);
            tracker.Place(strip, -hingePosition, Vector3.one);
            Renderer stripRenderer = tracker.MeshPart(strip.gameObject, stripMesh, palette.PackMaterial);

            Transform cards = tracker.Child(root.transform, "Cards", PrimitiveType.Cube);
            tracker.Remove<BoxCollider>(cards.gameObject);
            float cardsCentreY = shape.TearLineY - PackCardsGap - s_cardSize.y * 0.5f;
            tracker.Place(cards, new Vector3(0f, cardsCentreY, 0f), new Vector3(s_cardSize.x, s_cardSize.y, PackCardsDepth));
            tracker.Material(cards.GetComponent<Renderer>(), palette.CardFaceMaterial);
            tracker.Active(cards.gameObject, false);

            Transform label = tracker.Child(root.transform, "Label", null);
            tracker.Place(label, new Vector3(0f, 0f, -shape.Thickness / 2f + PackLabelOffset), Vector3.one * TextScale);
            tracker.Text(
                tracker.Component<TextMeshPro>(label.gameObject),
                new TextSpec(PackLabelText, PackLabelMaxFontSize, PackLabelMinFontSize, palette.PackLabelColor, FontStyles.Bold, TextAlignmentOptions.Center,
                    new Vector2(shape.Width - TextMargin * 2f, shape.Height * PackLabelHeightShare)));

            BoosterPackView view = tracker.Component<BoosterPackView>(root);
            tracker.Reference(view, BoosterPackView.BodyRendererField, bodyRenderer);
            tracker.Reference(view, BoosterPackView.StripRendererField, stripRenderer);
            tracker.Reference(view, BoosterPackView.StripHingeField, hinge);
            tracker.Reference(view, BoosterPackView.CardsField, cards);

            return tracker.Changed;
        }

        public static bool BuildWorldCard(GameObject root, RarityPaletteDefinition palette)
        {
            var tracker = new ChangeTracker();
            WorldCardView view = tracker.Component<WorldCardView>(root);
            float textWidth = s_cardSize.x - TextMargin * 2f;

            // Frame: the full card in the tier colour. It keeps its collider so cards can be clicked later.
            Transform frame = tracker.Child(root.transform, "Frame", PrimitiveType.Quad);
            tracker.Place(frame, Vector3.zero, new Vector3(s_cardSize.x, s_cardSize.y, 1f));
            tracker.Material(frame.GetComponent<Renderer>(), palette.MaterialOf(RarityTier.Common));

            // Face: a dark panel inset by the frame width, so the tier colour reads as a border.
            Transform face = tracker.Child(root.transform, "Face", PrimitiveType.Quad);
            tracker.Place(face, new Vector3(0f, 0f, FaceOffset), new Vector3(s_cardSize.x - CardFrameWidth * 2f, s_cardSize.y - CardFrameWidth * 2f, 1f));
            tracker.Material(face.GetComponent<Renderer>(), palette.CardFaceMaterial);
            tracker.Remove<MeshCollider>(face.gameObject);

            TextMeshPro nameText = AddLabel(tracker, root.transform, "Name", CardNameY,
                new TextSpec("Card Name", CardNameMaxFontSize, CardNameMinFontSize, palette.CardTextColor, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(textWidth, CardNameHeight)));
            TextMeshPro tierText = AddLabel(tracker, root.transform, "Tier", CardTierY,
                new TextSpec(palette.DisplayNameOf(RarityTier.Common), CardTierMaxFontSize, CardTierMinFontSize, palette.ColorOf(RarityTier.Common), FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(textWidth, CardTierHeight)));
            TextMeshPro idText = AddLabel(tracker, root.transform, "Id", CardIdY,
                new TextSpec("SetA_Common_01", CardIdFontSize, palette.CardTextColor, FontStyles.Normal, TextAlignmentOptions.TopRight, new Vector2(textWidth, CardIdHeight)));

            tracker.Reference(view, WorldCardView.FrameRendererField, frame.GetComponent<Renderer>());
            tracker.Reference(view, WorldCardView.NameLabelField, nameText);
            tracker.Reference(view, WorldCardView.TierLabelField, tierText);
            tracker.Reference(view, WorldCardView.IdLabelField, idText);

            return tracker.Changed;
        }

        private static TextMeshPro AddLabel(ChangeTracker tracker, Transform parent, string name, float y, TextSpec spec)
        {
            Transform label = tracker.Child(parent, name, null);
            tracker.Place(label, new Vector3(0f, y, TextOffset), Vector3.one * TextScale);
            TextMeshPro text = tracker.Component<TextMeshPro>(label.gameObject);
            tracker.Text(text, spec);
            return text;
        }

        /// <summary>What a label should look like. A min font size below the max turns on auto-sizing.</summary>
        private readonly struct TextSpec
        {
            public TextSpec(string text, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment, Vector2 size)
                : this(text, fontSize, fontSize, color, style, alignment, size)
            {
            }

            public TextSpec(string text, float maxFontSize, float minFontSize, Color color, FontStyles style, TextAlignmentOptions alignment, Vector2 size)
            {
                Text = text;
                MaxFontSize = maxFontSize;
                MinFontSize = minFontSize;
                Color = color;
                Style = style;
                Alignment = alignment;
                Size = size;
            }

            public string Text { get; }
            public float MaxFontSize { get; }
            public float MinFontSize { get; }
            public Color Color { get; }
            public FontStyles Style { get; }
            public TextAlignmentOptions Alignment { get; }
            public Vector2 Size { get; }
            public bool AutoSize => MinFontSize < MaxFontSize;
        }

        /// <summary>Applies values only when they differ, remembering whether anything changed.</summary>
        private sealed class ChangeTracker
        {
            public bool Changed { get; private set; }

            public Transform Child(Transform parent, string name, PrimitiveType? primitive)
            {
                Transform existing = parent.Find(name);
                if (existing != null)
                {
                    return existing;
                }

                GameObject child = primitive.HasValue ? GameObject.CreatePrimitive(primitive.Value) : new GameObject(name);
                child.name = name;
                child.transform.SetParent(parent, false);
                Changed = true;
                return child.transform;
            }

            public void Place(Transform target, Vector3 localPosition, Vector3 localScale)
            {
                if (target.localPosition != localPosition)
                {
                    target.localPosition = localPosition;
                    Changed = true;
                }

                if (target.localRotation != Quaternion.identity)
                {
                    target.localRotation = Quaternion.identity;
                    Changed = true;
                }

                if (target.localScale != localScale)
                {
                    target.localScale = localScale;
                    Changed = true;
                }
            }

            public TComponent Component<TComponent>(GameObject target)
                where TComponent : Component
            {
                TComponent component = target.GetComponent<TComponent>();
                if (component == null)
                {
                    component = target.AddComponent<TComponent>();
                    Changed = true;
                }

                return component;
            }

            public void RemoveChild(Transform parent, string name)
            {
                Transform existing = parent.Find(name);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                    Changed = true;
                }
            }

            /// <summary>A mesh with one material: MeshFilter + MeshRenderer, set only where they differ.</summary>
            public Renderer MeshPart(GameObject target, Mesh mesh, Material material)
            {
                MeshFilter filter = Component<MeshFilter>(target);
                if (filter.sharedMesh != mesh)
                {
                    filter.sharedMesh = mesh;
                    Changed = true;
                }

                MeshRenderer renderer = Component<MeshRenderer>(target);
                Material(renderer, material);
                return renderer;
            }

            public void Active(GameObject target, bool isActive)
            {
                if (target.activeSelf != isActive)
                {
                    target.SetActive(isActive);
                    Changed = true;
                }
            }

            public void Remove<TComponent>(GameObject target)
                where TComponent : Component
            {
                TComponent component = target.GetComponent<TComponent>();
                if (component != null)
                {
                    Object.DestroyImmediate(component);
                    Changed = true;
                }
            }

            public void Material(Renderer renderer, Material material)
            {
                if (renderer.sharedMaterial != material)
                {
                    renderer.sharedMaterial = material;
                    Changed = true;
                }
            }

            public void Reference(Object target, string fieldName, Object value)
            {
                var serialized = new SerializedObject(target);
                SerializedProperty property = serialized.FindProperty(fieldName);
                if (property.objectReferenceValue != value)
                {
                    property.objectReferenceValue = value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Changed = true;
                }
            }

            /// <summary>Applies a spec given in world sizes to a label built at <see cref="TextScale"/>.</summary>
            public void Text(TextMeshPro text, TextSpec spec)
            {
                float maxFontSize = spec.MaxFontSize / TextScale;
                float minFontSize = spec.MinFontSize / TextScale;
                Vector2 size = spec.Size / TextScale;

                if (text.text != spec.Text) { text.text = spec.Text; Changed = true; }
                if (text.enableAutoSizing != spec.AutoSize) { text.enableAutoSizing = spec.AutoSize; Changed = true; }

                if (spec.AutoSize)
                {
                    if (!Mathf.Approximately(text.fontSizeMax, maxFontSize)) { text.fontSizeMax = maxFontSize; Changed = true; }
                    if (!Mathf.Approximately(text.fontSizeMin, minFontSize)) { text.fontSizeMin = minFontSize; Changed = true; }
                }

                // With auto-sizing on this is only the starting size; keeping it at the max avoids a stale value.
                if (!Mathf.Approximately(text.fontSize, maxFontSize)) { text.fontSize = maxFontSize; Changed = true; }

                if (text.color != spec.Color) { text.color = spec.Color; Changed = true; }
                if (text.fontStyle != spec.Style) { text.fontStyle = spec.Style; Changed = true; }
                if (text.alignment != spec.Alignment) { text.alignment = spec.Alignment; Changed = true; }
                if (text.textWrappingMode != TextWrappingModes.Normal) { text.textWrappingMode = TextWrappingModes.Normal; Changed = true; }
                if (text.rectTransform.sizeDelta != size) { text.rectTransform.sizeDelta = size; Changed = true; }
            }
        }
    }
}
