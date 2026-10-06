using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// The generated booster pack model's moving parts: the top strip on a hinge at its back bottom edge
    /// (the back seam), and the face-down card stack inside the body. Shows a tear pose it is given; it
    /// decides nothing. Sealed is the rest state, which pooled packs return to.
    /// </summary>
    public sealed class BoosterPackView : MonoBehaviour
    {
        /// <summary>Serialized field names, for the card data generator's prefab builder.</summary>
        public const string BodyRendererField = nameof(_bodyRenderer);
        public const string StripRendererField = nameof(_stripRenderer);
        public const string StripHingeField = nameof(_stripHinge);
        public const string CardsField = nameof(_cards);

        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Renderer _stripRenderer;

        [SerializeField, Tooltip("Pivot of the top strip, on the back seam at the tear line.")]
        private Transform _stripHinge;

        [SerializeField, Tooltip("The face-down card stack inside the body, hidden until the crimp splits.")]
        private Transform _cards;

        private Vector3 _hingeRestPosition;
        private Vector3 _cardsRestPosition;

        /// <summary>The wrapper's renderers (body and strip), which take the set colour; not the cards or the label.</summary>
        public void CollectWrapperRenderers(List<Renderer> into)
        {
            if (_bodyRenderer != null) into.Add(_bodyRenderer);
            if (_stripRenderer != null) into.Add(_stripRenderer);
        }

        private void Awake()
        {
            if (_bodyRenderer == null || _stripRenderer == null || _stripHinge == null || _cards == null)
            {
                Debug.LogError($"{name}: {nameof(BoosterPackView)} is missing a part; regenerate it with TCG > Generate Card Data.", this);
                enabled = false;
                return;
            }

            _hingeRestPosition = _stripHinge.localPosition;
            _cardsRestPosition = _cards.localPosition;
        }

        /// <summary>Back to a sealed pack: strip on, cards inside and hidden.</summary>
        public void ResetSealed()
        {
            if (!enabled)
            {
                return;
            }

            ShowTear(0f, Vector3.zero, isStripVisible: true, areCardsVisible: false, cardsRise: 0f);
        }

        /// <param name="stripAngle">Degrees the strip has hinged back off the back seam.</param>
        /// <param name="stripOffset">The strip's offset from its sealed place, in pack space.</param>
        /// <param name="cardsRise">Metres the card stack has risen out of the top.</param>
        public void ShowTear(float stripAngle, Vector3 stripOffset, bool isStripVisible, bool areCardsVisible, float cardsRise)
        {
            if (!enabled)
            {
                return;
            }

            _stripHinge.localPosition = _hingeRestPosition + stripOffset;
            _stripHinge.localRotation = Quaternion.Euler(stripAngle, 0f, 0f);
            if (_stripHinge.gameObject.activeSelf != isStripVisible)
            {
                _stripHinge.gameObject.SetActive(isStripVisible);
            }

            _cards.localPosition = _cardsRestPosition + Vector3.up * cardsRise;
            if (_cards.gameObject.activeSelf != areCardsVisible)
            {
                _cards.gameObject.SetActive(areCardsVisible);
            }
        }
    }
}
