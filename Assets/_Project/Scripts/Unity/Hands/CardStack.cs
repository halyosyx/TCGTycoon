using System.Collections.Generic;
using System.Globalization;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Cards;
using TMPro;
using UnityEngine;

namespace Game.Unity.Hands
{
    /// <summary>
    /// The single cards in the player's hand, 1 to 10 (Core: <see cref="ItemLocation.Held"/>). It holds
    /// no list of its own: it mirrors the cards Core says are held and shows the most recent on top.
    /// Put down returns every held card to the binder; single cards never exist as loose world objects,
    /// so a pulled card can never be lost. A count beside it shows how many are held. It has no use (LMB):
    /// the display case takes it with E on the open case.
    /// </summary>
    public sealed class CardStack : MonoBehaviour, IHoldable
    {
        private readonly List<ItemRef> _returning = new List<ItemRef>();
        private readonly List<int> _returningCounts = new List<int>();

        private HoldableFactory _factory;
        private WorldCardView _face;
        private TMP_Text _count;
        private bool _isHeld;

        public string Noun => string.Format(CultureInfo.InvariantCulture, _factory.CardsNounFormat, Count);

        public int Count => _factory.Inventory.CountIn(ItemLocation.Held);

        public string UseVerb => string.Empty;

        public bool CanUse => false;

        public string DropVerb => _factory.ReturnVerb;

        public void Initialize(HoldableFactory factory, WorldCardView face, TMP_Text count)
        {
            _factory = factory;
            _face = face;
            _count = count;
        }

        private void OnEnable()
        {
            if (_factory != null && _factory.Inventory != null)
            {
                _factory.Inventory.Changed += Refresh;
            }
        }

        private void OnDisable()
        {
            if (_factory != null && _factory.Inventory != null)
            {
                _factory.Inventory.Changed -= Refresh;
            }

            _isHeld = false;
        }

        /// <summary>Shows the most recently taken held card on top, and how many are held.</summary>
        public void Refresh()
        {
            if (_face == null || _factory.Inventory == null)
            {
                return;
            }

            if (_count != null)
            {
                _count.text = string.Format(CultureInfo.InvariantCulture, _factory.CardCountFormat, Count);
            }

            IReadOnlyList<InventoryStack> stacks = _factory.Inventory.Stacks;
            for (int i = stacks.Count - 1; i >= 0; i--)
            {
                if (stacks[i].Location == ItemLocation.Held && _factory.Cards.TryGetCard(stacks[i].CardId, out Card card))
                {
                    _face.Show(card, _factory.Palette);
                    return;
                }
            }
        }

        public void OnHeld(Transform socket, int heldLayer)
        {
            _isHeld = true;
            transform.SetParent(socket, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = _factory.CardsHeldRotation;
            HoldableFactory.SetLayer(gameObject, heldLayer);
            Refresh();
        }

        public bool TryUse() => false;

        /// <summary>Returns every held card to the binder. Always succeeds: the binder has no limit.</summary>
        public bool TryDrop(DropContext context)
        {
            if (!_isHeld)
            {
                return false;
            }

            // Snapshot first: each move changes the stack list.
            _returning.Clear();
            _returningCounts.Clear();
            foreach (InventoryStack stack in _factory.Inventory.Stacks)
            {
                if (stack.Location == ItemLocation.Held)
                {
                    _returning.Add(ItemRef.Card(stack.CardId, stack.Tier));
                    _returningCounts.Add(stack.Count);
                }
            }

            for (int i = 0; i < _returning.Count; i++)
            {
                if (!_factory.Inventory.Move(_returning[i], ItemLocation.Held, ItemLocation.Binder, _returningCounts[i]).IsSuccess)
                {
                    return false;
                }
            }

            _isHeld = false;
            _factory.ReleaseCardStack(this);
            return true;
        }
    }
}
