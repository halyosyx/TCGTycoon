using System;
using System.Collections.Generic;
using Game.Core.Content;

namespace Game.Core.Packs
{
    /// <summary>The result of opening one pack: its cards in slot order, so a reveal can show the last slot last.</summary>
    public sealed class OpenedPack
    {
        public OpenedPack(string packId, IReadOnlyList<Card> cards)
        {
            PackId = packId ?? string.Empty;
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        public string PackId { get; }

        /// <summary>One card per slot, in slot order.</summary>
        public IReadOnlyList<Card> Cards { get; }

        /// <summary>Sum of the cards' values in cents.</summary>
        public long TotalValueCents
        {
            get
            {
                long totalCents = 0;
                foreach (Card card in Cards)
                {
                    totalCents += card.ValueCents;
                }

                return totalCents;
            }
        }
    }
}
