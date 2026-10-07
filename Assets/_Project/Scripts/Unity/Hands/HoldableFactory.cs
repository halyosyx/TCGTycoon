using System;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Unity.Cards;
using Game.Unity.Definitions;
using Game.Unity.Interaction;
using Game.Unity.UI.PackOpening;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Unity.Hands
{
    /// <summary>
    /// Builds and pools the things a player can hold: <see cref="SealedBoosterPack"/>s (from the pack
    /// model, one per pack in hand or set down) and the one <see cref="CardStack"/>. Holds their shared
    /// settings (models, layers, poses, prompt words) so each holdable stays small. Initialised once by
    /// <c>GameBootstrap</c>.
    /// </summary>
    public sealed class HoldableFactory : MonoBehaviour
    {
        [Header("Models")]
        [SerializeField, Tooltip("The pack model (Data/Generated/Prefabs/BoosterPack).")]
        private GameObject _packModel;

        [SerializeField, Tooltip("The world card model (Data/Generated/Prefabs/WorldCard), showing the top held card.")]
        private GameObject _cardModel;

        [SerializeField, Tooltip("Size of a pack's click box (x, y, z) in metres, matching the pack model (PackShape.Default).")]
        private Vector3 _packSize = new Vector3(0.07f, 0.12f, 0.007f);

        [Header("Layers")]
        [SerializeField, Range(0, 31), Tooltip("Layer of a set-down pack, so the interactor can find it.")]
        private int _interactableLayer = 6;

        [SerializeField, Tooltip("What a pack can be set down on: world geometry (the table, the floor).")]
        private LayerMask _surfaces = 1;

        [Header("Held pose (local to the hand socket)")]
        [SerializeField] private Vector3 _packHeldRotation = new Vector3(0f, 0f, -8f);
        [SerializeField] private Vector3 _cardsHeldRotation = new Vector3(0f, 0f, -6f);

        [Header("Putting a pack down")]
        [SerializeField, Min(0.1f), Tooltip("How far in front of the player a pack is set down, in metres.")]
        private float _dropReach = 0.6f;

        [SerializeField, Min(0.5f), Tooltip("How far down to look for a surface, in metres.")]
        private float _dropSearchDepth = 3f;

        [Header("Look")]
        [SerializeField, Tooltip("Base colour of a set-down pack while the player aims at it.")]
        private Color _highlightColor = new Color(0.55f, 0.45f, 0.85f);

        [Header("Held card count")]
        [SerializeField, Tooltip("Count beside the held card stack; {0} = cards held.")]
        private string _cardCountFormat = "\u00D7{0}";

        [SerializeField, Min(0.01f), Tooltip("World font size of the count (TextMesh Pro units).")]
        private float _cardCountFontSize = 0.14f;

        [SerializeField] private Color _cardCountColor = new Color(0.96f, 0.96f, 0.97f);

        [SerializeField, Tooltip("Where the count sits relative to the held card, in metres (front is −Z).")]
        private Vector3 _cardCountOffset = new Vector3(0.046f, -0.036f, -0.002f);

        [Header("Text")]
        [SerializeField] private string _takeVerb = "Take";
        [SerializeField, Tooltip("{0} = set short name.")] private string _packNounFormat = "{0} pack";
        [SerializeField] private string _openVerb = "Open pack";
        [SerializeField] private string _putDownVerb = "Put down";
        [SerializeField] private string _returnVerb = "Return to binder";
        [SerializeField, Tooltip("{0} = number of cards held.")] private string _cardsNounFormat = "{0} cards";

        private InventoryService _inventory;
        private CardPool _cards;
        private RarityPaletteDefinition _palette;
        private StoreConfigDefinition _store;
        private Func<ITearablePack, bool> _openPack;
        private ObjectPool<SealedBoosterPack> _packPool;
        private CardStack _cardStack;

        public InventoryService Inventory => _inventory;

        public CardPool Cards => _cards;

        public RarityPaletteDefinition Palette => _palette;

        public int InteractableLayer => _interactableLayer;

        public LayerMask Surfaces => _surfaces;

        public Vector3 PackSize => _packSize;

        public Quaternion PackHeldRotation => Quaternion.Euler(_packHeldRotation);

        public Quaternion CardsHeldRotation => Quaternion.Euler(_cardsHeldRotation);

        public float DropReach => _dropReach;

        public float DropSearchDepth => _dropSearchDepth;

        public Color HighlightColor => _highlightColor;

        public string TakeVerb => _takeVerb;

        public string OpenVerb => _openVerb;

        public string PutDownVerb => _putDownVerb;

        public string ReturnVerb => _returnVerb;

        public string CardsNounFormat => _cardsNounFormat;

        public string CardCountFormat => _cardCountFormat;

        private void Awake()
        {
            if (_packModel == null || _cardModel == null)
            {
                Debug.LogError($"{name}: {nameof(HoldableFactory)} needs a Pack Model and a Card Model.", this);
            }

            // Private fields survive between Play sessions when scene reload is disabled.
            _inventory = null;
            _cardStack = null;
            _packPool = new ObjectPool<SealedBoosterPack>(CreatePack, pack => pack.gameObject.SetActive(true), pack => pack.gameObject.SetActive(false), defaultCapacity: 2);
        }

        /// <param name="cards">Every card the player can own, for the card stack's face.</param>
        /// <param name="openPack">Starts opening the held pack (the zoom, which commits nothing); true when it started.</param>
        public void Initialize(InventoryService inventory, CardPool cards, RarityPaletteDefinition palette, StoreConfigDefinition store, Func<ITearablePack, bool> openPack)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _palette = palette;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _openPack = openPack ?? throw new ArgumentNullException(nameof(openPack));
        }

        /// <summary>A pack object for one unit of <paramref name="productId"/>, ready to be held.</summary>
        public SealedBoosterPack GetPack(string productId)
        {
            ProductDefinition product = _store.FindProduct(productId);
            CardSetDefinition set = product == null ? null : product.CardSet;
            SealedBoosterPack pack = _packPool.Get();
            string noun = string.Format(System.Globalization.CultureInfo.InvariantCulture, _packNounFormat, set == null ? productId : set.ShortName);
            pack.Bind(productId, noun, set == null ? Color.gray : set.Colour);
            return pack;
        }

        public void ReleasePack(SealedBoosterPack pack)
        {
            pack.transform.SetParent(transform, false);
            _packPool.Release(pack);
        }

        /// <summary>The card stack, shown; it mirrors whatever cards Core says are held.</summary>
        public CardStack GetCardStack()
        {
            if (_cardStack == null)
            {
                // Built inactive, so its OnEnable (which subscribes to the inventory) runs after Initialize.
                var root = new GameObject("CardStack");
                root.SetActive(false);
                root.transform.SetParent(transform, false);
                GameObject face = Instantiate(_cardModel, root.transform);
                foreach (Collider modelCollider in face.GetComponentsInChildren<Collider>(true))
                {
                    modelCollider.enabled = false;
                }

                var countObject = new GameObject("Count");
                countObject.transform.SetParent(root.transform, false);
                countObject.transform.localPosition = _cardCountOffset;
                var count = countObject.AddComponent<TextMeshPro>();
                count.fontSize = _cardCountFontSize;
                count.color = _cardCountColor;
                count.fontStyle = FontStyles.Bold;
                count.alignment = TextAlignmentOptions.Center;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.rectTransform.sizeDelta = new Vector2(0.05f, 0.02f);

                _cardStack = root.AddComponent<CardStack>();
                _cardStack.Initialize(this, face.GetComponent<WorldCardView>(), count);
            }

            _cardStack.gameObject.SetActive(true);
            _cardStack.Refresh();
            return _cardStack;
        }

        public void ReleaseCardStack(CardStack stack)
        {
            stack.transform.SetParent(transform, false);
            stack.gameObject.SetActive(false);
        }

        /// <summary>Starts opening the held pack through the pack opening screen (it stays in the hand until the rip).</summary>
        public bool OpenHeldPack(ITearablePack pack) => _openPack(pack);

        private SealedBoosterPack CreatePack()
        {
            var root = new GameObject("SealedBoosterPack");
            root.transform.SetParent(transform, false);
            GameObject model = Instantiate(_packModel, root.transform);
            foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>(true))
            {
                modelCollider.enabled = false;   // the pack's own box takes the clicks
            }

            var box = root.AddComponent<BoxCollider>();
            box.size = _packSize;
            var pack = root.AddComponent<SealedBoosterPack>();
            if (!model.TryGetComponent(out BoosterPackView view))
            {
                Debug.LogError($"{name}: the Pack Model has no {nameof(BoosterPackView)}; regenerate it with TCG > Generate Card Data.", this);
            }

            pack.Initialize(this, box, new RendererTint(RendererTint.PackMeshes(model)), view);
            return pack;
        }

        /// <summary>Sets a whole hierarchy's layer (held items switch to the Held layer and back).</summary>
        public static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
