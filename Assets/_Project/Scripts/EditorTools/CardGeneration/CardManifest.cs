using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// Reads and writes the card data manifests in <c>Data/Manifests/</c>, the authoring source for all
    /// card data (GDD v1.7, Docs/Systems/CARD_DATA_AND_SETS.md): <c>Sets.csv</c>, one card manifest per
    /// set, and <c>TierPrices.csv</c>. Reading checks each file on its own (headers, tiers, numbers) and
    /// reports problems as "file:line: message"; cross-file rules live in <see cref="CardDataPlan"/>.
    /// Cells are trimmed, so writing what was read gives the same text back.
    /// </summary>
    public static class CardManifest
    {
        public const string SetsFileName = "Sets.csv";
        public const string TierPricesFileName = "TierPrices.csv";

        public static readonly IReadOnlyList<string> SetsHeader =
            Array.AsReadOnly(new[] { "setId", "displayName", "shortName", "idPrefix", "lifecycle", "priceScalePercent", "cardManifest", "colour" });

        public static readonly IReadOnlyList<string> CardsHeader =
            Array.AsReadOnly(new[] { "id", "name", "tier", "flavour", "artHint" });

        public static readonly IReadOnlyList<string> TierPricesHeader =
            Array.AsReadOnly(new[] { "tier", "basePriceCents", "volatilityTier" });

        public static List<SetManifestEntry> ReadSets(string text, string fileName, List<string> errors)
        {
            var sets = new List<SetManifestEntry>();
            if (!TryParse(text, fileName, SetsHeader, errors, out CsvTable table))
            {
                return sets;
            }

            foreach (CsvRow row in table.Rows)
            {
                string where = Where(fileName, row.LineNumber);
                bool isValid = TryParseEnum(row.Cell(4).Trim(), out SetLifecycle lifecycle, where, "lifecycle", errors);
                isValid &= TryParseInt(row.Cell(5).Trim(), where, "priceScalePercent", errors, out int scale);
                string colour = row.Cell(7).Trim();
                isValid &= IsHexColour(colour, where, errors);
                if (isValid)
                {
                    sets.Add(new SetManifestEntry(
                        row.Cell(0).Trim(), row.Cell(1).Trim(), row.Cell(2).Trim(), row.Cell(3).Trim(), lifecycle, scale, row.Cell(6).Trim(), colour, row.LineNumber));
                }
            }

            return sets;
        }

        public static List<CardManifestEntry> ReadCards(string text, string fileName, List<string> errors)
        {
            var cards = new List<CardManifestEntry>();
            if (!TryParse(text, fileName, CardsHeader, errors, out CsvTable table))
            {
                return cards;
            }

            foreach (CsvRow row in table.Rows)
            {
                if (TryParseTier(row.Cell(2).Trim(), Where(fileName, row.LineNumber), errors, out RarityTier tier))
                {
                    cards.Add(new CardManifestEntry(
                        row.Cell(0).Trim(), row.Cell(1).Trim(), tier, row.Cell(3).Trim(), row.Cell(4).Trim(), row.LineNumber));
                }
            }

            return cards;
        }

        public static List<TierPriceEntry> ReadTierPrices(string text, string fileName, List<string> errors)
        {
            var prices = new List<TierPriceEntry>();
            if (!TryParse(text, fileName, TierPricesHeader, errors, out CsvTable table))
            {
                return prices;
            }

            foreach (CsvRow row in table.Rows)
            {
                string where = Where(fileName, row.LineNumber);
                bool isValid = TryParseTier(row.Cell(0).Trim(), where, errors, out RarityTier tier);
                isValid &= TryParseLong(row.Cell(1).Trim(), where, "basePriceCents", errors, out long cents);
                isValid &= TryParseEnum(row.Cell(2).Trim(), out VolatilityTier volatility, where, "volatilityTier", errors);
                if (isValid)
                {
                    prices.Add(new TierPriceEntry(tier, cents, volatility, row.LineNumber));
                }
            }

            return prices;
        }

        public static string WriteSets(IEnumerable<SetManifestEntry> sets)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (SetManifestEntry set in sets)
            {
                rows.Add(new[]
                {
                    set.SetId, set.DisplayName, set.ShortName, set.IdPrefix, set.Lifecycle.ToString(),
                    set.PriceScalePercent.ToString(CultureInfo.InvariantCulture), set.CardManifest, set.Colour,
                });
            }

            return CsvTable.Write(SetsHeader, rows);
        }

        public static string WriteCards(IEnumerable<CardManifestEntry> cards)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (CardManifestEntry card in cards)
            {
                rows.Add(new[] { card.Id, card.Name, card.Tier.ToString(), card.Flavour, card.ArtHint });
            }

            return CsvTable.Write(CardsHeader, rows);
        }

        public static string WriteTierPrices(IEnumerable<TierPriceEntry> prices)
        {
            var rows = new List<IReadOnlyList<string>>();
            foreach (TierPriceEntry price in prices)
            {
                rows.Add(new[] { price.Tier.ToString(), price.BasePriceCents.ToString(CultureInfo.InvariantCulture), price.Volatility.ToString() });
            }

            return CsvTable.Write(TierPricesHeader, rows);
        }

        /// <summary>"Champions.csv:12" for messages; just the file name for file-level problems.</summary>
        public static string Where(string fileName, int lineNumber)
        {
            return lineNumber > 0 ? $"{fileName}:{lineNumber.ToString(CultureInfo.InvariantCulture)}" : fileName;
        }

        private static bool TryParse(string text, string fileName, IReadOnlyList<string> expectedHeader, List<string> errors, out CsvTable table)
        {
            try
            {
                table = CsvTable.Parse(text);
            }
            catch (FormatException exception)
            {
                errors.Add($"{fileName}: {exception.Message}");
                table = null;
                return false;
            }

            if (!HeaderMatches(table.Header, expectedHeader))
            {
                errors.Add($"{fileName}: the header must be \"{string.Join(",", expectedHeader)}\" but is \"{string.Join(",", table.Header)}\".");
                return false;
            }

            return true;
        }

        private static bool HeaderMatches(IReadOnlyList<string> header, IReadOnlyList<string> expected)
        {
            if (header.Count != expected.Count)
            {
                return false;
            }

            for (int column = 0; column < expected.Count; column++)
            {
                if (!string.Equals(header[column].Trim(), expected[column], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseTier(string text, string where, List<string> errors, out RarityTier tier)
        {
            if (RarityTiers.TryParse(text, out tier))
            {
                return true;
            }

            errors.Add(RarityTiers.IsRemovedName(text)
                ? $"{where}: tier \"{text}\" was removed with the seven-tier ladder; use one of {string.Join(", ", RarityTiers.All)}."
                : $"{where}: unknown tier \"{text}\"; use one of {string.Join(", ", RarityTiers.All)}.");
            return false;
        }

        private static bool TryParseEnum<TEnum>(string text, out TEnum value, string where, string column, List<string> errors)
            where TEnum : struct
        {
            foreach (TEnum candidate in (TEnum[])Enum.GetValues(typeof(TEnum)))
            {
                if (string.Equals(candidate.ToString(), text, StringComparison.Ordinal))
                {
                    value = candidate;
                    return true;
                }
            }

            errors.Add($"{where}: {column} \"{text}\" must be one of {string.Join(", ", Enum.GetNames(typeof(TEnum)))}.");
            value = default;
            return false;
        }

        // "#RRGGBB" only, so a typo can't silently become black.
        private static bool IsHexColour(string text, string where, List<string> errors)
        {
            bool isValid = text.Length == 7 && text[0] == '#';
            for (int i = 1; isValid && i < text.Length; i++)
            {
                isValid = Uri.IsHexDigit(text[i]);
            }

            if (!isValid)
            {
                errors.Add($"{where}: colour \"{text}\" must be #RRGGBB.");
            }

            return isValid;
        }

        private static bool TryParseInt(string text, string where, string column, List<string> errors, out int value)
        {
            if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            errors.Add($"{where}: {column} \"{text}\" must be a whole number.");
            return false;
        }

        private static bool TryParseLong(string text, string where, string column, List<string> errors, out long value)
        {
            if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            errors.Add($"{where}: {column} \"{text}\" must be a whole number of cents.");
            return false;
        }
    }
}
