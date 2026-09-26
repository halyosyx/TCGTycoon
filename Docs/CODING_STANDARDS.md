# Coding Standards

How we write C# for TCG Card Show Simulator (Unity 6.3, C# 9). Every rule has a one-line reason; if a rule gets in the way, question the reason, not just the rule.

**Precedence:** architecture rules > correctness habits > naming and style. `.editorconfig` enforces the naming rules as IDE1006 warnings in VS Code; everything else is enforced in review.

## 1. Architecture rules

These override every style preference below.

| Rule | Why |
| --- | --- |
| Game rules live in `Game.Core`: no `UnityEngine` types, no `MonoBehaviour`, no `UnityEngine.Random`. | Rules stay deterministic, seeded and unit-testable without the Editor. |
| `Game.Unity` depends on `Game.Core`, never the reverse. UI and MonoBehaviours read state from Core services and send intents to them; they hold no rules. | One place per rule; the view can be rebuilt without touching the game. |
| One composition root, `GameSession` (`Core/Session`), constructs the services and passes them down. `GameBootstrap` (Unity) creates the session. | Every dependency is visible in one constructor. |
| Dependencies arrive through a constructor (Core) or an explicit `Initialize(...)` call (MonoBehaviours, which can't have constructors). | No hidden lookups; a missing dependency fails at wiring time, not mid-show. |
| Money is integer cents (`long`). Never `float` or `double` for currency. Round once, in one helper, when a price formula produces a fraction. | `0.1f + 0.2f != 0.3f`; a cent lost per sale adds up over 20 shows. |
| No mutable static state. A static that must exist is `readonly` and reset explicitly. | With domain reload disabled, statics survive between Play Mode runs. |
| Core never reads the clock (`Time.time`, `DateTime.Now`). Time and randomness are passed in. | Same seed and same inputs give the same run, in tests and in replays. |

```csharp
namespace Game.Core.Session
{
    /// <summary>Composition root: owns every Core service for one run.</summary>
    public sealed class GameSession
    {
        public MarketService Market { get; }
        public NegotiationFactory Negotiations { get; }

        public GameSession(GameContent content, int seed)
        {
            var rng = new SeededRng(seed);
            Market = new MarketService(content, rng);
            Negotiations = new NegotiationFactory(rng);
        }
    }
}
```

## 2. Naming and formatting

Microsoft's C# identifier conventions, with Unity's usual underscore for private fields.

| Kind | Style | Example |
| --- | --- | --- |
| Types, methods, properties, events, enums, enum members, constants, public fields | PascalCase | `MarketService`, `AdvanceDay`, `MaxPatience`, `Rarity.SecretRare` |
| Locals, parameters | camelCase | `offerCents`, `dayIndex` |
| Private and internal instance fields | `_camelCase` | `_basePriceCents` |
| Private static fields | `s_camelCase` | `s_animatorIds` |
| Interfaces, type parameters | `I` / `T` prefix | `IRng`, `TItem` |
| ScriptableObject classes and assets | PascalCase, class ends in `Definition` | `CardDefinition`, asset `SetA_EmberDrake` |

- **Events are verb phrases; handlers are `On` + event.** `SaleCompleted` → `OnSaleCompleted`. *Why:* you can tell what happened and who reacts without opening the file.
- **Booleans read as assertions.** `isOpen`, `hasStock`, `canAfford`, `IsFinished`. *Why:* `if (canAfford)` reads as a sentence; `if (afford)` doesn't.
- **One public type per file; file name matches the type.** *Why:* Unity requires it for MonoBehaviours and ScriptableObjects, and it makes every type findable by file name.
- **Block-scoped namespaces matching the folder:** `Game.Core.Market`, `Game.Unity.UI`. *Why:* Unity 6.3 compiles C# 9; file-scoped namespaces fail with CS8773. No C# 10+ features (global usings, record structs, required members).
- **Never name a folder after a type we use** (`Random`, `Debug`, `Editor`, `Object`, `Time`, `Input`). *Why:* the folder becomes a namespace that hides the type (`Debug.Log` fails with CS0234, `new Random()` with CS0118).
- **Readability over brevity; no abbreviations** except universal ones (`Id`, `UI`, `Rng`, `Ev`). *Why:* `customerPatience` needs no decoding; `cp` does.
- **Enum members are PascalCase.** *Why:* consistency. Roslyn's naming rules can't check enum members, so review catches these.

```csharp
// Bad
public class marketService
{
    public static int count;          // mutable static, lower-case public field
    private float price;              // float money, no underscore
    public event Action<Sale> onSale; // noun-ish, camelCase event
    bool open;                        // not an assertion, implicit private
}

// Good
public sealed class MarketService
{
    private static readonly string[] s_trendLabels = { "Falling", "Steady", "Rising" };
    private long _basePriceCents;
    public event Action<PriceChange> PricesUpdated;
    public bool IsMarketOpen { get; private set; }
}
```

### Comments and documentation

- **XML `///` summaries on public types and any non-obvious public member.** *Why:* they show up in IntelliSense where the caller is working.
- **Comments explain why, not what.** *Why:* the code already says what; only a comment can say why the obvious alternative was rejected.

```csharp
// Bad:  // subtract fee
// Good: // Fee is charged even on a zero-revenue day (GDD: "flat $60, every show").
_cashCents -= TableFeeCents;
```

## 3. Patterns decided for this project

### Observer: USE (plain C# events)

Core services raise events; UI, audio and results subscribe. The seller doesn't know who listens. *Why:* Core stays engine-free, and adding a new listener (a sound, a stat) touches no rules.

```csharp
// Bad: Core reaches into the UI (and needs a singleton to do it)
public void CompleteSale(Sale sale)
{
    _cashCents += sale.PriceCents;
    ResultsScreen.Instance.AddLine(sale);
}

// Good: Core announces; listeners decide what to do
public event Action<Sale> SaleCompleted;

public void CompleteSale(Sale sale)
{
    _cashCents += sale.PriceCents;
    SaleCompleted?.Invoke(sale);   // ?. is fine on delegates, just not on Unity objects
}
```

```csharp
public sealed class SaleFeedView : MonoBehaviour
{
    private ShowSimulation _show;

    public void Initialize(ShowSimulation show)
    {
        _show = show;
        _show.SaleCompleted += OnSaleCompleted;
    }

    // Core services outlive scenes: a missed unsubscribe calls into a destroyed view.
    private void OnDestroy()
    {
        if (_show != null) _show.SaleCompleted -= OnSaleCompleted;
    }

    private void OnSaleCompleted(Sale sale) { /* add a line to the feed */ }
}
```

**Pair every subscription with an unsubscribe** in the matching teardown (`Initialize` with `OnDestroy`, or `OnEnable` with `OnDisable`).

**Alternative we're not using:** ScriptableObject event channels are a valid Unity pattern, but they're assets, and assets can't live in a no-engine assembly.

**When not to use:** when you need an answer back (call a method), or when order between listeners matters (make it an explicit call sequence). Never make events `static`.

### State: USE

Name the states and allow only legal transitions. *Why:* it replaces long `if`/`else` chains whose bugs are illegal state combinations.

- `GameStateMachine` (Unity): Home → BoothSetup → ShowFloor → Results. Each state has enter/exit work (load a scene, show UI), so **one class per state**.
- `NegotiationSession` (Core): awaiting player → accepted / walked. States carry no behaviour of their own, so **an enum plus guarded methods** is enough.
- Customer AI (Core rules, Unity visuals): browsing → deciding → negotiating → leaving.

```csharp
public enum NegotiationState { AwaitingPlayer, Accepted, Walked }

public void Counter(long counterCents)
{
    if (State != NegotiationState.AwaitingPlayer)
    {
        throw new InvalidOperationException($"Cannot counter while {State}.");
    }

    if (counterCents <= _maxPriceCents)
    {
        State = NegotiationState.Accepted;
        return;
    }

    _patience -= IsInsulting(counterCents) ? 2 : 1;   // GDD negotiation rule 4
    State = _patience <= 0 ? NegotiationState.Walked : NegotiationState.AwaitingPlayer;
}
```

**When not to use:** a single boolean with two outcomes. Don't wrap a bool in a state machine.

### Flyweight: USE

Shared data is defined once and is immutable. Each runtime copy holds an id plus its own mutable state. A `CardDefinition` asset converts once, at startup, into a Core `Card`. *Why:* 36 copies of the same Secret Rare shouldn't carry 36 copies of its name, rarity and base price, or disagree about them.

```csharp
// Shared and immutable: one per card in the set
public sealed class Card
{
    public string Id { get; }
    public string Name { get; }
    public Rarity Rarity { get; }
    public long BasePriceCents { get; }

    public Card(string id, string name, Rarity rarity, long basePriceCents)
    {
        Id = id;
        Name = name;
        Rarity = rarity;
        BasePriceCents = basePriceCents;
    }
}

// One per copy the player owns: id + mutable state only
public sealed class OwnedCard
{
    public string CardId { get; }
    public long CostBasisCents { get; }
    public BoothZone? Zone { get; set; }

    public OwnedCard(string cardId, long costBasisCents)
    {
        CardId = cardId;
        CostBasisCents = costBasisCents;
    }
}
```

**ScriptableObjects are read-only at runtime.** *Why:* in the Editor, a change made in Play Mode is saved into the asset.

**When not to use:** per-copy data never goes on the definition. If only one instance of something ever exists, just use the object.

### Object Pool: USE (`UnityEngine.Pool`)

Pool GameObjects that churn: customers, pack-opening card visuals, floating price labels. Use `UnityEngine.Pool.ObjectPool<T>`; don't hand-roll a pool. *Why:* bulk-opening a box spawns 360 card visuals, and `Instantiate`/`Destroy` in bursts causes GC and frame spikes.

```csharp
public sealed class PackOpeningView : MonoBehaviour
{
    [SerializeField] private CardView _cardViewPrefab;
    private ObjectPool<CardView> _cardViews;

    private void Awake()
    {
        _cardViews = new ObjectPool<CardView>(
            createFunc: () => Instantiate(_cardViewPrefab, transform),
            actionOnGet: view => view.gameObject.SetActive(true),
            actionOnRelease: view => view.gameObject.SetActive(false),
            actionOnDestroy: view => Destroy(view.gameObject),
            defaultCapacity: 10);
    }
}
```

**When not to use:** Core objects (plain C# allocations are cheap and pooling would leak engine concerns into Core), and anything spawned once per scene.

### Game Loop: PROVIDED BY UNITY

We don't build a loop; we decide what's allowed in Unity's. **No game rules in `Update`.** Core advances only through explicit calls such as `AdvanceDay()` and `Tick(deltaSeconds)`, and it steps in fixed increments. *Why:* tests drive Core without frames, and a fixed step makes a seed replay the same show at 30 or 144 fps.

```csharp
// Unity layer: the only place frame time enters Core
private void Update() => _show.Tick(Time.deltaTime);

// Core: fixed steps, independent of frame rate
public void Tick(float deltaSeconds)
{
    _accumulatedSeconds += deltaSeconds;
    while (_accumulatedSeconds >= StepSeconds)
    {
        _accumulatedSeconds -= StepSeconds;
        Step();   // customer arrivals, patience, timers
    }
}
```

### Command: DEFER (not in v1)

Recognise it when these show up: a transaction log for replays and bug reports, undo in booth setup, or sending player actions to a co-op host. *Why defer:* v1 has none of these, and commands add a class per action.

### Singleton: AVOID (banned)

```csharp
// Banned
public sealed class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;
    private void Awake() => Instance = this;
}
```

*Why:* it's hidden global state (any file can change the market), it can't be replaced in a test, and with domain reload disabled `Instance` survives into the next Play Mode run and points at a destroyed object. Use `GameSession` and pass services down.

## 4. Unity performance and correctness

| Rule | Why |
| --- | --- |
| Cache component and transform references in `Awake`. Never call `GetComponent`, `FindObjectOfType` or `Camera.main` in `Update`. | Lookups every frame are waste. `Camera.main` is much cheaper than before Unity 2020.2 but still a lookup, and a field makes the dependency explicit. |
| No per-frame allocations: no LINQ, string concatenation or `new` collections in `Update` or hot loops. LINQ is fine in setup code and Core tests. | Allocations become GC spikes, which read as stutter. |
| `[SerializeField] private` fields, not public fields; expose read-only properties when other code needs access. | The Inspector gets its field without the whole codebase getting write access. |
| Prefer composition over inheritance; keep MonoBehaviours thin. | Small components combine; deep hierarchies fork. |
| Use `TryGetComponent`. Avoid string-based APIs (`Invoke`, `SendMessage`, animator strings); use cached ids (`Animator.StringToHash`) and direct references. | Strings fail at runtime on a typo; references fail at compile time. |
| Null-check serialized references in `Awake` and log an error naming the object. | "NullReferenceException" mid-show beats nobody; "Booth: Price Label is not assigned" at load fixes itself. |
| Null-check Unity objects with `== null` or implicit bool, never `?.`, `??` or `is null`. | Destroyed Unity objects only look null to Unity's `==`. The Unity extension's analyzers flag this (UNT0007, UNT0008). |

```csharp
// Bad: allocates every frame, and uses float for money
private void Update()
{
    var unsold = _items.Where(item => !item.IsSold).ToList();
    _priceLabel.text = "$" + (_priceCents / 100f).ToString("0.00");
}

// Good: update the label only when the price changes
private void OnPriceChanged(long priceCents) => _priceLabel.text = Money.Format(priceCents);
```

```csharp
private void Awake()
{
    if (_priceLabel == null)
    {
        Debug.LogError($"{name}: Price Label is not assigned.", this);
    }
}
```

**Statics, if you really need one:** `readonly` stops reassignment, not mutation, so a static collection still needs an explicit reset.

```csharp
// Bad: survives between Play Mode runs when domain reload is off
public static class ShowStats { public static int SalesToday; }

// Good: state lives on an instance owned by GameSession
public sealed class EconomyService { public int SalesToday { get; private set; } }

// Acceptable: a readonly cache, reset explicitly
private static readonly Dictionary<string, int> s_animatorIds = new Dictionary<string, int>();

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics() => s_animatorIds.Clear();
```

## 5. Testing

- **Core changes are test-first:** write EditMode tests in `Assets/Tests/EditMode` (folders mirror Core), then implement. *Why:* the rules are the product, and Core tests run in milliseconds.
- **Name tests `MethodName_Scenario_ExpectedResult`.** *Why:* a failing test's name should describe the bug.
- **Seed anything probabilistic** (pull rates, price noise), and set tolerances you can justify statistically, not "whatever passes". *Why:* a fixed seed makes the test repeatable; a justified tolerance keeps it passing when the seed changes.
- **Shared fakes and builders go in `TestUtilities/`.** *Why:* the same test setup copy-pasted into ten files drifts.

```csharp
[Test]
public void Counter_AtOrBelowMax_Accepts()
{
    var session = NegotiationBuilder.WithMaxPriceCents(1_000).Build();

    session.Counter(1_000);

    Assert.That(session.State, Is.EqualTo(NegotiationState.Accepted));
}

[Test]
public void OpenPack_OneHundredThousandPacks_SecretRareRateNearThreePercent()
{
    var opener = new PackOpener(TestContent.SetA, new SeededRng(20260917));
    const int packCount = 100_000;
    int secretRares = 0;

    for (int i = 0; i < packCount; i++)
    {
        if (opener.Open().RareSlot.Rarity == Rarity.SecretRare) secretRares++;
    }

    // σ = √(n·p·(1−p)) ≈ 54 packs, so ±300 packs (±0.3%) is about 5.5σ.
    Assert.That(secretRares / (double)packCount, Is.EqualTo(0.03).Within(0.003));
}
```

## 6. Review checklist

Before writing code, and again before a change is done:

1. **Reusable?** Could another system use this? Then shape it with parameters and data, not hard-coded cases.
2. **Already exists?** Search `Core/` and `Unity/` first; extend, don't duplicate.
3. **Utility?** Logic needed in two places moves to `Core/Common` (pure) or the Unity layer's shared helpers.
4. **Tool?** Repeated editor work (data validation, balance runs) becomes an editor tool in `Scripts/EditorTools`.
5. **Reviewable?** Small methods, clear names, why-comments.
6. **Standard?** No new compiler, analyzer or IDE1006 warnings.

## 7. Open questions

Not decided yet. Don't invent a rule for these; raise them when they come up.

1. **Money rounding mode.** Round-half-away-from-zero or banker's rounding when `basePrice × trend × event × noise` yields fractional cents? And a `Money` value type wrapping `long`, or raw `long` plus a helper?
2. **Content ids.** String ids (`"setA_07"`, readable in saves and logs) or integers (smaller, faster)?
3. **Simulation step size.** 0.1 s is a guess; it sets how finely customer arrivals and patience resolve.
4. **Nullable reference types.** Enable `#nullable` in `Game.Core` (C# 8+, available in C# 9)? It catches missing dependencies at compile time but adds noise to every signature.
5. **Async.** Unity 6's `Awaitable` for scene loading and UI transitions, or coroutines? Not needed before M2.
6. **XML documentation enforcement.** Unity's generated projects don't emit documentation files, so missing `///` summaries can't raise CS1591. Review-only unless we find a better hook.
