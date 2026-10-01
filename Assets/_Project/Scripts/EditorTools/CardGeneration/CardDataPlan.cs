using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Core.Content;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// The card data the manifests describe, checked as a whole before anything is written: every set
    /// with its cards and values, and the tier prices. Pure apart from <see cref="LoadFolder"/>, so the
    /// rules are tested without the editor. Any error leaves <see cref="IsValid"/> false and the
    /// generator writes nothing.
    /// </summary>
    public sealed class CardDataPlan
    {
        /// <summary>Price scales are whole percentages: value = base x scale / 100.</summary>
        public const int PercentDivisor = 100;

        // Tier codes are part of every card id, which inventory and saves key on: never change one.
        private static readonly Dictionary<RarityTier, string> s_tierCodes = new Dictionary<RarityTier, string>
        {
            { RarityTier.Common, "C" },
            { RarityTier.Uncommon, "U" },
            { RarityTier.HoloFullArt, "HFA" },
            { RarityTier.SpecialFullArtHolo, "SFAH" },
        };

        private CardDataPlan(IReadOnlyList<PlannedSet> sets, IReadOnlyList<TierPriceEntry> prices, IReadOnlyList<string> errors)
        {
            Sets = sets;
            Prices = prices;
            Errors = errors;
        }

        public IReadOnlyList<PlannedSet> Sets { get; }

        /// <summary>Tier prices in manifest order.</summary>
        public IReadOnlyList<TierPriceEntry> Prices { get; }

        public IReadOnlyList<string> Errors { get; }

        public bool IsValid => Errors.Count == 0;

        /// <summary>The id code for a tier: C, U, HFA or SFAH.</summary>
        public static string TierCode(RarityTier tier)
        {
            if (!s_tierCodes.TryGetValue(tier, out string code))
            {
                throw new ArgumentOutOfRangeException(nameof(tier), (int)tier, $"{RarityTiers.Describe(tier)} has no id code.");
            }

            return code;
        }

        /// <summary>
        /// Splits an id of the form <c>&lt;prefix&gt;_&lt;tierCode&gt;_&lt;index&gt;</c> (RC_C_014). The prefix is
        /// capital letters and digits, the index one or more digits.
        /// </summary>
        public static bool TryParseCardId(string id, out string prefix, out string tierCode, out int index)
        {
            prefix = string.Empty;
            tierCode = string.Empty;
            index = 0;
            string[] parts = (id ?? string.Empty).Split('_');
            if (parts.Length != 3 || !IsPrefix(parts[0]) || parts[1].Length == 0 || parts[2].Length == 0)
            {
                return false;
            }

            foreach (char character in parts[2])
            {
                if (character < '0' || character > '9')
                {
                    return false;
                }
            }

            prefix = parts[0];
            tierCode = parts[1];
            return int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out index);
        }

        /// <summary>Value in cents of a card at <paramref name="basePriceCents"/> in a set scaled by <paramref name="scalePercent"/>; false when it isn't a whole number of cents.</summary>
        public static bool TryScale(long basePriceCents, int scalePercent, out long valueCents)
        {
            long scaled = basePriceCents * scalePercent;
            valueCents = scaled / PercentDivisor;
            return scaled % PercentDivisor == 0;
        }

        /// <summary>
        /// Reads Sets.csv, every set's card manifest and TierPrices.csv from <paramref name="folder"/>
        /// and builds the plan. Missing or unreadable files are errors.
        /// </summary>
        public static CardDataPlan LoadFolder(string folder, bool requireNames = true)
        {
            var errors = new List<string>();
            string setsText = ReadFile(folder, CardManifest.SetsFileName, errors);
            string pricesText = ReadFile(folder, CardManifest.TierPricesFileName, errors);
            List<SetManifestEntry> sets = setsText == null ? new List<SetManifestEntry>() : CardManifest.ReadSets(setsText, CardManifest.SetsFileName, errors);
            List<TierPriceEntry> prices = pricesText == null ? new List<TierPriceEntry>() : CardManifest.ReadTierPrices(pricesText, CardManifest.TierPricesFileName, errors);

            var cardsBySetId = new Dictionary<string, IReadOnlyList<CardManifestEntry>>(StringComparer.Ordinal);
            foreach (SetManifestEntry set in sets)
            {
                if (string.IsNullOrEmpty(set.CardManifest) || cardsBySetId.ContainsKey(set.SetId))
                {
                    continue;
                }

                string text = ReadFile(folder, set.CardManifest, errors);
                cardsBySetId[set.SetId] = text == null ? new List<CardManifestEntry>() : CardManifest.ReadCards(text, set.CardManifest, errors);
            }

            return Build(sets, cardsBySetId, prices, errors, requireNames);
        }

        /// <summary>Checks the manifests against each other and computes card values.</summary>
        public static CardDataPlan Build(
            IReadOnlyList<SetManifestEntry> sets,
            IReadOnlyDictionary<string, IReadOnlyList<CardManifestEntry>> cardsBySetId,
            IReadOnlyList<TierPriceEntry> prices,
            IEnumerable<string> readErrors = null,
            bool requireNames = true)
        {
            if (sets == null) throw new ArgumentNullException(nameof(sets));
            if (cardsBySetId == null) throw new ArgumentNullException(nameof(cardsBySetId));
            if (prices == null) throw new ArgumentNullException(nameof(prices));

            var errors = readErrors == null ? new List<string>() : new List<string>(readErrors);
            Dictionary<RarityTier, TierPriceEntry> priceByTier = CheckPrices(prices, errors);
            CheckSets(sets, errors);

            var plannedSets = new List<PlannedSet>(sets.Count);
            var cardIds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (SetManifestEntry set in sets)
            {
                if (!cardsBySetId.TryGetValue(set.SetId, out IReadOnlyList<CardManifestEntry> entries))
                {
                    entries = Array.Empty<CardManifestEntry>();
                }

                var cards = new List<PlannedCard>(entries.Count);
                foreach (CardManifestEntry entry in entries)
                {
                    if (CheckCard(entry, set, cardIds, priceByTier, requireNames, errors, out long valueCents))
                    {
                        cards.Add(new PlannedCard(entry, set.SetId, valueCents));
                    }
                }

                plannedSets.Add(new PlannedSet(set, cards.AsReadOnly()));
            }

            return new CardDataPlan(plannedSets.AsReadOnly(), new List<TierPriceEntry>(prices).AsReadOnly(), errors.AsReadOnly());
        }

        /// <summary>The planned set with <paramref name="setId"/>, or null.</summary>
        public PlannedSet FindSet(string setId)
        {
            foreach (PlannedSet set in Sets)
            {
                if (string.Equals(set.SetId, setId, StringComparison.Ordinal))
                {
                    return set;
                }
            }

            return null;
        }

        private static Dictionary<RarityTier, TierPriceEntry> CheckPrices(IReadOnlyList<TierPriceEntry> prices, List<string> errors)
        {
            var priceByTier = new Dictionary<RarityTier, TierPriceEntry>();
            foreach (TierPriceEntry price in prices)
            {
                string where = CardManifest.Where(CardManifest.TierPricesFileName, price.LineNumber);
                if (priceByTier.ContainsKey(price.Tier))
                {
                    errors.Add($"{where}: {price.Tier} is listed more than once.");
                    continue;
                }

                priceByTier.Add(price.Tier, price);
            }

            foreach (RarityTier tier in RarityTiers.All)
            {
                if (!priceByTier.ContainsKey(tier))
                {
                    errors.Add($"{CardManifest.TierPricesFileName}: no price for {tier}.");
                }
            }

            return priceByTier;
        }

        private static void CheckSets(IReadOnlyList<SetManifestEntry> sets, List<string> errors)
        {
            var setIds = new HashSet<string>(StringComparer.Ordinal);
            var prefixes = new HashSet<string>(StringComparer.Ordinal);
            var manifests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SetManifestEntry set in sets)
            {
                string where = CardManifest.Where(CardManifest.SetsFileName, set.LineNumber);
                if (set.SetId.Length == 0 || set.SetId.IndexOf(' ') >= 0)
                {
                    errors.Add($"{where}: setId \"{set.SetId}\" must be non-empty with no spaces.");
                }
                else if (!setIds.Add(set.SetId))
                {
                    errors.Add($"{where}: setId {set.SetId} is listed more than once.");
                }

                if (set.DisplayName.Length == 0) errors.Add($"{where}: {set.SetId} has no displayName.");
                if (set.ShortName.Length == 0) errors.Add($"{where}: {set.SetId} has no shortName (the binder tab label).");
                if (!IsPrefix(set.IdPrefix))
                {
                    errors.Add($"{where}: idPrefix \"{set.IdPrefix}\" must be capital letters and digits.");
                }
                else if (!prefixes.Add(set.IdPrefix))
                {
                    errors.Add($"{where}: idPrefix {set.IdPrefix} is used by another set.");
                }

                if (set.PriceScalePercent <= 0) errors.Add($"{where}: priceScalePercent must be above 0.");
                if (set.CardManifest.Length == 0)
                {
                    errors.Add($"{where}: {set.SetId} has no cardManifest.");
                }
                else if (!manifests.Add(set.CardManifest))
                {
                    errors.Add($"{where}: cardManifest {set.CardManifest} is used by another set.");
                }
            }
        }

        private static bool CheckCard(
            CardManifestEntry entry,
            SetManifestEntry set,
            Dictionary<string, string> cardIds,
            Dictionary<RarityTier, TierPriceEntry> priceByTier,
            bool requireNames,
            List<string> errors,
            out long valueCents)
        {
            valueCents = 0;
            string where = CardManifest.Where(set.CardManifest, entry.LineNumber);
            int errorsBefore = errors.Count;

            if (!TryParseCardId(entry.Id, out string prefix, out string tierCode, out _))
            {
                errors.Add($"{where}: id \"{entry.Id}\" must look like {set.IdPrefix}_{TierCode(entry.Tier)}_001.");
            }
            else
            {
                if (!string.Equals(prefix, set.IdPrefix, StringComparison.Ordinal))
                {
                    errors.Add($"{where}: id {entry.Id} must start with {set.SetId}'s prefix {set.IdPrefix}.");
                }

                if (!string.Equals(tierCode, TierCode(entry.Tier), StringComparison.Ordinal))
                {
                    errors.Add($"{where}: id {entry.Id} has tier code {tierCode} but the tier is {entry.Tier} ({TierCode(entry.Tier)}).");
                }
            }

            if (cardIds.TryGetValue(entry.Id, out string firstWhere))
            {
                errors.Add($"{where}: id {entry.Id} is already used at {firstWhere}.");
            }
            else
            {
                cardIds.Add(entry.Id, where);
            }

            if (requireNames && entry.Name.Length == 0)
            {
                errors.Add($"{where}: {entry.Id} has no name (TCG > Generate Card Data > Fill missing names, or type one).");
            }

            if (priceByTier.TryGetValue(entry.Tier, out TierPriceEntry price)
                && !TryScale(price.BasePriceCents, set.PriceScalePercent, out valueCents))
            {
                errors.Add($"{where}: {entry.Tier} at {price.BasePriceCents} cents x {set.PriceScalePercent}% is not a whole number of cents.");
            }

            return errors.Count == errorsBefore;
        }

        private static bool IsPrefix(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char character in text)
            {
                bool isCapitalOrDigit = (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9');
                if (!isCapitalOrDigit)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ReadFile(string folder, string fileName, List<string> errors)
        {
            string path = Path.Combine(folder ?? string.Empty, fileName);
            if (!File.Exists(path))
            {
                errors.Add($"{fileName}: not found in {folder}.");
                return null;
            }

            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                errors.Add($"{fileName}: can't be read ({exception.Message}).");
                return null;
            }
        }
    }
}
