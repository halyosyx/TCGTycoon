using System;
using System.Collections.Generic;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Economy;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Core.Store;

namespace Game.Core.Session
{
    /// <summary>
    /// Composition root for one run: owns the Core services and is the only object Unity code talks
    /// to. Holds cash (<see cref="Economy"/>), the supplier store, the inventory, and the pack openers:
    /// the default pack (the free floor prop) plus one per booster pack product the store sells, all
    /// drawing from one seeded RNG. Market and shows join in later milestones.
    /// </summary>
    public sealed class GameSession
    {
        private readonly PackOpener _packOpener;
        private readonly Dictionary<string, PackOpener> _productOpeners = new Dictionary<string, PackOpener>(StringComparer.Ordinal);

        /// <summary>A session with no cash and an empty store: only the default pack can be opened.</summary>
        /// <exception cref="InvalidOperationException">The pack configuration has validation errors.</exception>
        public GameSession(PackConfig pack, CardPool pool, int seed)
            : this(pack, pool, seed, 0, StoreCatalog.Empty)
        {
        }

        /// <exception cref="InvalidOperationException">A pack configuration has validation errors.</exception>
        public GameSession(PackConfig pack, CardPool pool, int seed, long startingCashCents, StoreCatalog catalog)
        {
            Pack = pack ?? throw new ArgumentNullException(nameof(pack));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var rng = new SeededRng(seed);
            Inventory = new InventoryService();
            Economy = new EconomyService(startingCashCents);
            Store = new StoreService(catalog, Economy, Inventory);
            _packOpener = new PackOpener(pack, pool, rng);
            foreach (StoreCatalogListing listing in catalog.Listings)
            {
                Product product = listing.Product;
                if (product.Type == ProductType.BoosterPack)
                {
                    _productOpeners.Add(product.Id, new PackOpener(product.Pack, product.Pool, rng));
                }
            }
        }

        public PackConfig Pack { get; }

        public CardPool Pool { get; }

        public InventoryService Inventory { get; }

        public EconomyService Economy { get; }

        public StoreService Store { get; }

        /// <summary>
        /// Opens one pack and adds its cards to the inventory at the pack's price, then returns them.
        /// The cards are owned before the caller sees them, so a reveal is presentation only: closing
        /// or failing it can't lose cards.
        /// </summary>
        public OpenedPack OpenPack()
        {
            OpenedPack opened = _packOpener.Open();
            Inventory.AddPack(opened, Pack.PriceCents);
            return opened;
        }

        /// <summary>
        /// Opens one owned sealed pack of <paramref name="productId"/>: in one inventory change the pack
        /// leaves and its cards arrive, carrying exactly what that pack was paid. As with
        /// <see cref="OpenPack"/>, the cards are owned before the caller sees them.
        /// </summary>
        /// <exception cref="InvalidOperationException">The product isn't a booster pack the store sells, or none is owned.</exception>
        public OpenedPack OpenSealedPack(string productId)
        {
            if (productId == null || !_productOpeners.TryGetValue(productId, out PackOpener opener))
            {
                throw new InvalidOperationException($"'{productId}' isn't a booster pack the store sells.");
            }

            if (Inventory.CountOfSealed(productId) == 0)
            {
                throw new InvalidOperationException($"No sealed '{productId}' is owned.");
            }

            OpenedPack opened = opener.Open();
            Inventory.OpenSealed(productId, opened);
            return opened;
        }
    }
}
