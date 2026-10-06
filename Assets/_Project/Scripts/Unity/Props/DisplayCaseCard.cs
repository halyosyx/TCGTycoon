using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using Game.Unity.Interaction;
using UnityEngine;

namespace Game.Unity.Props
{
    /// <summary>
    /// One card shown in the glass case: a pooled world card bound to a copy Core says is in the case.
    /// While the lid is open, E takes it back into the hand (<see cref="GlassCase.TakeCard"/>), if the hand
    /// is empty or holds a card stack with room. It rises slightly while aimed at.
    /// </summary>
    public sealed class DisplayCaseCard : MonoBehaviour, IInteractable
    {
        private GlassCase _case;
        private WorldCardView _face;
        private ItemRef _item;
        private string _name;
        private Vector3 _restPosition;

        public ItemRef Item => _item;

        public string PromptVerb => _case.TakeVerb;

        public string PromptObject => _name;

        public void Initialize(GlassCase glassCase, WorldCardView face)
        {
            _case = glassCase;
            _face = face;
        }

        public void Bind(ItemRef item, Card card, RarityPaletteDefinition palette, Vector3 slotPosition)
        {
            _item = item;
            _name = card.DisplayName;
            _restPosition = slotPosition;
            transform.localPosition = slotPosition;
            _face.Show(card, palette);
        }

        public bool CanInteract(InteractionContext context) => _case != null && _case.IsLidOpen && _case.HandCards.CanTakeMore;

        public void SetHovered(bool isHovered)
        {
            transform.localPosition = _restPosition + (isHovered ? Vector3.up * _case.HoverLift : Vector3.zero);
        }

        public void Interact(InteractionContext context)
        {
            if (CanInteract(context))
            {
                _case.TakeCard(_item);
            }
        }
    }
}
