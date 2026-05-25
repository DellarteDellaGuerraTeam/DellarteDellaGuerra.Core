# Case Study — SettlementVisual IndexOutOfRangeException

A textbook DADG debugging case. Root cause was a **data bug in the campaign map scene**,
not a C# bug — despite the crash appearing in engine code.

---

## Symptoms

- `IndexOutOfRangeException` at `SettlementVisual.cs:713` during campaign play
- Crash reproducible on any save with an active siege at a settlement with wall level < 3
- No crash at wall level 3

---

## Stack trace (simplified)

```
SettlementVisual.GetDefenderRangedEngineSpawnEntitiesForCurrentLevel()
  → _defenderRangedEngineSpawnEntitiesCacheForCurrentLevel[slotIndex]   ← crash here
  slotIndex = 0, cache.Length = 0
```

Key variables when paused:
- `slotIndex = 0`
- `_defenderRangedEngineSpawnEntitiesCacheForCurrentLevel.Length = 0`
- `wallLevel = 2` (settlement with breached/partially-built walls)

---

## Investigation path

- **[[bannerlord-debugging-workflow]]** — paused on CLR exception during campaign → did NOT
  resume; inspected variables and stack first
- **`mcp__jetbrains-debugger__get_variables`** — confirmed `slotIndex=0`, `cacheLength=0`,
  `wallLevel=2`; this ruled out "slot 3 overflow" hypothesis
- **`mcp__bannerlord-search-1_3_1__search_bannerlord_code`** — found cache population logic
  in `SettlementVisual`: filters `map_defensive_engine` entities by
  `(entity.GetUpgradeLevelMask() & _currentSettlementUpgradeLevelMask) == mask` where
  mask = `1 << (wallLevel - 1)` → mask `2` for level 2, `1` for level 1
- Grepped DADG map scene (`defender_ranged`) → only `defender_ranged_l3` entities found
  (96 total, zero `l1` / `l2`) — cache would always be empty for wall level 1 or 2
- Compared against vanilla (`SandBox/SceneObj/Main_map/scene.xscene`) — vanilla has all
  three levels

**See:** `.claude/skills/bannerlord-debug/SKILL.md` §5 (Investigating data bugs) and §6
(when to spawn subagents for huge scene files)

---

## Root cause

DADG's campaign map (`DellarteDellaGuerraMap/SceneObj/Main_map/scene.xscene`) was missing
`defender_ranged_l1` and `defender_ranged_l2` entities entirely. Only `l3` was present.

The `SettlementVisual` cache is populated at runtime from scene entities filtered by wall
level mask — so any settlement at wall level 1 or 2 produced an empty cache, and the
engine immediately accessed slot 0 → `IndexOutOfRangeException`.

---

## Fix

- **Scene fix (only change needed):** subagent added 380 `defender_ranged_l1` + 380
  `defender_ranged_l2` entities to the DADG main map, each copied from its `l3` counterpart
  with the `<level name="level_1"/>` / `<level name="level_2"/>` block set accordingly
- Verified with no C# changes — the scene fix alone eliminated the crash

---

## Lessons

- **Cache empty ≠ C# bug** — `slotIndex=0, length=0` instantly pointed to the data layer,
  not code logic; always inspect variables before assuming code is wrong
- **Always check DADG scenes against vanilla** — DADG overrides the map scene entirely;
  any entity type vanilla uses that DADG omits will silently produce empty caches or
  missing references at runtime
- **Spawn a subagent for xscene edits** — scene files are 20+ MB; a subagent keeps the
  main conversation context clean (see `.claude/skills/bannerlord-debug/SKILL.md` §6)
- **The CLR exception window is everything** — resuming immediately would have lost
  `slotIndex=0` and `cacheLength=0`; those two variables contained the entire diagnosis
