# Unity + Claude Code Dev Environment Setup

2026-09-19 · Christian · Windows 11, PowerShell, VS Code

First used for: **TCG Card Show Simulator (v1)** · Reusable for any Unity project

## TL;DR decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Unity version | **Unity 6.3 LTS** (latest `6000.3.x` patch in the Hub) | Supported until Dec 2027, matches Unity Learn and most Asset Store packages. Revisit 6.7 LTS once it ships and has a few patches. |
| Render pipeline | **URP** (Universal 3D template) | Works for both art-style options still open in the GDD (low-poly 3D booth or 2D). |
| IDE | **VS Code** + Unity extension + C# Dev Kit (you already run Claude Code here) | Free, fits your existing Claude Code workflow. Rider is the upgrade path. |
| AI coding | **Claude Code** (CLI + VS Code extension) | Terminal agent that edits the repo, runs git, and drives Unity through MCP. |
| Unity ↔ Claude bridge | **MCP for Unity** (CoplayDev, free, MIT) | Lets Claude read the console, inspect scenes, create objects, and run tests. |
| Version control | **Git + Git LFS + GitHub** | You already have the GitHub MCP and Issues → PR → board flow set up. |
| Architecture | Plain C# rules in a separate assembly, Unity layer on top | Straight from the GDD. Makes Market/Negotiation unit-testable and Claude-friendly. |

## 1. Which Unity version and why

As of September 2026 the Unity 6 line has three relevant options:

- **Unity 6.0 LTS** reaches end of support in October 2026. Skip it.
- **Unity 6.3 LTS** is supported until December 2027. This is the pick.
- **Unity 6.6** (a "Supported Update", released Sept 1, 2026) has the newest features (CoreCLR groundwork, faster Enter Play Mode, uGUI improvements) but is only supported until the next release, and features can change before 6.7 LTS.

Unity 6.7 LTS is expected later in 2026, and the "Unity 7" generation (effectively 6.8) targets a beta in December 2026. The plan: build v1 on 6.3 LTS, and consider a single upgrade to 6.7 LTS between milestones (for example after M2 or M3) once it has had a couple of patches. Always commit before upgrading.

**License:** Unity Personal is free while revenue stays under the Personal threshold (currently $200K/year). Fine for this project.

## 2. What to download

Everything below is free unless marked.

| Tool | Purpose | Get it |
| --- | --- | --- |
| Unity Hub | Installs and manages Unity editors | `winget install Unity.UnityHub` or unity.com/download |
| Unity 6.3 LTS | The engine | Via Unity Hub (see section 3) |
| Git for Windows | Version control; also gives Claude Code its Bash tool | `winget install Git.Git` (includes Git LFS) |
| .NET 10 SDK | Needed by the VS Code C# extensions and the C# language server | `winget install Microsoft.DotNet.SDK.10` |
| VS Code | Code editor | `winget install Microsoft.VisualStudioCode` |
| Python 3.12 + uv | Runs the MCP for Unity server | `winget install Python.Python.3.12` and `winget install astral-sh.uv` |
| Claude Code | AI coding agent | `irm https://claude.ai/install.ps1 \| iex` (PowerShell) |
| Windows Terminal | Nicer terminal for Claude Code | Usually preinstalled; `winget install Microsoft.WindowsTerminal` |
| Optional: GitHub Desktop or Fork | Visual git client, handy for reviewing Claude's diffs | `winget install GitHub.GitHubDesktop` |
| Optional: JetBrains Rider | Best-in-class Unity C# IDE | Free for non-commercial use; needs a paid licence if you sell the game |

After installing, open a **new** PowerShell window (PATH changes don't reach open terminals) and check:

```powershell
git --version
git lfs install        # one-time, enables LFS for your user
dotnet --list-sdks
uv --version
claude --version
```

## 3. Install Unity

1. Open Unity Hub and sign in with your Unity ID.
2. **Installs → Install Editor →** pick the newest **Unity 6.3 LTS** (`6000.3.x`).
3. Modules to tick:
   - **Windows Build Support (IL2CPP)**: for release builds (Steam).
   - **Documentation**: offline docs, handy for Claude and for you.
   - **Untick** "Microsoft Visual Studio Community" if you're using VS Code.
   - Skip Android/iOS/WebGL for now; you can add modules later.
4. Hub → **Preferences → Installs**: put editors on a fast SSD with plenty of space (each editor is roughly 5–10 GB).

## 4. VS Code setup for Unity

Do this after creating the project in section 5. The goal is IntelliSense that stays in sync with your assemblies, a working debugger, Unity-aware code warnings, and an editor that ignores Unity's generated folders.

### 4.1 How the pieces fit together

| Piece | Where | What it does |
| --- | --- | --- |
| .NET 10 SDK | Windows | Runs the C# language server that powers IntelliSense |
| **C#** + **C# Dev Kit** extensions | VS Code | IntelliSense, refactoring, solution explorer, test explorer |
| **Unity** extension (Microsoft) | VS Code | Unity debugger, Unity-specific analyzers, Unity message autocomplete |
| **Visual Studio Editor** package | Unity | Generates the `.sln`/`.csproj` files VS Code reads, and opens scripts in VS Code |

If IntelliSense ever breaks, the cause is almost always the last row: the project files are stale.

### 4.2 Install the extensions

```powershell
code --install-extension VisualStudioToolsForUnity.vstuc   # Unity (debugger + analyzers)
code --install-extension ms-dotnettools.csdevkit            # C# Dev Kit
code --install-extension ms-dotnettools.csharp              # C# language support
code --install-extension anthropic.claude-code              # Claude Code
code --install-extension EditorConfig.EditorConfig          # .editorconfig for non-C# files
code --install-extension usernamehw.errorlens               # Optional: errors shown inline
code --install-extension TimGJones.hlsltools                # Optional: shader syntax/IntelliSense
```

C# Dev Kit follows Visual Studio Community's licence terms: free for individual developers, including commercial solo projects.

### 4.3 Configure Unity to use VS Code

1. **Window → Package Manager → In Project:** find **Visual Studio Editor** (`com.unity.ide.visualstudio`) and update it to the newest version. The VS Code Unity extension needs 2.0.20 or newer.
2. If an old **Visual Studio Code Editor** package (`com.unity.ide.vscode`) is present, remove it. It's deprecated and conflicts.
3. **Edit → Preferences → External Tools → External Script Editor:** choose **Visual Studio Code**. If it isn't listed, choose *Browse…* and pick `Code.exe`.
4. Under **Generate .csproj files for**, tick **Embedded packages** and **Local packages**. Leave the rest off to keep the solution small.
5. Click **Regenerate project files**. A `TCGCardShowSim.sln` should appear in the project root.
6. Double-click any script in Unity. VS Code should open the **project folder** (not just the file). Wait for the C# Dev Kit status bar to finish loading the solution.

### 4.4 Workspace files (commit these)

These live in `.vscode/` and `.editorconfig` at the project root. The `.gitignore` in the appendix already keeps the three `.vscode` files tracked.

**`.vscode/settings.json`** hides Unity's generated folders, stops VS Code from watching them (less CPU), and points C# Dev Kit at the right solution:

```json
{
  "dotnet.defaultSolution": "TCGCardShowSim.sln",
  "files.exclude": {
    "**/*.meta": true,
    "Library": true,
    "Temp": true,
    "Logs": true,
    "obj": true,
    "UserSettings": true,
    "MemoryCaptures": true
  },
  "search.exclude": {
    "Library": true,
    "Temp": true,
    "Logs": true,
    "obj": true,
    "**/*.csproj": true,
    "**/*.sln": true
  },
  "files.watcherExclude": {
    "**/Library/**": true,
    "**/Temp/**": true,
    "**/Logs/**": true,
    "**/obj/**": true
  },
  "files.associations": {
    "*.uxml": "xml",
    "*.uss": "css",
    "*.asmdef": "json",
    "*.inputactions": "json"
  },
  "editor.rulers": [120]
}
```

**`.vscode/launch.json`** adds the debugger configuration:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach to Unity",
      "type": "vstuc",
      "request": "attach"
    }
  ]
}
```

**`.vscode/extensions.json`** makes VS Code prompt for the right extensions on any machine that opens the repo:

```json
{
  "recommendations": [
    "visualstudiotoolsforunity.vstuc",
    "ms-dotnettools.csdevkit",
    "ms-dotnettools.csharp",
    "anthropic.claude-code",
    "editorconfig.editorconfig",
    "usernamehw.errorlens"
  ]
}
```

**`.editorconfig`** enforces the code conventions from `CLAUDE.md`. C# Dev Kit reads it for warnings and formatting, and Claude's code gets checked against the same rules:

```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{json,asmdef,uxml,uss,yml,yaml,md}]
indent_size = 2

[*.cs]
# Unity 6.3 compiles C# 9: file-scoped namespaces (C# 10) are not available
csharp_style_namespace_declarations = block_scoped:warning
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
dotnet_sort_system_directives_first = true

# Private fields: _camelCase
dotnet_naming_rule.private_fields_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_underscore.style = underscore_camel
dotnet_naming_rule.private_fields_underscore.severity = suggestion
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.underscore_camel.capitalization = camel_case
dotnet_naming_style.underscore_camel.required_prefix = _
```

Unity hides the leading underscore in the Inspector, so `[SerializeField] private int _tableFee;` shows as "Table Fee".

### 4.5 Debugging

1. In the Unity Editor's status bar (bottom right), click the bug icon and switch **Code Optimization** to **Debug Mode**. Release mode ignores breakpoints. VS Code will also offer to switch when you attach.
2. In VS Code, set a breakpoint, open **Run and Debug**, pick **Attach to Unity**, press **F5**, and choose the Editor instance.
3. Press Play in Unity. Execution stops at your breakpoint; you get locals, watch, call stack and stepping.
4. Switch back to Release mode when you're not debugging; Debug mode makes the Editor slower.

Core services (`MarketService`, `NegotiationSession`…) are easier to debug through EditMode tests: set a breakpoint in the test, attach, then run the test from Unity's **Window → General → Test Runner**.

### 4.6 Daily habits that avoid most problems

- **Create, move and rename scripts in Unity's Project window, not in VS Code's explorer.** Every file has a `.meta` with its GUID; moving a `.cs` without its `.meta` breaks every scene and prefab reference to that script. Creating new files from VS Code or Claude is fine (Unity generates the `.meta`), but moves and renames belong in Unity, or via `git mv` of both files.
- **After adding or changing an `.asmdef`, regenerate project files** (4.3 step 5). This is the most common cause of red squiggles on code that compiles fine.
- **Trust the Unity console over VS Code.** Unity's compiler is the source of truth; VS Code's diagnostics are a helpful preview.
- **Keep Unity open while coding.** It recompiles as you save and keeps project files current.

### 4.7 Troubleshooting

| Symptom | Fix |
| --- | --- |
| No IntelliSense, or "project not loaded" | Regenerate project files in Unity, then in VS Code run **Developer: Reload Window** from the Command Palette (Ctrl+Shift+P) |
| Red errors in VS Code but Unity compiles fine | Stale project files after an `.asmdef` change: regenerate, then run **Restart Language Server** from the Command Palette |
| New script's types not found | Unity hasn't imported it yet: switch to Unity, let it compile, come back |
| Unity opens scripts in Notepad or Visual Studio | External Script Editor isn't set to VS Code (4.3 step 3) |
| No Unity analyzers or Unity message autocomplete | Visual Studio Editor package is outdated, or the Unity extension isn't installed |
| Debugger won't attach | Editor is in Release mode, or Windows Firewall blocked it; allow Unity and VS Code on private networks |
| C# language server errors mentioning the SDK | .NET SDK missing or not on PATH: `dotnet --list-sdks`, then restart VS Code |
| VS Code slow or fans spinning | Check `files.watcherExclude` covers `Library/` and `Temp/` |

## 5. Create the project

1. Unity Hub → **New project → Universal 3D** (URP). Name it `TCGCardShowSim`. Put it in your git projects folder, for example `C:\dev\TCGCardShowSim`.
2. Connecting to Unity Cloud is optional; you don't need it for anything in this guide.

### Project settings checklist

| Where | Setting | Value / why |
| --- | --- | --- |
| Project Settings → Editor | Version Control mode | **Visible Meta Files** |
| Project Settings → Editor | Asset Serialization | **Force Text** (readable diffs, lets Claude read scenes/prefabs) |
| Project Settings → Editor | Enter Play Mode Settings | **Do not reload Domain or Scene** for fast iteration. Rule: reset any static state in code. Switch back if weird bugs appear. |
| Project Settings → Player | Active Input Handling | **Input System Package (New)** |
| Project Settings → Player | Company / Product name | Set now; it affects save-file paths |
| Project Settings → Player → Other | Api Compatibility Level | **.NET Standard 2.1** (default is fine) |

### Packages (Window → Package Manager)

Already in the URP template: Input System, Unity Test Framework, uGUI (includes TextMeshPro in Unity 6).

Add:

| Package | How | Used for |
| --- | --- | --- |
| Newtonsoft Json | Add by name: `com.unity.nuget.newtonsoft-json` | `SaveService` JSON (handles dictionaries, polymorphism) |
| MCP for Unity | Add from git URL (section 8) | Claude ↔ Editor bridge |
| Optional: Cinemachine | Unity Registry | Only if the booth camera needs movement later |

### Folder and assembly layout

This mirrors the GDD's "rules in plain C#" architecture. Assembly Definitions (`.asmdef`) keep compile times low and enforce the boundary so game rules can't accidentally depend on MonoBehaviours.

```
Assets/
  _Project/
    Scripts/
      Core/                    Game.Core.asmdef   (No Engine References ON)
        Market/                MarketService, price formula, news events
        Inventory/             InventoryService, cost basis
        Packs/                 PackOpener, RarityTable logic
        Show/                  ShowSimulation, NegotiationSession
        Economy/               EconomyService
        Save/                  SaveService (state DTOs)
        Random/                Seeded RNG wrapper
      Unity/                   Game.Unity.asmdef  (references Game.Core)
        Data/                  ScriptableObject definitions (CardDefinition, etc.)
        Flow/                  GameStateMachine, GameSession bootstrap
        UI/                    Screens and views
        Debug/                 Debug console commands
    Data/                      ScriptableObject assets (cards, sets, products, archetypes)
    Scenes/                    Boot, Home, Venue
    Art/  Audio/  Prefabs/  UI/
  Tests/
    EditMode/                  Game.Core.Tests.asmdef (references Game.Core, Test Assemblies)
```

Notes:

- **`Game.Core` with "No Engine References"** means it cannot use `UnityEngine` at all. That's the point: `MarketService`, `NegotiationSession` and friends become pure C#, fast to test and trivial for Claude to reason about. Use `System.Random` or your own seeded RNG there, never `UnityEngine.Random`.
- ScriptableObjects live in `Game.Unity` and are converted into plain data for Core at startup.
- EditMode tests run in milliseconds and cover the balance maths (Rip EV ≈ 85%, negotiation rules, price formula). This is also how the GDD's "1,000 simulated pack openings" debug check becomes a real test.

## 6. Git + GitHub setup

From the project root:

```powershell
git init
# add .gitignore and .gitattributes from the appendix first
git lfs install
git add .
git commit -m "Initial Unity 6.3 LTS project"
gh repo create tcg-card-show-sim --private --source . --push   # or create on github.com and push
```

Optional but useful: configure **Unity Smart Merge** so scene/prefab conflicts merge semantically (replace the version folder with yours):

```powershell
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.3.XfY/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

Working rule with Claude Code: **commit before every agent session** and review with `git diff` (or GitHub Desktop) after. Cheap undo button.

## 7. Claude Code setup

You already have Claude Code on Windows with PowerShell and VS Code. For this project:

1. **Update:** native installs auto-update; confirm with `claude --version`. If `claude` isn't found after install, open a new terminal.
2. **Git Bash:** with Git for Windows installed, Claude Code uses Git Bash for shell commands; without it, it falls back to PowerShell. If it can't find Git Bash, set `CLAUDE_CODE_GIT_BASH_PATH` to `C:\Program Files\Git\bin\bash.exe` in `~/.claude/settings.json` under `env`.
3. **Start in the project root:** `cd C:\dev\TCGCardShowSim` then `claude`, or use the Claude Code panel in VS Code.
4. **Run `/init`** once, then replace the generated file with the `CLAUDE.md` template in the appendix. CLAUDE.md is read at the start of every session, so it's where Unity rules and GDD context live.
5. **Add `.claude/settings.json`** (appendix) to keep Claude out of `Library/` and `Temp/`, block `.meta` edits, and ask before touching project settings.
6. **Drop the GDD into the repo** at `Docs/GDD.md` so Claude can read it directly.

### How to work with Claude on a Unity project

- **Plan first for anything bigger than one file:** use plan mode (Shift+Tab) and have Claude propose classes and files before writing them.
- **Test-first for Core:** "Write EditMode tests for `NegotiationSession` covering rules 1–5 in the GDD, then implement until they pass." Pure C# rules make this loop fast.
- **Let Claude close the loop through MCP:** after edits, ask it to refresh, wait for compilation, read the console and fix errors.
- **Keep scenes and prefabs manual or MCP-driven.** Claude shouldn't hand-edit `.unity` / `.prefab` YAML; it should use MCP tools, or tell you what to wire up in the Inspector.
- **One milestone per branch:** `m1-market-sim`, `m2-packs`… Use the GitHub MCP to open issues for each GDD milestone row and PRs when done.
- **Custom commands/skills:** repeated prompts (e.g. "run EditMode tests and summarise failures", "balance check: simulate 1,000 packs per set") can live in `.claude/commands/` or `.claude/skills/` so they become one-word slash commands.

## 8. MCP servers

MCP servers give Claude Code tools beyond reading files. Recommended set, in order of value:

| MCP / plugin | What it gives Claude | Cost | Priority |
| --- | --- | --- | --- |
| **MCP for Unity** (CoplayDev) | Live Editor control: console, scenes, GameObjects, components, assets, scripts, tests, builds | Free, MIT | Essential |
| **GitHub MCP** | Issues, PRs, project board | Free | Already set up; reuse |
| **Context7** | Up-to-date docs for libraries (Unity packages, Newtonsoft, PrimeTween…) instead of stale training data | Free tier | Recommended |
| **Microsoft Learn MCP** | Official C#/.NET docs search | Free | Nice to have |
| **C# LSP plugin** (`csharp-lsp`) | Go-to-definition, find references, diagnostics for `.cs` files | Free | Optional; some Windows setups report it being flaky |
| Unity official MCP (AI Assistant package) | Similar Editor access, built by Unity | Needs a Unity AI trial/subscription and Unity Cloud link | Alternative only |

### 8.1 MCP for Unity (the important one)

Requirements: Unity 2021.3 LTS through 6.x, Python 3.10+ via `uv`.

1. In Unity: **Window → Package Manager → + → Add package from git URL** and enter:
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
   (For a stable build, pin a release tag instead of `#main`, e.g. `#v10.0.0` or whatever is newest.)
2. **Window → MCP for Unity → Configure All Detected Clients.** It detects Claude Code and writes the config.
3. In a terminal in the project root: `claude mcp list` should show the Unity server as connected. Inside Claude Code, `/mcp` shows its tools.
4. Smoke test: *"Read the Unity console and summarise any errors"*, then *"Create an empty GameObject called Bootstrap in the open scene."*

Tips: Unity must be open with the project loaded for the tools to work. The package's tool groups (UI, testing, animation…) can be toggled to keep Claude's tool list small. The repo also ships a Claude skill (`unity-mcp-skill`) worth a look. Security: this gives the agent real control over your editor, so only point it at content you trust and keep commits frequent.

### 8.2 Context7 and Microsoft Learn

```powershell
claude mcp add --transport http context7 https://mcp.context7.com/mcp
claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp
```

Add `--scope user` to make them available in every project. Check each provider's page if a URL has changed. Usage: "use context7 to check the current PrimeTween API for sequences."

### 8.3 C# language server plugin (optional)

```powershell
dotnet tool install --global csharp-ls
```

Then in Claude Code: `/plugin` → find **csharp-lsp** in the official marketplace → install → restart Claude Code. Unity must have generated the `.sln` (section 4.3). If Claude doesn't show LSP tools after a restart, skip it; grep plus MCP for Unity's compile/console loop works fine.

### Scope: user vs project

- **User scope** (every project): Context7, Microsoft Learn, GitHub.
- **Project scope** (`.mcp.json` committed in the repo): MCP for Unity, since it's tied to one Unity project.

## 9. Recommended plugins and third-party tools

### Essential (free)

| Tool | Why for this game | Install |
| --- | --- | --- |
| **Unity Test Framework** | EditMode tests for all Core services | Already in template |
| **Newtonsoft Json** | Save/load full run state | `com.unity.nuget.newtonsoft-json` |
| **In-game Debug Console** (yasirkula) | GDD debug tools: jump days, add cash, force events. `[ConsoleMethod]` turns a static method into a command | OpenUPM `com.yasirkula.ingamedebugconsole` or Asset Store |
| **PrimeTween** | Card flip, rarity glow, UI juice. Zero-allocation tweens | Asset Store (free) or OpenUPM `com.kyrylokuzyk.primetween` |
| **UI Toolkit** (built in) | Great fit for the UI-heavy home computer screens (CardTrader app, supplier shop). Use uGUI for world-space bits in the booth | Built in |

### Nice to have

| Tool | Why | Cost |
| --- | --- | --- |
| NaughtyAttributes or Tri-Inspector | Cleaner Inspectors for the many ScriptableObjects (cards, sets, archetypes) | Free |
| Odin Inspector | Best-in-class editor tooling; builds data tables and validation for SOs | Paid |
| Hot Reload for Unity | Edit C# during Play Mode without recompiling; big time saver for tuning negotiation feel | Paid, free trial |
| Graphy | FPS/memory overlay | Free |
| vHierarchy / vFolders | Tidier Hierarchy and Project windows | Paid, cheap |
| DOTween | Alternative to PrimeTween if you already know it | Free (Pro paid) |

### Later (post-M4)

| Tool | When |
| --- | --- |
| **GameCI** (GitHub Actions) | Run EditMode tests and Windows builds on every PR |
| **Steamworks.NET** | When you commit to a Steam release |
| **Unity Localization** | If you plan other languages |
| **Addressables** | Only if content grows large; v1 doesn't need it |

**OpenUPM tip:** add packages from OpenUPM with its CLI (`npm install -g openupm-cli`, then `openupm add <package>`) or add a scoped registry in Project Settings → Package Manager.

## 10. First-session checklist

1. Unity 6.3 LTS project opens with no console errors.
2. VS Code shows IntelliSense on a MonoBehaviour, Unity analyzers flag an empty `Update()`, and "Attach to Unity" hits a breakpoint in Debug mode.
3. `git status` is clean after first commit; `Library/` is not tracked.
4. `claude` starts in the project root and reads `CLAUDE.md` (ask it "what are this project's architecture rules?").
5. `claude mcp list` shows Unity (connected), GitHub, Context7.
6. Claude can read the Unity console and create a GameObject through MCP.
7. Kick off M1 with Claude: create `Game.Core`, `Game.Unity` and `Game.Core.Tests` asmdefs, a seeded RNG, and a first failing test for `price(day) = basePrice × trend × eventMult × noise`.

## 11. Reusing this for other projects

| Layer | Set once (global) | Per project |
| --- | --- | --- |
| Tools | Hub, Git, .NET SDK, VS Code + extensions, Python/uv, Claude Code | Unity version choice, `.vscode/` files, `.editorconfig` (update `dotnet.defaultSolution`) |
| Claude | User-scope MCPs (GitHub, Context7, MS Learn), `~/.claude/settings.json` | `CLAUDE.md`, `.claude/settings.json`, `.mcp.json` |
| Unity | Hub editor installs, Smart Merge git config | Packages, asmdefs, project settings |
| Git | `git lfs install`, global config | `.gitignore`, `.gitattributes`, repo |

For a new project: copy the appendix files, swap the GDD section of CLAUDE.md, install MCP for Unity in the new project, and run the checklist.

---

## Appendix A: `CLAUDE.md` (project root)

```markdown
# TCG Card Show Simulator — Claude instructions

## Project
Solo Unity 6.3 LTS (URP) game. The player is a card vendor: read a moving market, buy sealed
product, rip or hold, and haggle at weekend card shows. Full design: Docs/GDD.md (source of truth).
v1 scope is intentionally small. Do not add features outside the GDD's "In v1" column.

## Architecture rules
- All game rules live in Assets/_Project/Scripts/Core (assembly Game.Core, NO engine references).
  No UnityEngine types there. Services: MarketService, InventoryService, PackOpener,
  ShowSimulation, NegotiationSession, EconomyService, SaveService.
- Unity-facing code (MonoBehaviours, ScriptableObjects, UI) lives in Scripts/Unity (Game.Unity).
  It talks to Core only through the GameSession object.
- All randomness goes through the seeded RNG in Core. Never use UnityEngine.Random for game logic.
- Money is stored as integer cents (long), not float.
- Enter Play Mode has domain reload disabled: never rely on static fields keeping default values;
  reset statics explicitly.

## Workflow
- For Core changes: write or update EditMode tests in Assets/Tests/EditMode first, then implement.
- After editing C#: use the Unity MCP to refresh, wait for compilation, and read the console.
  Fix all errors and new warnings before reporting done.
- Run EditMode tests via the Unity MCP test tool and report pass/fail counts.
- Do not hand-edit .unity, .prefab or .asset YAML. Use Unity MCP tools or tell me what to set up
  in the Inspector.
- Never create, edit or delete .meta files. Unity manages them.
- Never move or rename existing scripts/assets with plain file moves. Use the Unity MCP asset tools,
  or `git mv` the file and its .meta together.
- After adding or changing an .asmdef, ask me to regenerate project files (or use the Unity MCP).
- Never touch Library/, Temp/, Logs/, obj/ or UserSettings/.
- Ask before changing ProjectSettings/ or Packages/manifest.json.
- Keep changes scoped to the current milestone (M1–M5 in the GDD).

## Conventions
- C#: PascalCase types/methods, _camelCase private fields, one public type per file,
  block-scoped namespaces: Game.Core.Market, Game.Unity.UI, etc.
- Unity 6.3 compiles C# 9. Do not use C# 10+ features (file-scoped namespaces, global usings,
  record structs, required members). init/records need an IsExternalInit shim in Core.
- Follow .editorconfig for formatting and naming.
- ScriptableObjects: [CreateAssetMenu(menuName = "TCG/...")], names end in Definition.
- Prefer small, pure methods in Core; no LINQ in per-frame Unity code.
- Commit messages: "M1: <what changed>".
```

## Appendix B: `.claude/settings.json` (project)

```json
{
  "permissions": {
    "deny": [
      "Read(./Library/**)",
      "Read(./Temp/**)",
      "Read(./Logs/**)",
      "Read(./obj/**)",
      "Edit(**/*.meta)"
    ],
    "ask": [
      "Edit(./ProjectSettings/**)",
      "Edit(./Packages/manifest.json)"
    ],
    "allow": [
      "Bash(git status)",
      "Bash(git diff:*)",
      "Bash(git log:*)"
    ]
  }
}
```

Put personal overrides in `.claude/settings.local.json` (git-ignored).

## Appendix C: `.gitignore`

```gitignore
# Unity generated
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
/[Rr]ecordings/

# Asset meta data should only be ignored when the corresponding asset is also ignored
!/[Aa]ssets/**/*.meta

# IDE / generated project files
.vs/
.idea/
.vscode/*
!.vscode/settings.json
!.vscode/launch.json
!.vscode/extensions.json
*.csproj
*.sln
*.slnx
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Builds
*.apk
*.aab
*.unitypackage
*.app

# Crash reports / misc
sysinfo.txt
crashlytics-build.properties
/[Aa]ssets/[Ss]treaming[Aa]ssets/aa/*

# Claude Code personal settings
.claude/settings.local.json

# OS
.DS_Store
Thumbs.db
```

## Appendix D: `.gitattributes`

```gitattributes
* text=auto

# Code
*.cs      text diff=csharp
*.shader  text
*.hlsl    text
*.uss     text
*.uxml    text
*.json    text
*.md      text

# Unity YAML (Smart Merge driver configured in section 6)
*.unity       merge=unityyamlmerge eol=lf
*.prefab      merge=unityyamlmerge eol=lf
*.asset       merge=unityyamlmerge eol=lf
*.meta        merge=unityyamlmerge eol=lf
*.mat         merge=unityyamlmerge eol=lf
*.anim        merge=unityyamlmerge eol=lf
*.controller  merge=unityyamlmerge eol=lf
*.physicMaterial merge=unityyamlmerge eol=lf

# Binary assets via Git LFS
*.png  filter=lfs diff=lfs merge=lfs -text
*.jpg  filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text
*.tga  filter=lfs diff=lfs merge=lfs -text
*.exr  filter=lfs diff=lfs merge=lfs -text
*.fbx  filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text
*.wav  filter=lfs diff=lfs merge=lfs -text
*.mp3  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.ttf  filter=lfs diff=lfs merge=lfs -text
*.otf  filter=lfs diff=lfs merge=lfs -text
*.mp4  filter=lfs diff=lfs merge=lfs -text
```

## Appendix E: Useful links

- Unity downloads and release notes: unity.com/download · unity.com/releases/unity-6
- Claude Code setup docs: code.claude.com/docs/en/setup
- MCP for Unity: github.com/CoplayDev/unity-mcp (docs: coplaydev.github.io/unity-mcp)
- Unity official MCP (alternative): unity.com/blog/unity-ai-mcp-how-to-get-started
- Unity .gitignore reference: github.com/github/gitignore (Unity.gitignore)
- OpenUPM: openupm.com
