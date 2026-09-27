using System;
using Game.Core.Packs;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Presentation state of one pack reveal: Idle → Revealing → Row → Idle. Cards are revealed in
    /// slot order. The pack's cards are already in the inventory when <see cref="Begin"/> is called,
    /// so every transition here is display only; <see cref="Store"/> is legal at any point.
    /// </summary>
    public sealed class PackRevealStateMachine
    {
        public PackRevealState State { get; private set; } = PackRevealState.Idle;

        /// <summary>The pack on screen; null while Idle.</summary>
        public OpenedPack Pack { get; private set; }

        /// <summary>How many cards are face up, counting from slot 1.</summary>
        public int RevealedCount { get; private set; }

        public int CardCount => Pack == null ? 0 : Pack.Cards.Count;

        public bool HasUnrevealedCards => RevealedCount < CardCount;

        /// <exception cref="InvalidOperationException">A pack is already on screen.</exception>
        public void Begin(OpenedPack pack)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            RequireState(PackRevealState.Idle, nameof(Begin));

            Pack = pack;
            RevealedCount = 0;
            State = pack.Cards.Count == 0 ? PackRevealState.Row : PackRevealState.Revealing;
        }

        /// <summary>Reveals the next card and returns its slot index (0-based).</summary>
        /// <exception cref="InvalidOperationException">Not revealing, or every card is already face up.</exception>
        public int RevealNext()
        {
            RequireState(PackRevealState.Revealing, nameof(RevealNext));
            if (!HasUnrevealedCards)
            {
                throw new InvalidOperationException("Every card is already revealed; show the row instead.");
            }

            int slotIndex = RevealedCount;
            RevealedCount++;
            return slotIndex;
        }

        /// <summary>Lays every card out face up. Called after the last reveal, or early to skip (quick open).</summary>
        public void ShowRow()
        {
            RequireState(PackRevealState.Revealing, nameof(ShowRow));
            RevealedCount = CardCount;
            State = PackRevealState.Row;
        }

        /// <summary>Closes the screen. Legal while revealing too: the cards are already owned.</summary>
        public void Store()
        {
            if (State == PackRevealState.Idle)
            {
                throw new InvalidOperationException("There is no pack on screen to store.");
            }

            Pack = null;
            RevealedCount = 0;
            State = PackRevealState.Idle;
        }

        private void RequireState(PackRevealState required, string action)
        {
            if (State != required)
            {
                throw new InvalidOperationException($"Can't {action} while {State}.");
            }
        }
    }
}
