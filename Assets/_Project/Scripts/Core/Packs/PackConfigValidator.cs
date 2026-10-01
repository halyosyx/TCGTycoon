using System;
using System.Collections.Generic;
using Game.Core.Content;

namespace Game.Core.Packs
{
    /// <summary>
    /// Checks a pack configuration against its card pool. Errors mean the pack can't be opened;
    /// warnings are allowed but probably mistakes. Used by <see cref="PackOpener"/> and the editor tool.
    /// </summary>
    public static class PackConfigValidator
    {
        public static IReadOnlyList<ValidationIssue> Validate(PackConfig config, CardPool pool)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (pool == null) throw new ArgumentNullException(nameof(pool));

            var issues = new List<ValidationIssue>();

            if (config.PriceCents <= 0)
            {
                issues.Add(PackIssue(ValidationSeverity.Warning, $"Price is {config.PriceCents} cents, so Rip EV as a share of price is meaningless."));
            }

            if (config.Slots.Count == 0)
            {
                issues.Add(PackIssue(ValidationSeverity.Error, "The pack has no slots."));
            }

            for (int slotIndex = 0; slotIndex < config.Slots.Count; slotIndex++)
            {
                ValidateSlot(config.Slots[slotIndex], slotIndex, pool, issues);
            }

            ValidatePool(pool, issues);
            return issues;
        }

        public static bool HasErrors(IReadOnlyList<ValidationIssue> issues)
        {
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            foreach (ValidationIssue issue in issues)
            {
                if (issue.Severity == ValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateSlot(PackSlot slot, int slotIndex, CardPool pool, List<ValidationIssue> issues)
        {
            if (slot.Entries.Count == 0)
            {
                issues.Add(SlotIssue(ValidationSeverity.Error, slotIndex, "The slot has no tier entries."));
                return;
            }

            long positiveWeightTotal = 0;
            var seenTiers = new HashSet<RarityTier>();
            foreach (TierWeight entry in slot.Entries)
            {
                if (!RarityTiers.IsDefined(entry.Tier))
                {
                    // Names stale seven-tier data ("removed tier FullArt (4)") so it can't pass unnoticed.
                    issues.Add(SlotIssue(ValidationSeverity.Error, slotIndex, $"Uses {RarityTiers.Describe(entry.Tier)}."));
                    continue;
                }

                if (!seenTiers.Add(entry.Tier))
                {
                    issues.Add(SlotIssue(ValidationSeverity.Warning, slotIndex, $"{entry.Tier} is listed more than once; its weights are added together."));
                }

                if (entry.Weight < 0)
                {
                    issues.Add(SlotIssue(ValidationSeverity.Error, slotIndex, $"{entry.Tier} has a negative weight ({entry.Weight})."));
                    continue;
                }

                positiveWeightTotal += entry.Weight;
                if (entry.Weight > 0 && !pool.HasCards(entry.Tier))
                {
                    issues.Add(SlotIssue(ValidationSeverity.Error, slotIndex, $"{entry.Tier} can roll, but the card pool has no {entry.Tier} cards."));
                }
            }

            if (positiveWeightTotal == 0)
            {
                issues.Add(SlotIssue(ValidationSeverity.Error, slotIndex, "No tier has a positive weight, so nothing can roll."));
            }
        }

        private static void ValidatePool(CardPool pool, List<ValidationIssue> issues)
        {
            var seenIds = new HashSet<string>();
            foreach (Card card in pool.Cards)
            {
                if (!seenIds.Add(card.Id))
                {
                    issues.Add(PackIssue(ValidationSeverity.Warning, $"Card id '{card.Id}' appears more than once in the pool; inventory would merge those cards."));
                }
            }
        }

        private static ValidationIssue PackIssue(ValidationSeverity severity, string message)
        {
            return new ValidationIssue(severity, ValidationIssue.PackLevel, message);
        }

        private static ValidationIssue SlotIssue(ValidationSeverity severity, int slotIndex, string message)
        {
            return new ValidationIssue(severity, slotIndex, message);
        }
    }
}
