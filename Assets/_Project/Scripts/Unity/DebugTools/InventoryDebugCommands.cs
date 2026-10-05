using System;
using System.Text;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Session;
using static System.FormattableString;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Play Mode commands on the running session's inventory, for checking the binder without
    /// opening packs by hand. Plain C#, like <see cref="PackDebugCommands"/>, so the editor console and a
    /// future in-game console can host them. Granted cards cost their value, as if bought at market.
    /// </summary>
    public sealed class InventoryDebugCommands
    {
        public const string Prefix = "inventory.";

        public const string HelpText =
            "Inventory commands (Play Mode, on the scene's session):\n" +
            "  inventory.sample                 grant every card in the pool once, plus extra copies of Commons,\n" +
            "                                   Uncommons and one hit (fills the set tab past one spread)\n" +
            "  inventory.where                  units in each location (Binder, Held, DisplayCase, Placed)";

        // Extra copies so copy badges show on Commons, Uncommons and one hit.
        private const int ExtraCommonCopies = 2;
        private const int ExtraUncommonCopies = 1;
        private const int ExtraHitCopies = 1;

        private readonly GameSession _session;

        public InventoryDebugCommands(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>True for command lines this class runs (they start with "inventory.").</summary>
        public static bool Handles(string commandLine)
        {
            return commandLine != null && commandLine.TrimStart().StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public string Execute(string commandLine)
        {
            string[] parts = (commandLine ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1 && string.Equals(parts[0], "inventory.sample", StringComparison.OrdinalIgnoreCase))
            {
                return Sample();
            }

            if (parts.Length == 1 && string.Equals(parts[0], "inventory.where", StringComparison.OrdinalIgnoreCase))
            {
                return Where();
            }

            return $"Unknown inventory command '{commandLine}'.\n{HelpText}";
        }

        // One line per location, in enum order, plus the total; units = single cards plus sealed units.
        private string Where()
        {
            InventoryService inventory = _session.Inventory;
            var output = new StringBuilder();
            foreach (ItemLocation location in (ItemLocation[])Enum.GetValues(typeof(ItemLocation)))
            {
                output.AppendLine(Invariant($"{location}: {inventory.CountIn(location)}"));
            }

            output.Append(Invariant($"Total: {inventory.TotalItemCount}"));
            return output.ToString();
        }

        private string Sample()
        {
            int granted = 0;
            bool isHitDoubled = false;
            foreach (Card card in _session.Pool.Cards)
            {
                int copies = 1 + ExtraCopies(card.Tier, ref isHitDoubled);
                for (int i = 0; i < copies; i++)
                {
                    _session.Inventory.Add(card, card.ValueCents);
                }

                granted += copies;
            }

            return Invariant($"Granted {granted} cards ({_session.Pool.Cards.Count} distinct) to the inventory.");
        }

        private static int ExtraCopies(RarityTier tier, ref bool isHitDoubled)
        {
            switch (tier)
            {
                case RarityTier.Common:
                    return ExtraCommonCopies;
                case RarityTier.Uncommon:
                    return ExtraUncommonCopies;
                default:
                    if (isHitDoubled)
                    {
                        return 0;
                    }

                    isHitDoubled = true;
                    return ExtraHitCopies;
            }
        }
    }
}
