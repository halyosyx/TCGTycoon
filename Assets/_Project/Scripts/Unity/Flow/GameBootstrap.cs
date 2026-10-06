using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Session;
using Game.Core.Store;
using Game.Unity.Definitions;
using Game.Unity.Hands;
using Game.Unity.Player;
using Game.Unity.Props;
using Game.Unity.UI;
using Game.Unity.UI.Hud;
using Game.Unity.UI.PackOpening;
using Game.Unity.UI.Store;
using UnityEngine;

namespace Game.Unity.Flow
{
    /// <summary>
    /// Creates the <see cref="GameSession"/> from the scene's content assets and hands it to the
    /// scene's views. The one place a scene's runtime objects are wired together; no singletons.
    /// Screens that must stay deletable (prototypes) read what they need from here instead of being
    /// referenced by it: <see cref="Session"/> and <see cref="BinderReadModel"/>.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField, Tooltip("Default pack: its card set is the session's pool (inventory.sample grants it) and gets the first binder tab. Packs the player opens are bought from the store.")]
        private PackConfigDefinition _pack;

        [SerializeField, Tooltip("Tier colours and names shared by every card view.")]
        private RarityPaletteDefinition _palette;

        [SerializeField, Tooltip("Card sets the binder gives a tab, in order. Leave empty to show every set the loaded products use (the pack's set, then the store's).")]
        private List<CardSetDefinition> _binderSets = new List<CardSetDefinition>();

        [SerializeField, Tooltip("Starting cash.")]
        private EconomyConfigDefinition _economy;

        [SerializeField, Tooltip("The supplier website: listings, prices (market × supplier percent) and labels.")]
        private StoreConfigDefinition _store;

        [Header("Randomness")]
        [SerializeField, Tooltip("On: a new seed every Play session (logged, so a run can be replayed). Off: always use Seed.")]
        private bool _useRandomSeed = true;

        [SerializeField, Tooltip("Seed used when Use Random Seed is off.")]
        private int _seed = 1;

        [Header("Scene")]
        [SerializeField]
        private PlayerController _player;

        [SerializeField]
        private PackOpeningScreen _packOpeningScreen;

        [SerializeField, Tooltip("The HUD (Hud.uxml on the Hud panel): day, cash, toasts, crosshair and prompt.")]
        private HudPresenter _hud;

        [SerializeField, Tooltip("The supplier website on the home computer (0 opens it for now).")]
        private StoreScreen _storeScreen;

        [SerializeField, Tooltip("Stacks of bought packs on the home table; E takes one into the hand.")]
        private PackStackSpawner _packStacks;

        [SerializeField, Tooltip("Builds and pools what the player can hold: packs and the card stack.")]
        private HoldableFactory _holdables;

        [SerializeField, Tooltip("The glass display case on the vendor table. Optional: without it there is nowhere to show cards for sale.")]
        private GlassCase _glassCase;

        private InventoryBinderReadModel _binderReadModel;
        private HandCards _handCards;
        private BinderToHand _binderActions;
        private CardPool _allCards;

        /// <summary>The run's session; null if the content failed to load.</summary>
        public GameSession Session { get; private set; }

        /// <summary>The inventory as binder tabs, for any binder view. Created with the session in Awake.</summary>
        public IBinderReadModel BinderReadModel => _binderReadModel;

        /// <summary>What a binder view may do with an entry (take into the hand, put back); null without hands.</summary>
        public IBinderActions BinderActions => _binderActions;

        private void Awake()
        {
            // Survives between Play sessions when scene reload is disabled.
            Session = null;
            _binderReadModel = null;
            _handCards = null;
            _binderActions = null;
            _allCards = null;

            if (_pack == null || _pack.CardSet == null || _palette == null || _economy == null || _store == null)
            {
                Debug.LogError($"{name}: {nameof(GameBootstrap)} needs a Pack with a Card Set, a Palette, an Economy config and a Store config.", this);
                return;
            }

            // Unity picks the seed (Core never reads the clock); logging it makes a run replayable.
            int seed = _useRandomSeed ? Environment.TickCount : _seed;
            try
            {
                StoreCatalog catalog = _store.ToCatalog();
                Session = new GameSession(_pack.ToPackConfig(), _pack.CardSet.ToCardPool(), seed, _economy.StartingCashCents, catalog);
                Debug.Log($"{name}: session started with seed {seed}.", this);
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                Debug.LogError($"{name}: can't start the session: {exception.Message}", this);
                return;
            }

            // TODO(F2 day cycle): the GameStateMachine calls Store.ResetNightlyStock() at the start of
            // each Prep Night. The run starts on one, with the catalog's full stock already in place.

            _allCards = CreateAllCards();
            _binderReadModel = CreateBinderReadModel();
            if (_player != null && _player.Hands != null && _holdables != null)
            {
                _handCards = new HandCards(Session.Inventory, _player.Hands, _holdables);
                _binderActions = new BinderToHand(_allCards, _handCards);
            }
        }

        // Start runs after every Awake, so each view has already checked its own references.
        private void Start()
        {
            if (Session == null)
            {
                return;
            }

            // Packs reach the reveal only through the hand: stacks give a pack to the hand, and using
            // the held pack tears it open. All four are required together. The HUD (optional) shows the
            // tear's hints.
            if (_player == null || _player.Hands == null || _packOpeningScreen == null || _packStacks == null || _holdables == null)
            {
                Debug.LogError($"{name}: {nameof(GameBootstrap)} is missing a scene reference (player with hands, pack opening screen, pack stacks or holdables).", this);
                return;
            }

            _packOpeningScreen.Initialize(Session, _palette, _player, _hud);
            _holdables.Initialize(Session.Inventory, _allCards, _palette, _store, _packOpeningScreen.BeginTear);
            _packStacks.Initialize(Session.Inventory, _store, _holdables);

            // The HUD is optional so a scene without one still plays; pack opening never depends on it.
            if (_hud != null)
            {
                _hud.Initialize(_player, _player.Interactor, Session.Economy);
            }
            else
            {
                Debug.LogWarning($"{name}: no HUD assigned, so day, cash and the interaction prompt aren't shown.", this);
            }

            if (_glassCase != null)
            {
                _glassCase.Initialize(Session.Inventory, _allCards, _palette, _handCards, _hud);
            }

            // Optional too: without it the store is reachable only through the debug console's buy.
            if (_storeScreen != null)
            {
                _storeScreen.Initialize(Session, _store, _player, _hud);
            }
            else
            {
                Debug.LogWarning($"{name}: no store screen assigned, so the computer can't be opened.", this);
            }
        }

        // The binder's tabs: the sets listed on this component, or every set the loaded products use.
        // Its card lookup holds those sets' cards, so names resolve for every tab, not just the pack's set.
        private InventoryBinderReadModel CreateBinderReadModel()
        {
            var tabs = new List<BinderSet>();
            var cards = new List<Card>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasListedSets = _binderSets != null && _binderSets.Count > 0;
            foreach (CardSetDefinition set in hasListedSets ? _binderSets : AvailableCardSets())
            {
                if (set == null || string.IsNullOrEmpty(set.Id) || !seenIds.Add(set.Id))
                {
                    continue;
                }

                tabs.Add(new BinderSet(set.Id, set.ShortName));
                cards.AddRange(set.ToCardPool().Cards);
            }

            var products = new List<Product>();
            foreach (StoreListingState listing in Session.Store.Listings)
            {
                products.Add(listing.Product);
            }

            return new InventoryBinderReadModel(Session.Inventory, new CardPool(cards), tabs, products);
        }

        // Every card the player can own, from every set the loaded products use: names the cards in a
        // held card stack whatever the binder's tabs are.
        private CardPool CreateAllCards()
        {
            var cards = new List<Card>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CardSetDefinition set in AvailableCardSets())
            {
                if (set != null && !string.IsNullOrEmpty(set.Id) && seenIds.Add(set.Id))
                {
                    cards.AddRange(set.ToCardPool().Cards);
                }
            }

            return new CardPool(cards);
        }

        // Every product the scene loads contributes its card set: the default pack's, then each store
        // listing's in listing order (Hidden ones too, since owned stock can outlive a listing).
        private IEnumerable<CardSetDefinition> AvailableCardSets()
        {
            yield return _pack.CardSet;
            foreach (StoreListing listing in _store.Listings)
            {
                if (listing != null && listing.Product != null && listing.Product.CardSet != null)
                {
                    yield return listing.Product.CardSet;
                }
            }
        }

        // The read model subscribes to the inventory's Changed event; unsubscribe when the scene goes away.
        private void OnDestroy()
        {
            if (_binderReadModel != null)
            {
                _binderReadModel.Dispose();
                _binderReadModel = null;
            }
        }
    }
}
