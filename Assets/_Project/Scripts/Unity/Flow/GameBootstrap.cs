using System;
using Game.Core.Session;
using Game.Unity.Definitions;
using Game.Unity.Player;
using Game.Unity.Props;
using Game.Unity.UI.Collection;
using Game.Unity.UI.Hud;
using Game.Unity.UI.PackOpening;
using UnityEngine;

namespace Game.Unity.Flow
{
    /// <summary>
    /// Creates the <see cref="GameSession"/> from the scene's content assets and hands it to the
    /// scene's views. The one place a scene's runtime objects are wired together; no singletons.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField, Tooltip("Pack product opened by the pack prop.")]
        private PackConfigDefinition _pack;

        [SerializeField, Tooltip("Tier colours and names shared by every card view.")]
        private RarityPaletteDefinition _palette;

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

        [SerializeField]
        private InventoryScreen _inventoryScreen;

        [SerializeField]
        private CrosshairView _crosshair;

        /// <summary>The run's session; null if the content failed to load.</summary>
        public GameSession Session { get; private set; }

        private void Awake()
        {
            // Survives between Play sessions when scene reload is disabled.
            Session = null;

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
            }
        }

        // Start runs after every Awake, so each view has already checked its own references.
        private void Start()
        {
            if (Session == null)
            {
                return;
            }

            if (_player == null || _packProp == null || _packOpeningScreen == null || _inventoryScreen == null || _crosshair == null)
            {
                Debug.LogError($"{name}: {nameof(GameBootstrap)} is missing a scene reference (player, pack prop, screens or crosshair).", this);
                return;
            }

            _packOpeningScreen.Initialize(Session, _palette, _player, _packProp);
            _inventoryScreen.Initialize(Session, _palette, _player);
            _crosshair.Initialize(_player);
        }
    }
}
