---
name: bannerlord-debug
description: >
  Debugging guide for DADG / Bannerlord issues — crashes, exceptions, wrong game
  behaviour. Read this skill whenever the task involves debugging a crash, analysing
  a CLR exception, investigating unexpected game behaviour, tracing a bug through
  mod code or game data, or iterating fix → relaunch → verify cycles.
---

# DADG Bannerlord Debugging Workflow

---

## 0 — Read ALL knowledge bases before writing a single line of code

**Stop. Read them all first.** Many "obvious" fixes turn out to be wrong or already handled
once you understand the full picture. This step has saved (and cost) hours.

- `.claude/knowledge/dadg-project-overview.md` — architecture, build commands, MCP tools, DI pattern
- `.claude/knowledge/dadg-feature-domains.md` — every feature domain and where the C# lives
- `.claude/knowledge/dadg-module-structure.md` — multi-module layout (Core, Map, Scenes, Content)
- `.claude/knowledge/dadg-gabs-protocol.md` — GABS/GABP bridge details
- `.claude/knowledge/bannerlord-debugging-workflow.md` — CLR exception strategy (critical)

DADG is **multi-module**. The bug you're chasing may not be in C# at all — it could be in:
- XML data (`../DellarteDellaGuerra/ModuleData/`)
- Scene entities (`../DellarteDellaGuerraScenes/SceneObj/<scene>/scene.xscene`)
- Campaign map entities (`../DellarteDellaGuerraMap/SceneObj/Main_map/scene.xscene`)

---

## 1 — CLR exception strategy (critical)

See `.claude/knowledge/bannerlord-debugging-workflow.md` for full detail. Summary:

**During loading** — resume immediately. These are handled engine exceptions (e.g. `MBObjectManager.cs:993` XML parsing). They are noise.

**During campaign or mission** — **STOP. Do not resume.** A first-chance CLR exception during gameplay means Bannerlord will crash on resume. This is your best debugging window.

When paused on a campaign/mission exception:
1. Read the full stack trace and current source location
2. Inspect variables via `mcp__jetbrains-debugger__get_variables` or `evaluate_expression`
3. Search Bannerlord source (`mcp__bannerlord-search-1_3_1__search_bannerlord_code`) for context
4. Reason about root cause before acting
5. Only resume when you have a diagnosis and intend to write a fix, or are deliberately discarding the session

The hook at `http://localhost:7777/state` blocks GABS tool calls when the debugger is paused — intentional. Resume the debugger before polling the game.

---

## 2 — Standard fix → relaunch cycle

### 2a. Check if already running

First call `mcp__bannerlord-game-controller__games_status` with `gameId: "bannerlord"`:
- **Connected** → skip reset and launch, go straight to checking game state
- **Disconnected / stopped** → proceed with reset + start below

### 2b. Reset the session

Use the **`bannerlord-gabs-session-reset`** skill to kill Bannerlord and clean GABS state.

### 2c. Launch with debugger

Use the **`bannerlord-gabs-start`** skill (with optional save name argument). This:
- Writes `bridge.json` with the fixed port/token
- Ensures `AssertAutoIgnore.exe` is running (auto-dismisses Bannerlord popups)
- Starts the JetBrains debug session (`DellarteDellaGuerra.Integration: Standalone`)
  - The debug config triggers a build automatically — if the session fails to start, check the Rider build output for compile errors
- Waits for `runtime.json` via `Bash run_in_background` (handles any load time, fast or slow)
- Connects GABP and reports blockers

### 2d. Resume loading-phase exceptions

During save load, a CLR exception at `MBObjectManager.cs:993` is expected. Check via `get_debug_session_status` — if it's that known location, resume. If it's somewhere else, investigate per §1.

---

## 3 — Polling the game (do not stop)

Once the game is running, **keep polling**. A one-shot check tells you nothing.

```
bannerlord.core.get_game_state    # state, campaign time
bannerlord.core.check_blockers    # paused? menu active? inquiry?
bannerlord.core.get_campaign_time # time advancing?
```

To run time at max speed:
```
bannerlord.core.set_time_speed {"speed": 4, "unpause": true}
```

Poll every ~30 seconds. If the debugger hook blocks a GABS call (`isPaused: true`), check the session status immediately — it may be the crash exception you're waiting for.

If GABP disconnects unexpectedly, call `mcp__jetbrains-debugger__get_debug_session_status` (50 frames, include_variables: true) to inspect the crash state. The Rider session stays open after a crash — no rush, take your time to investigate. If the session is already gone, fall back to crash logs at `%AppData%\Mount and Blade II Bannerlord\logs\`.

---

## 4 — Setting breakpoints and adding logs

### Breakpoints

Set via `mcp__jetbrains-debugger__set_breakpoint` (file + line). Useful for:
- Watching a specific method before the crash
- Checking values at a known code path
- Conditional breakpoints to catch specific objects or states

After a breakpoint fires, inspect variables and evaluate expressions before resuming.

### Logging

DADG uses NLog. Log file: `../log/` (relative to `src/`). Add calls via the injected `ILogger`:

```csharp
_logger.Info($"Defender cache length: {cache.Length}, slotIndex: {slotIndex}");
_logger.Warn($"Settlement {settlement.Name} has no defender engine slots at wall level {wallLevel}");
```

Rebuild (or let the debug config build on launch), then grep the log to observe behaviour:
```bash
grep -i "defender cache" "../log/dadg.log" | tail -20
```

Logging is especially useful in mission code where interactive breakpoints are awkward (battles move fast).

---

## 5 — Investigating data bugs

Many DADG bugs are not C# bugs — they are data bugs.

### XML data

Grep the XML directly across all modules:
```
../DellarteDellaGuerra/ModuleData/
../DellarteDellaGuerraMap/ModuleData/
../Native/ModuleData/          ← vanilla reference
../SandBox/ModuleData/         ← vanilla reference
../SandBoxCore/ModuleData/     ← vanilla reference
```

Compare DADG XML against vanilla to find missing entries, wrong IDs, or misconfigured values.

### Scene files (xscene)

Scene files live in:
```
../DellarteDellaGuerraScenes/SceneObj/<scene_name>/scene.xscene   ← mission scenes
../DellarteDellaGuerraMap/SceneObj/Main_map/scene.xscene          ← campaign map
../SandBox/SceneObj/Main_map/scene.xscene                         ← vanilla map reference
```

⚠️ **xscene files are enormous** (20+ MB). Never read them whole. Grep only:
```
Grep pattern="map_defensive_engine" path="...scene.xscene" context=5
```

Compare DADG scenes against vanilla to find missing entity types, wrong tags, or missing
upgrade level masks. Example from this codebase: DADG was missing `defender_ranged_l1` and
`defender_ranged_l2` entities entirely — every settlement only had `l3`, so any settlement
with wall level 1 or 2 had an empty visual cache → crash.

---

## 6 — When to spawn subagents

**Do spawn** when:
- Grepping / analysing a huge file (xscene, large XML) that would flood your context
- Making targeted edits to a large file you don't want inline (`general-purpose` agent with write access)
- A task is fully self-contained with a clear deliverable (e.g. "fix missing entities in scene.xscene")

**Do not spawn** for:
- Normal code edits to small/medium files
- Reading a few files to understand context
- Anything that needs the current conversation's debugging state

When briefing a subagent on a scene task, always include:
- Exact file paths
- A grep sample showing the XML structure
- A clear success criterion
- The reminder that xscene files are huge — grep only, no wholesale reads
