using Game.Unity.Interaction;
using UnityEngine;

namespace Game.Unity.Props
{
    /// <summary>
    /// The floor of the open glass case. E here places the whole held card stack in the case, as many as
    /// fit (<see cref="GlassCase.PlaceHeldCards"/>). Offered only while the lid is open and the hand holds
    /// single cards: a held pack gets no prompt, because sealed product never goes in the case.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GlassCaseInterior : MonoBehaviour, IInteractable
    {
        private GlassCase _case;

        public string PromptVerb => _case.PlaceVerb;

        public string PromptObject => _case.HeldCardsNoun();

        public void Initialize(GlassCase glassCase) => _case = glassCase;

        public bool CanInteract(InteractionContext context) => _case != null && _case.IsLidOpen && _case.HandCards.IsHoldingCards;

        public void SetHovered(bool isHovered)
        {
        }

        public void Interact(InteractionContext context)
        {
            if (CanInteract(context))
            {
                _case.PlaceHeldCards();
            }
        }
    }
}
