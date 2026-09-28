# UI Style Guide: Card Show UI Kit

Source of truth for all in-game UI. Mockups live in `Docs/UI/Mockups/` (exported from the design canvas). Tokens live in `Assets/_Project/UI/Styles/Tokens.uss`, type roles in `Typography.uss`. If this guide and a mockup disagree, this guide wins.

## 1. Principles

1. **Quiet chrome, loud cards.** Panels are neutral grey. Colour in the UI always means something: teal = the action or selection, green/red = money up/down, tier colours = rarity.
2. **Name by what it is, not where it appears.** `CashReadout`, not `ShowDayMoney`. Every component must work at home and at the booth.
3. **Tokens only.** No hex values, font sizes, spacing or radii outside `Tokens.uss`. Need a new value? Add a token first.
4. **PC, mouse and keyboard.** No touch targets, no gamepad glyphs. Keyboard shortcuts are shown with the Keycap component.
5. **Diegetic where it's cheap.** The inventory is a physical binder. Prefer objects from a card vendor's world over generic windows.

## 2. Setup

- **PanelSettings:** Scale Mode = Scale With Screen Size, Reference Resolution 1920×1080, Screen Match Mode = Match Width Or Height, Match = 0.5. All sizes in this guide are px at 1920×1080. Check every screen at 1280×720 too.
- **Theme:** one Theme Style Sheet (`Assets/_Project/UI/Themes/GameTheme.tss`) that imports `Tokens.uss`, `Typography.uss` and `Components.uss`. Screen-specific sheets are attached per UXML.
- **Fonts:** Baloo 2 (SemiBold, Bold, ExtraBold) and Nunito (Regular, SemiBold, Bold, ExtraBold), both from Google Fonts (SIL Open Font License, fine for a commercial game). Put the .ttf files in `Assets/_Project/UI/Fonts/Baloo2/` and `.../Nunito/`, then create one SDF Font Asset per weight next to each file, named `<File> SDF.asset`. The paths in `Typography.uss` expect exactly those names.
- **Themes:** Dark Grey by default. Add `theme-light` to a screen root to use Light Grey. **The HUD is always dark.**

## 3. USS constraints (UI Toolkit, Unity 6.3)

These trip people up coming from web CSS. Mockups were drawn in HTML, so translate as follows:

| Web CSS in mockup | Do this in USS |
| --- | --- |
| `gap` | Not supported. Space children with margins (e.g. `margin-left: var(--space-3)` on all but the first). |
| CSS grid | Not supported. Use flex rows, or `flex-wrap: wrap` with fixed-width children. |
| `box-shadow` (the one elevation level) | Not supported. Skip it for now; later, a 9-sliced soft-shadow sprite behind the element. |
| dashed borders | Not supported. Use a 1px solid `--color-page-border` at `--opacity-subdued`. |
| `transform: rotate/translate` | Use the separate USS properties `rotate`, `translate`, `scale`. |
| `line-height` | Not a USS property. Comes from the font asset. |
| gradients, blur, filters | Don't. Flat fills, borders, radii, opacity only. No custom shaders. |

Supported and used by the kit: `var()`, `:root`, `rgba()`, `border-radius`, `opacity`, `text-shadow`, `letter-spacing`, `transition`, `position: absolute`, `overflow: hidden`, `:hover`, `:active`, `:focus`, `:disabled`.

## 4. Naming

- USS classes: `kebab-case`, component root = component name, parts use `__`, states use `--`. Example: `.cash-readout`, `.cash-readout__event`, `.cash-readout--sale`.
- Custom controls: C# `[UxmlElement] public partial class CashReadout : VisualElement`, in `Game.Unity.UI.Controls`. Expose data through `[UxmlAttribute]` properties or plain setters. Controls hold **no game logic**; a presenter pushes values in.
- Prototype code: namespace `Game.Unity.UI.Prototype`, folder `Prototype/`. See section 8.

## 5. Typography

| Role (class) | Font | Size token | Use |
| --- | --- | --- | --- |
| `.text-title` | Baloo 2 ExtraBold | `--font-size-2xl` 32 | Screen titles ("Binder") |
| `.text-heading` | Baloo 2 Bold | `--font-size-xl` 24 | Panel headings, "Day 8" |
| `.text-subheading` | Baloo 2 Bold | `--font-size-lg` 20 | Header bars, day type |
| `.text-label` | Baloo 2 SemiBold | `--font-size-sm` 16 | Buttons, tabs, chip names |
| `.text-body` | Nunito Regular | `--font-size-md` 18 | Sentences |
| `.text-caption` | Nunito SemiBold, muted | `--font-size-sm` 16 | Stat labels, hints |
| `.num-hud` | Nunito ExtraBold | `--font-size-hud` 44 | HUD clock and cash |
| `.num-lg` / `.num-md` / `.num-sm` | Nunito ExtraBold / ExtraBold / Bold | 32 / 20 / 14 | All other numbers |

Rules: **every numeral is Nunito** (money, counts, clock, XP, card ids). Use the real minus sign `−` (U+2212) for negative money. Positive and negative values always carry a sign **and** a caret glyph, never colour alone. Put `.text-on-world` on any text that sits over the 3D scene.

## 6. Components

Shared building blocks. Build each once in `Components.uss` + a control class, then reuse.

| Component | Anatomy | States |
| --- | --- | --- |
| **Panel** | `--color-surface` fill, 1px `--color-border`, `--radius-lg`. Optional header bar: 56 tall, `.text-subheading`, bottom border. Padding 16 / 24 / 32 for small / medium / large. | none |
| **Button** | 44 tall, padding 0 18, `--radius-md`, `.text-label` at `--font-size-md`, optional 18px icon on the left. Primary = accent fill + on-accent text. Secondary = raised fill + border. Ghost = no fill. | default, hover, pressed, focus (2px border), disabled (`--opacity-disabled`) |
| **Tier chip** | 28 tall, no fill, 1px border in `--color-tier-N`, 14px tier glyph in tier colour, name in `--color-text`. Compact variant shows the short code (C, U, R, H, FA, AI, SI). | none |
| **Stat pair** | Label (`.text-caption`) + value (`.num-*`). Stacked or inline. | normal, positive, negative |
| **Card face** (placeholder) | `--color-card-body` fill, border in tier colour (width ≈ card width / 40, min 3), `--radius-sm`. Glyph top-left, card id top-right (`.num-sm`, card-muted), name + tier name bottom-left. Aspect 63:88. | none |
| **Copy badge** | Min 40×30, accent fill, `--radius-md`, `.num-sm` 16 ExtraBold on-accent, e.g. `x2`. Sits over a card's bottom-right corner, offset −8/−8. | none |
| **Keycap** | Min 32×32, raised fill, 1px border with a 3px bottom border, `--radius-md`, Baloo 2 Bold 18. | none |
| **Interaction prompt** | Keycap + verb (`.text-subheading`) + object (muted). Centered, just below the crosshair. | shown, hidden |
| **Toast** | Panel, 340 wide: 32px status square (check icon, positive border) + title + detail + optional amount. | neutral, positive |
| **Tab** | 56 tall, sticks out of its container's edge. Active = accent fill + on-accent text, wider (156 vs 140). Inactive = surface fill. Label + count. | active, inactive, hover |

Icons: Phosphor only, regular or bold weight.

Rarity tiers follow the GDD's seven-tier ladder. Tier N uses `--color-tier-N` and glyph N:

| Tier | Name | Code | Glyph |
| --- | --- | --- | --- |
| 1 | Common | C | circle |
| 2 | Uncommon | U | triangle |
| 3 | Rare | R | square |
| 4 | Holographic | H | pentagon |
| 5 | Full Art | FA | hexagon |
| 6 | Alternate Illustration | AI | star |
| 7 | Special Illustration | SI | crown |

The UI kit's old tier names (Super Rare, Ultra Rare, Secret Rare, Mythic) are not used anywhere.

## 7. HUD

One UIDocument, always dark theme, 40px inset from screen edges. HUD backgrounds use `--color-hud-plate` (30% opacity), no border, no shadow, `--radius-lg`, and all HUD text gets `.text-on-world`. Bar tracks use `--color-track`.

**Top-left: DayClock** (320 wide, padding 18/20)
- Heading row: "Day {n}" (`.text-heading`, 26 ExtraBold) + day type (`.text-subheading`, muted): **"Prep Night"** or **"Show Day"**. No icons, no weekday.
- Clock row: time (`.num-hud`) + AM/PM (Baloo 2 Bold 20, muted).
- **Prep Night:** clock fixed at 10:00 PM. Below it, "It's Investin' time" (Baloo 2 Bold 18, accent). Nothing else.
- **Show Day:** live clock only. No meter, no footer. In the last in-game hour, "Closing soon" (Baloo 2 Bold 17, negative) appears right of the clock.

**Top-right: cluster**, laid out left to right as VendorLevel then CashReadout, top-aligned with a 12px margin between them. Toasts stack below the cluster, right-aligned.

> **v1 scope:** VendorLevel is *up for discussion* in GDD v1.5 and is **not built** in v1. Build CashReadout alone, right-aligned, so VendorLevel can slot in to its left later without moving anything. The VendorLevel spec below is kept for reference only.
- **VendorLevel** (280 wide, padding 16): 44×44 accent badge with the level number (`.num-md` 24, on-accent), then "Vendor level" (Baloo 2 Bold 16) with XP "420 / 800" (`.num-sm`, muted) on the right, then an 8px XP bar (track + accent fill).
  - States: idle; XP gained ("+25" positive before the XP count, ~1.5 s); level up ("Level up!" in accent replaces the XP count, badge number increments, bar resets).
- **CashReadout** (260 wide, padding 16/20): right-aligned `.num-hud` amount, **no label or icon**. Event line under it for ~2 s: label (caption) + signed delta with caret.
  - Sale: "Sale +$14.00" (positive). Purchase: "{item} −$150.00" (negative). **Table fee is not shown on the HUD**; it belongs on the Results screen.

**Centre:** crosshair (8px dot; becomes a 28px accent ring with a 6px dot when aiming at something usable) and the interaction prompt 58px below the screen centre.

**Bottom-right:** key hints (keycaps + labels) on a panel, e.g. Tab Inventory, F Booth setup, Esc Pause.

## 8. Prototypes (temporary UI)

Some screens are placeholders meant to be replaced. The **Binder inventory** is the first. Rules:

1. Layout lives in `Assets/_Project/UI/Prototype/<Screen>/` and code in `Assets/_Project/Scripts/Unity/UI/Prototype/<Screen>/`, namespace `Game.Unity.UI.Prototype`.
2. Every prototype file starts with: `// PROTOTYPE: temporary <Screen> UI. Replace, don't extend. See Docs/UI/UI_STYLE_GUIDE.md §8.`
3. A prototype may use shared kit components (Card face, Copy badge, Tab, Keycap, Panel) but **no shared component may depend on a prototype**.
4. A prototype talks to the game only through a small read-model interface defined outside the prototype folder (e.g. `IBinderReadModel` in `Game.Unity.UI`). Replacing the screen means deleting the prototype folder and writing a new view against the same interface.
5. No game rules in prototype code. Data comes from `GameSession` / Core services via the read model.

### Binder inventory (prototype) spec

- Full-screen view over a `--color-scrim` backdrop. Opened and closed with Tab.
- **Top bar** (panel, padding 12/24, width matches the binder): "Binder" (`.text-title`) + "{Set} · Singles" (muted) on the left; "Pages 3–4 of 4" on the right.
- **Binder:** cover `--color-cover`, `--radius-lg`, padding 20 / 20 / 44 (bottom leaves room for page numbers). Two pages with a 40px spine between them holding 3 ring outlines.
- **Page:** `--color-page` fill, padding 20, 3×3 pockets. Pocket 164×229 with 12px spacing, 1px `--color-page-border`, padding 6, containing a Card face (152×217). Page number centred below each page.
- **Pocket contents:** Card face + Copy badge. **No prices, no stats** in this screen. Empty pocket = subdued border + "Empty pocket" caption.
- **Selected pocket:** 2px accent border, `translate: 0 -8px`.
- **Tabs** on the binder's right edge: Set A, Set B, Sealed, Bulk, each with a count.
- **Key hints** at the bottom: A/D page, Q/E tab, Enter card details, Tab close.
- Card details (stats, 7-day price line) is a separate screen, out of scope for the prototype.
