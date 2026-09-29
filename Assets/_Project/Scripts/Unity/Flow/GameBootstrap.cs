using System;
using System.Collections.Generic;
using Game.Core.Content;
using Game.Core.Session;
using Game.Unity.Definitions;
using Game.Unity.Player;
using Game.Unity.Props;
using Game.Unity.UI;
using Game.Unity.UI.Hud;
using Game.Unity.UI.PackOpening;
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
        [SerializeField, Tooltip("Pack product opened by the pack prop.")]
        private PackConfigDefinition _pack;

        [SerializeField, Tooltip("Tier colours and names shared by every card view.")]
        private RarityPaletteDefinition _palette;

        [SerializeField, Tooltip("Card sets the binder gives a tab, in order. Leave empty to show every set the loaded products use (today: the pack's set).")]
        private List<CardSetDefinition> _binderSets = new List<CardSetDefinition>();

        [Header("Randomness")]
        [SerializeField, Tooltip("On: a new seed every Play session (logged, so a run can be replayed). Off: always use Seed.")]
        private bool _useRandomSeed = true;

        [SerializeField, Tooltip("Seed used when Use Random Seed is off.")]
        private int _seed = 1;

        [Header("Scene")]
        [SerializeField]
        private PlayerController _player;

        [SerializeField]
        private PackProp _packProp;

        [SerializeField]
        private PackOpeningScreen _packOpeningScreen;

        [SerializeField, Tooltip("The HUD (Hud.uxml on the Hud panel): day, cash, toasts, crosshair and prompt.")]
        private HudPresenter _hud;

        private InventoryBinderReadModel _binderReadModel;

        /// <summary>The run's session; null if the content failed to load.</summary>
        public GameSession Session { get; private set; }

        /// <summary>The inventory as binder tabs, for any binder view. Created with the session in Awake.</summary>
        public IBinderReadModel BinderReadModel => _binderReadModel;

        private void Awake()
        {
            // Survives between Play sessions when scene reload is disabled.
            Session = null;
            _binderReadModel = null;

            if (_pack == null || _pack.CardSet == null || _palette == null)
            {
                Debug.LogError($"{name}: {nameof(GameBootstrap)} needs a Pack with a Card Set and a Palette.", this);
                return;
            }

            // Unity picks the seed (Core never reads the clock); logging it makes a run replayable.
            int seed = _useRandomSeed ? Environment.TickCount : _seed;
            try
            {
                Session = new GameSession(_pack.ToPackConfig(), _pack.CardSet.ToCardPool(), seed);
                Debug.Log($"{name}: session started with seed {seed}.", this);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{name}: can't start the session: {exception.Message}", this);
                return;
            }

            _binderReadModel = CreateBinderReadModel();
        }

        // Start runs after every Awake, so each view has already checked its own references.
        private void Start()
        {
            if (Session == null)
            {
                return;
            }

            if (_player == null || _packProp == null || _packOpeningScreen == null)
            {
                Debug.LogError($"{name}: {nameof(GameBootstrap)} is missing a scene reference (player, pack prop or pack opening screen).", this);
                return;
            }

            _packOpeningScreen.Initialize(Session, _palette, _player, _packProp);

            // The HUD is optional so a scene without one still plays; pack opening never depends on it.
            if (_hud != null)
            {
                _hud.Initialize(_player, _player.Interactor);
            }
            else
            {
                Debug.LogWarning($"{name}: no HUD assigned, so day, cash and the interaction prompt aren't shown.", this);
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

            return new InventoryBinderReadModel(Session.Inventory, new CardPool(cards), tabs);
        }

        // Every product the scene loads contributes its card set. Today that is the one booster pack;
        // when more products arrive (bundles, boxes, Set B), add their sets here.
        private IEnumerable<CardSetDefinition> AvailableCardSets()
        {
            yield return _pack.CardSet;
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
