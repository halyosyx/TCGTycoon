using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using Game.Unity.Hands;
using Game.Unity.Interaction;
using Game.Unity.UI.Controls;
using Game.Unity.UI.Hud;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Game.Unity.Props
{
    /// <summary>
    /// The glass display case on the vendor table: single cards for sale (Core:
    /// <see cref="ItemLocation.DisplayCase"/>, 25 slots). It keeps no list of its own; on every inventory
    /// change it lays out whatever Core says is in the case, one pooled card per copy in a 5 × 5 grid.
    /// <list type="bullet">
    /// <item>The case body and lid: E opens or closes the lid (<see cref="GlassCaseLid"/>). Closed with
    /// cards in the hand, the prompt says to open it first.</item>
    /// <item>The open interior (<see cref="GlassCaseInterior"/>): E places the whole held stack; what
    /// doesn't fit stays in the hand and a toast says so.</item>
    /// <item>A card in the open case (<see cref="DisplayCaseCard"/>): E takes it back into the hand.</item>
    /// </list>
    /// Sealed product never goes in: a held pack gets no place prompt. No per-frame work at rest: the
    /// component updates only while the lid moves.
    /// </summary>
    public sealed class GlassCase : MonoBehaviour, IInteractable
    {
        [Header("Parts")]
        [SerializeField, Tooltip("Pivot on the lid's hinge edge; it turns about its local X.")]
        private Transform _lidHinge;

        [SerializeField, Tooltip("Parent of the card visuals, at the centre of the case floor (Z away from the vendor).")]
        private Transform _slots;

        [SerializeField, Tooltip("The case floor collider: E there places the held cards.")]
        private GlassCaseInterior _interior;

        [SerializeField, Tooltip("The world card model (Data/Generated/Prefabs/WorldCard).")]
        private GameObject _cardModel;

        [Header("Lid")]
        [SerializeField, Min(0f), Tooltip("Seconds to open or close the lid.")]
        private float _lidSeconds = 0.45f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees the lid stands at when open.")]
        private float _openAngle = 90f;

        [Header("Cards")]
        [SerializeField, Tooltip("A card's width and height in metres (63 x 88 mm).")]
        private Vector2 _cardSize = new Vector2(0.063f, 0.088f);

        [SerializeField, Min(0f), Tooltip("Space between columns of cards (across the case), in metres.")]
        private float _columnGap = 0.044f;

        [SerializeField, Min(0f), Tooltip("Space between rows of cards (toward the vendor), in metres.")]
        [FormerlySerializedAs("_cardGap")]
        private float _rowGap = 0.012f;

        [SerializeField, Min(0f), Tooltip("How far cards lie above the case floor, in metres.")]
        private float _cardLift = 0.004f;

        [SerializeField, Min(0f), Tooltip("How far a card rises while the player aims at it, in metres.")]
        private float _hoverLift = 0.006f;

        [SerializeField, Range(0, 31), Tooltip("Layer of the card visuals, so the interactor can find them.")]
        private int _interactableLayer = 6;

        [Header("Text")]
        [SerializeField] private string _openVerb = "Open";
        [SerializeField] private string _closeVerb = "Close";
        [SerializeField] private string _caseNoun = "glass case";
        [SerializeField, Tooltip("The object while the lid is closed and the player holds cards.")]
        private string _caseNounToPlace = "glass case to place cards";
        [SerializeField] private string _placeVerb = "Place";
        [SerializeField, Tooltip("{0} = cards held.")] private string _cardsNounFormat = "{0} cards";
        [SerializeField] private string _takeVerb = "Take";
        [SerializeField, Tooltip("Toast title when some cards don't fit; {0} = cards left in the hand.")]
        private string _leftoverTitleFormat = "Case full: {0} still in hand";
        [SerializeField, Tooltip("Toast detail when some cards don't fit; {0} = the case's slot count.")]
        private string _leftoverDetailFormat = "The case holds {0} cards. Nothing was lost.";

        private readonly List<ItemRef> _slotItems = new List<ItemRef>(DisplayCaseLayout.SlotCount);
        private readonly List<DisplayCaseCard> _visibleCards = new List<DisplayCaseCard>(DisplayCaseLayout.SlotCount);

        private GlassCaseLid _lid;
        private ObjectPool<DisplayCaseCard> _cardPool;
        private InventoryService _inventory;
        private CardPool _cards;
        private RarityPaletteDefinition _palette;
        private HandCards _handCards;
        private IHudSink _hud;
        private bool _isInitialized;

        public bool IsLidOpen => _lid != null && _lid.IsOpen;

        public GlassCaseLidState LidState => _lid == null ? GlassCaseLidState.Closed : _lid.State;

        public HandCards HandCards => _handCards;

        public string PlaceVerb => _placeVerb;

        public string TakeVerb => _takeVerb;

        public float HoverLift => _hoverLift;

        /// <summary>The cards shown, in slot order (for tests and debugging).</summary>
        public IReadOnlyList<DisplayCaseCard> VisibleCards => _visibleCards;

        // --- The lid (case body and lid colliders) ---

        public string PromptVerb => IsClosedOrClosing ? _openVerb : _closeVerb;

        public string PromptObject => IsClosedOrClosing && _handCards != null && _handCards.IsHoldingCards ? _caseNounToPlace : _caseNoun;

        private bool IsClosedOrClosing => _lid.State == GlassCaseLidState.Closed || _lid.State == GlassCaseLidState.Closing;

        private void Awake()
        {
            if (_lidHinge == null || _slots == null || _interior == null || _cardModel == null)
            {
                Debug.LogError($"{name}: {nameof(GlassCase)} needs a Lid Hinge, Slots, an Interior and a Card Model.", this);
            }

            // Private fields survive between Play sessions when scene reload is disabled.
            _lid = new GlassCaseLid(_lidSeconds, _openAngle);
            _cardPool = new ObjectPool<DisplayCaseCard>(CreateCard, card => card.gameObject.SetActive(true), card => card.gameObject.SetActive(false), defaultCapacity: DisplayCaseLayout.SlotCount, maxSize: DisplayCaseLayout.SlotCount);
            _visibleCards.Clear();
            _slotItems.Clear();
            _isInitialized = false;
            ApplyLid();

            // Update runs only while the lid moves.
            enabled = false;
        }

        /// <summary>Wires the case to the game. Called once by <c>GameBootstrap</c>.</summary>
        /// <param name="hud">Shows the "didn't fit" toast; may be null.</param>
        public void Initialize(InventoryService inventory, CardPool cards, RarityPaletteDefinition palette, HandCards handCards, IHudSink hud)
        {
            if (inventory == null || cards == null || palette == null || handCards == null || _interior == null || _cardModel == null)
            {
                Debug.LogError($"{name}: {nameof(GlassCase)} is missing its inventory, cards, palette, hand cards or parts.", this);
                return;
            }

            _inventory = inventory;
            _cards = cards;
            _palette = palette;
            _handCards = handCards;
            _hud = hud;
            _interior.Initialize(this);
            _inventory.Changed += Refresh;
            _isInitialized = true;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
            {
                _inventory.Changed -= Refresh;
            }
        }

        private void Update()
        {
            _lid.Tick(Time.deltaTime);
            ApplyLid();
            if (!_lid.IsMoving)
            {
                enabled = false;
            }
        }

        public bool CanInteract(InteractionContext context) => _isInitialized;

        public void SetHovered(bool isHovered)
        {
        }

        /// <summary>Opens or closes the lid (E); closing with cards inside is fine.</summary>
        public void Interact(InteractionContext context)
        {
            if (!_isInitialized)
            {
                return;
            }

            ToggleLid();
        }

        public void ToggleLid()
        {
            _lid.Toggle();
            enabled = true;
        }

        // --- Placing and taking (the interior and the cards ask the case) ---

        /// <summary>
        /// Places the whole held stack in the case (E on the open interior), as many as fit. What doesn't
        /// fit stays in the hand, and a toast says how many.
        /// </summary>
        public CardsMoveResult PlaceHeldCards()
        {
            if (!IsLidOpen)
            {
                return new CardsMoveResult(0, _handCards.HeldCount, MoveFailure.NotAllowedThere);
            }

            CardsMoveResult result = _handCards.PlaceAll(ItemLocation.DisplayCase);
            if (result.Left > 0 && result.Failure == MoveFailure.CapacityFull && _hud != null)
            {
                _hud.ShowToast(
                    ToastKind.Neutral,
                    string.Format(CultureInfo.InvariantCulture, _leftoverTitleFormat, result.Left),
                    string.Format(CultureInfo.InvariantCulture, _leftoverDetailFormat, _inventory.Capacities.DisplayCase),
                    null);
            }

            return result;
        }

        /// <summary>Takes one copy of a card in the case back into the hand (E on it, lid open).</summary>
        public bool TakeCard(ItemRef card) => IsLidOpen && _handCards.TryTake(card, ItemLocation.DisplayCase);

        /// <summary>"3 cards", for the interior's prompt.</summary>
        public string HeldCardsNoun() => string.Format(CultureInfo.InvariantCulture, _cardsNounFormat, _handCards.HeldCount);

        // Lays out what Core says is in the case. Runs on inventory changes only.
        private void Refresh()
        {
            int count = DisplayCaseLayout.Fill(_inventory.Stacks, _slotItems);
            while (_visibleCards.Count > count)
            {
                int last = _visibleCards.Count - 1;
                _cardPool.Release(_visibleCards[last]);
                _visibleCards.RemoveAt(last);
            }

            while (_visibleCards.Count < count)
            {
                _visibleCards.Add(_cardPool.Get());
            }

            for (int slot = 0; slot < count; slot++)
            {
                ItemRef item = _slotItems[slot];
                if (!_cards.TryGetCard(item.Id, out Card card))
                {
                    Debug.LogError($"{name}: the display case holds {item.Id}, which isn't in the card pool.", this);
                    continue;
                }

                _visibleCards[slot].Bind(item, card, _palette, DisplayCaseLayout.SlotPosition(slot, _cardSize, new Vector2(_columnGap, _rowGap), _cardLift));
            }
        }

        private void ApplyLid()
        {
            if (_lidHinge != null)
            {
                _lidHinge.localRotation = Quaternion.Euler(_lid.Angle, 0f, 0f);
            }
        }

        private DisplayCaseCard CreateCard()
        {
            GameObject model = Instantiate(_cardModel, _slots);
            HoldableFactory.SetLayer(model, _interactableLayer);

            // Lying flat, face up, its top edge away from the vendor so the vendor reads it.
            model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (!model.TryGetComponent(out WorldCardView face))
            {
                throw new InvalidOperationException($"{name}: the Card Model has no {nameof(WorldCardView)}.");
            }

            var card = model.AddComponent<DisplayCaseCard>();
            card.Initialize(this, face);
            return card;
        }
    }
}
