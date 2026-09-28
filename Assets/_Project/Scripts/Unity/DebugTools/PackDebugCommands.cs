using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using static System.FormattableString;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Text commands for exercising packs without a scene: open packs, show and clear inventory.
    /// Plain C#, so the editor console and a future in-game console can host the same commands.
    /// </summary>
    public sealed class PackDebugCommands
    {
        /// <summary>Opening at most this many packs lists every pull; more prints a distribution summary.</summary>
        public const int DetailedOpenLimit = 10;

        /// <summary>Upper bound for one "open" so the editor stays responsive.</summary>
        public const int MaxPacksPerCommand = 1_000_000;

        public const string HelpText =
            "Commands:\n" +
            "  open <count>     open packs; lists pulls for up to 10, otherwise a summary. Cards go to inventory at the pack price.\n" +
            "  inventory        list owned stacks with tier, count and cost basis\n" +
            "  clearinventory   remove every card\n" +
            "  help             show this list";

        private readonly PackOpener _opener;
        private readonly InventoryService _inventory;

        public PackDebugCommands(PackOpener opener, InventoryService inventory)
        {
            _opener = opener ?? throw new ArgumentNullException(nameof(opener));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>Runs one command line and returns its output.</summary>
        public string Execute(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return string.Empty;
            }

            string[] parts = commandLine.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0].ToLowerInvariant())
            {
                case "help":
                    return HelpText;
                case "open":
                    return Open(parts);
                case "inventory":
                    return DescribeInventory();
                case "clearinventory":
                    _inventory.Clear();
                    return "Inventory cleared.";
                default:
                    return $"Unknown command '{parts[0]}'. Type 'help' for the list.";
            }
        }

        private string Open(string[] parts)
        {
            if (parts.Length != 2
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                || count < 1
                || count > MaxPacksPerCommand)
            {
                return Invariant($"Usage: open <count>, where count is 1 to {MaxPacksPerCommand:N0}.");
            }

            PackConfig config = _opener.Config;
            var tally = new PackTally(config.Slots.Count);
            var output = new StringBuilder();
            output.AppendLine(Invariant($"Opened {count:N0} × {config.DisplayName} for {Money.Format(config.PriceCents * count)}; cards added to inventory."));

            for (int packNumber = 1; packNumber <= count; packNumber++)
            {
                OpenedPack pack = _opener.Open();
                _inventory.AddPack(pack, config.PriceCents);
                tally.Add(pack);
                if (count <= DetailedOpenLimit)
                {
                    AppendPulls(output, packNumber, pack);
                }
            }

            if (count > DetailedOpenLimit)
            {
                AppendDistribution(output, config, tally);
            }

            AppendRipEv(output, config, tally);
            return output.ToString().TrimEnd();
        }

        private static void AppendPulls(StringBuilder output, int packNumber, OpenedPack pack)
        {
            output.AppendLine(Invariant($"Pack {packNumber} (value {Money.Format(pack.TotalValueCents)}):"));
            for (int slotIndex = 0; slotIndex < pack.Cards.Count; slotIndex++)
            {
                Card card = pack.Cards[slotIndex];
                output.AppendLine(Invariant($"  {slotIndex + 1,2}. {card.Tier,-22} {card.DisplayName,-28} {Money.Format(card.ValueCents),9}"));
            }
        }

        private void AppendDistribution(StringBuilder output, PackConfig config, PackTally tally)
        {
            long totalCards = tally.PackCount * config.Slots.Count;
            output.AppendLine("Tier                      Pulled   Share   Expected   Packs with tier+   Expected");
            foreach (RarityTier tier in RarityTiers.All)
            {
                long pulled = 0;
                double expectedShare = 0d;
                for (int slotIndex = 0; slotIndex < config.Slots.Count; slotIndex++)
                {
                    pulled += tally.TierCount(slotIndex, tier);
                    expectedShare += PackAnalysis.TierProbability(config.Slots[slotIndex], tier) / config.Slots.Count;
                }

                if (pulled == 0 && expectedShare == 0d)
                {
                    continue;
                }

                output.AppendLine(Invariant(
                    $"{tier,-22} {pulled,10:N0}  {Percent((double)pulled / totalCards),7}  {Percent(expectedShare),7}   {Percent(tally.ObservedChanceOfAtLeastOne(tier)),10}      {Percent(PackAnalysis.ChanceOfAtLeastOne(config, tier)),7}"));
            }
        }

        private void AppendRipEv(StringBuilder output, PackConfig config, PackTally tally)
        {
            double observed = tally.AverageValueCents;
            double expected = PackAnalysis.ExpectedValueCents(config, _opener.Pool);
            output.AppendLine(Invariant(
                $"Rip EV: observed {observed:0.0}¢ ({ShareOfPrice(observed, config)}) · expected {expected:0.0}¢ ({ShareOfPrice(expected, config)}) · price {Money.Format(config.PriceCents)}"));
        }

        private string DescribeInventory()
        {
            if (_inventory.Stacks.Count == 0)
            {
                return "Inventory is empty.";
            }

            var output = new StringBuilder();
            output.AppendLine("Tier                   Card                          Count   Cost basis");
            int totalCards = 0;
            IEnumerable<InventoryStack> ordered = _inventory.Stacks
                .OrderByDescending(stack => stack.Tier)
                .ThenBy(stack => stack.CardId, StringComparer.Ordinal);
            foreach (InventoryStack stack in ordered)
            {
                string cardName = _opener.Pool.TryGetCard(stack.CardId, out Card card) ? card.DisplayName : stack.CardId;
                output.AppendLine(Invariant($"{stack.Tier,-22} {cardName,-28} {stack.Count,6:N0}   {Money.Format(stack.CostBasisCents),10}"));
                totalCards += stack.Count;
            }

            output.Append(Invariant($"{_inventory.Stacks.Count} stacks, {totalCards:N0} cards, total cost basis {Money.Format(_inventory.TotalCostBasisCents)}"));
            return output.ToString();
        }

        private static string Percent(double share) => Invariant($"{share * 100d:0.00}%");

        private static string ShareOfPrice(double valueCents, PackConfig config)
        {
            return config.PriceCents > 0 ? Invariant($"{valueCents / config.PriceCents * 100d:0.0}% of price") : "no price";
        }
    }
}
