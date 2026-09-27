using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Common;
using Game.Core.Content;

namespace Game.Core.Packs
{
    /// <summary>
    /// Opens packs. For each configured slot, in order, it rolls a tier from the slot's weights and then
    /// picks a card of that tier uniformly at random. The configuration is the only source of slot
    /// structure; rolls are independent (no pity, no god packs).
    /// </summary>
    public sealed class PackOpener
    {
        private readonly IRng _rng;
        private readonly WeightedRoller<RarityTier>[] _slotRollers;

        /// <exception cref="InvalidOperationException">The configuration has validation errors.</exception>
        public PackOpener(PackConfig config, CardPool pool, IRng rng)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            IReadOnlyList<ValidationIssue> issues = PackConfigValidator.Validate(config, pool);
            if (PackConfigValidator.HasErrors(issues))
            {
                IEnumerable<string> errors = issues.Where(issue => issue.Severity == ValidationSeverity.Error).Select(issue => issue.ToString());
                throw new InvalidOperationException($"Pack '{config.Id}' can't be opened:\n{string.Join("\n", errors)}");
            }

            _slotRollers = new WeightedRoller<RarityTier>[config.Slots.Count];
            for (int slotIndex = 0; slotIndex < config.Slots.Count; slotIndex++)
            {
                _slotRollers[slotIndex] = CreateRoller(config.Slots[slotIndex]);
            }
        }

        public PackConfig Config { get; }

        public CardPool Pool { get; }

        /// <summary>Opens one pack: one card per slot, in slot order.</summary>
        public OpenedPack Open()
        {
            var cards = new Card[_slotRollers.Length];
            for (int slotIndex = 0; slotIndex < _slotRollers.Length; slotIndex++)
            {
                RarityTier tier = _slotRollers[slotIndex].Roll(_rng);
                IReadOnlyList<Card> tierCards = Pool.CardsOf(tier);
                cards[slotIndex] = tierCards[_rng.NextInt(tierCards.Count)];
            }

            return new OpenedPack(Config.Id, cards);
        }

        private static WeightedRoller<RarityTier> CreateRoller(PackSlot slot)
        {
            var tiers = new RarityTier[slot.Entries.Count];
            var weights = new int[slot.Entries.Count];
            for (int i = 0; i < slot.Entries.Count; i++)
            {
                tiers[i] = slot.Entries[i].Tier;
                weights[i] = slot.Entries[i].Weight;
            }

            return new WeightedRoller<RarityTier>(tiers, weights);
        }
    }
}
