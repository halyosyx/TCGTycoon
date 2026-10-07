using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// The generated booster pack model's moving parts for the back-seam opening: two back flaps, each on
    /// a hinge at its side edge; a dark tear line that grows down the seam; and the card stack inside,
    /// which never moves (the first card's face is shown on top of it, <see cref="PlaceRevealCard"/>).
    /// Shows a pose it is given; it decides nothing. Sealed is the rest state, which pooled packs return to.
    /// </summary>
    public sealed class BoosterPackView : MonoBehaviour
    {
        /// <summary>Serialized field names, for the card data generator's prefab builder.</summary>
        public const string BodyRendererField = nameof(_bodyRenderer);
        public const string LeftFlapRendererField = nameof(_leftFlapRenderer);
        public const string RightFlapRendererField = nameof(_rightFlapRenderer);
        public const string LeftHingeField = nameof(_leftHinge);
        public const string RightHingeField = nameof(_rightHinge);
        public const string SeamTearField = nameof(_seamTear);
        public const string CardsField = nameof(_cards);

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _leftFlapRenderer;
        [SerializeField] private Renderer _rightFlapRenderer;

        [SerializeField, Tooltip("Pivot of the left back flap, on the pack's left side edge; it turns about Y.")]
        private Transform _leftHinge;

        [SerializeField, Tooltip("Pivot of the right back flap, on the pack's right side edge; it turns about Y.")]
        private Transform _rightHinge;

        [SerializeField, Tooltip("Pivot at the top of the back seam; its Y scale is how far down the seam has torn.")]
        private Transform _seamTear;

        [SerializeField, Tooltip("The card stack inside the body, hidden until the flaps open.")]
        private Transform _cards;

        // The first card's face sits just proud of the stack, turned to face the back (+Z).
        private const float RevealCardLift = 0.0002f;

        private bool _isReady;

        /// <summary>The wrapper's renderers (body and flaps), which take the set colour; not the cards, seam or label.</summary>
        public void CollectWrapperRenderers(List<Renderer> into)
        {
            if (_bodyRenderer != null) into.Add(_bodyRenderer);
            if (_leftFlapRenderer != null) into.Add(_leftFlapRenderer);
            if (_rightFlapRenderer != null) into.Add(_rightFlapRenderer);
        }

        private void Awake()
        {
            _isReady = _bodyRenderer != null && _leftFlapRenderer != null && _rightFlapRenderer != null
                && _leftHinge != null && _rightHinge != null && _seamTear != null && _cards != null;
            if (!_isReady)
            {
                Debug.LogError($"{name}: {nameof(BoosterPackView)} is missing a part; regenerate it with TCG > Generate Card Data.", this);
            }
        }

        /// <summary>
        /// Puts <paramref name="card"/> (a world card, a child of this pack) on top of the card stack, its face
        /// turned to the back so it is the first thing seen when the flaps open.
        /// </summary>
        public void PlaceRevealCard(Transform card)
        {
            if (!_isReady)
            {
                return;
            }

            Vector3 stackPosition = _cards.localPosition;
            card.localPosition = new Vector3(stackPosition.x, stackPosition.y, stackPosition.z + _cards.localScale.z * 0.5f + RevealCardLift);
            card.localRotation = Quaternion.Euler(0f, 180f, 0f);
            card.localScale = Vector3.one;
        }

        /// <summary>The cards have lifted out to the reveal: the opened wrapper is left empty.</summary>
        public void HideCards()
        {
            if (_isReady && _cards.gameObject.activeSelf)
            {
                _cards.gameObject.SetActive(false);
            }
        }

        /// <summary>Back to a sealed pack: flaps shut, seam whole, cards inside and hidden.</summary>
        public void ResetSealed() => ShowOpen(0f, 0f, areCardsVisible: false);

        /// <param name="seamTear">How far down the back seam has torn, 0 to 1.</param>
        /// <param name="flapAngle">Degrees each flap has opened outward (toward the back) about its side edge.</param>
        public void ShowOpen(float seamTear, float flapAngle, bool areCardsVisible)
        {
            if (!_isReady)
            {
                return;
            }

            // Opening outward means each flap's inner edge swings toward +Z (the back); about Y that is a
            // negative angle on the left hinge and a positive one on the right.
            _leftHinge.localRotation = Quaternion.Euler(0f, -flapAngle, 0f);
            _rightHinge.localRotation = Quaternion.Euler(0f, flapAngle, 0f);

            // The tear line shows the seam tearing; once the flaps swing apart there is no seam left.
            bool isTorn = seamTear > 0f && flapAngle <= 0f;
            if (_seamTear.gameObject.activeSelf != isTorn)
            {
                _seamTear.gameObject.SetActive(isTorn);
            }

            _seamTear.localScale = new Vector3(1f, Mathf.Max(seamTear, 0.0001f), 1f);

            if (_cards.gameObject.activeSelf != areCardsVisible)
            {
                _cards.gameObject.SetActive(areCardsVisible);
            }
        }
    }
}
