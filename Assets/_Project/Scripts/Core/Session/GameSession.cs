using System;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;

namespace Game.Core.Session
{
    /// <summary>
    /// Composition root for one run: owns the Core services and is the only object Unity code talks
    /// to. For now it holds the inventory and one pack product; cash, market and shows join in later
    /// milestones.
    /// </summary>
    public sealed class GameSession
    {
        private readonly PackOpener _packOpener;

        /// <exception cref="InvalidOperationException">The pack configuration has validation errors.</exception>
        public GameSession(PackConfig pack, CardPool pool, int seed)
        {
            Pack = pack ?? throw new ArgumentNullException(nameof(pack));
            Pool = pool ?? throw new ArgumentNullException(nameof(pool));
            Inventory = new InventoryService();
            _packOpener = new PackOpener(pack, pool, new SeededRng(seed));
        }

        public PackConfig Pack { get; }

        public CardPool Pool { get; }

        public InventoryService Inventory { get; }

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
    }
}
