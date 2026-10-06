using System;
using Game.Core.Packs;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// Presentation state of one pack opening: Idle → Tearing → Revealing → Row ⇄ Showcase, and back to
    /// Idle. The pack's cards are committed to the inventory once, when the tear starts
    /// (<see cref="TryBeginTear"/>), before anything is animated; every later transition is display
    /// only, and <see cref="Store"/> is legal at any point. Cards are revealed in slot order.
    /// </summary>
    public sealed class PackRevealStateMachine
    {
        /// <summary><see cref="ShowcasedSlot"/> while no card is showcased.</summary>
        public const int NoSlot = -1;

        public PackRevealState State { get; private set; } = PackRevealState.Idle;

        /// <summary>The pack on screen; null while Idle.</summary>
        public OpenedPack Pack { get; private set; }

        /// <summary>How many cards are face up, counting from slot 1.</summary>
        public int RevealedCount { get; private set; }

        /// <summary>Slot index of the card in the showcase, or <see cref="NoSlot"/>.</summary>
        public int ShowcasedSlot { get; private set; } = NoSlot;

        public int CardCount => Pack == null ? 0 : Pack.Cards.Count;

        public bool HasUnrevealedCards => RevealedCount < CardCount;

        /// <summary>
        /// Starts tearing a pack open. From Idle it calls <paramref name="commit"/> exactly once (the call
        /// that takes the pack and adds its cards to the inventory) and enters Tearing. In any other state
        /// it returns false without calling it, so a mashed button can never open a second pack.
        /// </summary>
        /// <exception cref="InvalidOperationException">The commit returned no pack; the state stays Idle.</exception>
        public bool TryBeginTear(Func<OpenedPack> commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            if (State != PackRevealState.Idle)
            {
                return false;
            }

            // A commit that throws leaves the machine Idle: nothing was taken, so nothing is shown.
            OpenedPack pack = commit();
            if (pack == null)
            {
                throw new InvalidOperationException("The tear's commit returned no pack.");
            }

            Pack = pack;
            RevealedCount = 0;
            ShowcasedSlot = NoSlot;
            State = PackRevealState.Tearing;
            return true;
        }

        /// <summary>The cards are out of the top: the face-down stack takes over (the row, for an empty pack).</summary>
        public void FinishTear()
        {
            RequireState(PackRevealState.Tearing, nameof(FinishTear));
            State = Pack.Cards.Count == 0 ? PackRevealState.Row : PackRevealState.Revealing;
        }

        /// <summary>Shows an already-committed pack without a tear (tests and tools).</summary>
        /// <exception cref="InvalidOperationException">A pack is already on screen.</exception>
        public void Begin(OpenedPack pack)
        {
            if (pack == null) throw new ArgumentNullException(nameof(pack));
            RequireState(PackRevealState.Idle, nameof(Begin));

            Pack = pack;
            RevealedCount = 0;
            ShowcasedSlot = NoSlot;
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

        /// <summary>
        /// Lays every card out face up. Called after the last reveal, or early to skip (quick open), which
        /// also works mid-tear.
        /// </summary>
        public void ShowRow()
        {
            if (State != PackRevealState.Tearing)
            {
                RequireState(PackRevealState.Revealing, nameof(ShowRow));
            }

            RevealedCount = CardCount;
            State = PackRevealState.Row;
        }

        /// <summary>Lifts one card out of the row into the showcase.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The slot isn't in the pack.</exception>
        public void Showcase(int slotIndex)
        {
            RequireState(PackRevealState.Row, nameof(Showcase));
            if (slotIndex < 0 || slotIndex >= CardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, $"The pack has slots 0 to {CardCount - 1}.");
            }

            ShowcasedSlot = slotIndex;
            State = PackRevealState.Showcase;
        }

        /// <summary>Puts the showcased card back in the row.</summary>
        public void ReturnToRow()
        {
            RequireState(PackRevealState.Showcase, nameof(ReturnToRow));
            ShowcasedSlot = NoSlot;
            State = PackRevealState.Row;
        }

        /// <summary>Closes the screen. Legal at any point: the cards are already owned.</summary>
        public void Store()
        {
            if (State == PackRevealState.Idle)
            {
                throw new InvalidOperationException("There is no pack on screen to store.");
            }

            Pack = null;
            RevealedCount = 0;
            ShowcasedSlot = NoSlot;
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
