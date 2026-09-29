# JetBrains Rider Debugger — Tips and Gotchas

## Conditional breakpoints

### `&&` is not a valid format specifier

**Symptom:** Setting a conditional breakpoint with `&&` (logical AND) produces:

> Breakpoint Condition Error  
> Failed to evaluate conditional breakpoint expression.  
> Error: ';&' is not a valid format specifier.  
> Would you like to stop at the breakpoint?

**Cause:** JetBrains/Rider treats `&` as a special character (format specifier) in breakpoint
condition strings when the expression is transmitted through certain paths (e.g. the MCP
JetBrains debugger bridge). The `&&` operator gets mis-parsed before it reaches the
C# evaluator.

**Fix — use `?.` null-conditional instead of null-guard `&&`:**

```csharp
// ❌ breaks
mergedXmlForManaged != null && mergedXmlForManaged.DocumentElement != null && mergedXmlForManaged.DocumentElement.Name == "MusicTracks"

// ✅ works
mergedXmlForManaged?.DocumentElement?.Name == "MusicTracks"
```

**General rule:** avoid `&&` / `||` in breakpoint conditions set via MCP. Use:
- `?.` and `??` null-conditional / null-coalescing operators
- Single boolean conditions with no logical connectives
- Method calls that encapsulate the logic (`string.Equals(...)`, `.Contains(...)`, etc.)

### `<`, `>` and `&` are XML-escaped — and the condition dies *silently*

**Symptom:** the breakpoint fires on **every** hit, with no error dialog, as if you had set no
condition at all. You then read the data of whatever object happened to arrive first and
attribute it to the one you were filtering for.

**Cause:** the condition travels through an XML layer on its way to Rider. Any `<`, `>` or `&`
comes back out as `&lt;`, `&gt;`, `&amp;`. Read the `condition` field echoed in the
`set_breakpoint` / `wait_for_pause` response — if it shows `&amp;&amp;` or `&lt;`, the condition
is already corrupt. A corrupt condition is not always reported as an error; it is often just
dropped, and `hitCount` stays `0` while the breakpoint keeps pausing.

```csharp
// ❌ silently dead — comparison operators and && are escaped
_weapon.Side == BattleSideEnum.Defender && _weapon.GameEntity.GlobalPosition.Distance(Agent.Main.Position) < 25

// ❌ still dead — the ternary removes `&&` but `<` is escaped too
_weapon.Side == BattleSideEnum.Defender ? dist < 25f : false

// ✅ equality only, no <, >, &, or generics
_weapon.Id.Id == 2408
```

**Rule: breakpoint conditions must contain only `==` / `!=` equality on a scalar.** Anything
requiring a comparison, a conjunction, or a generic type argument (`OfType<T>` — the `<>`
break too) belongs in `evaluate_expression` at the pause, not in the condition. Pin the
object you want by identity first (evaluate a query to get its `Id`), then condition on that id.

**Verify before trusting a pause:** at the first hit, evaluate the identity you were filtering
for (e.g. `_weapon.Id.Id`) and confirm it matches. Cost: one call. It caught a wrong-cannon
mix-up on 2026-08-15 where every reading came from a cannon 107 m from the intended one.

---

## Multi-statement `evaluate_expression` needs a `Func<T>` IIFE

`evaluate_expression` accepts several statements, but a trailing `return` yields `"value": "void"` —
the result never comes back. Wrap the body in an immediately-invoked lambda instead:

```csharp
((System.Func<string>)(() => {
  var cn = /* ... */;
  int n = 0;
  foreach (var a in items) { if (cn.SomeGate(a)) n++; }
  return "n=" + n;
}))()
```

Unlike breakpoint conditions, expressions are **not** escaped, so `&&`, `<` and `OfType<T>` are all
fine here. Two more gotchas: re-declaring a local from a previous evaluate fails with
`Synthetic with name "x" is already added` (rename it), and namespace-qualify mod types
(`Bannerlord.Cannons.BattleMechanics.Artillery.BaseFieldSiegeWeapon`) so the expression works from
any frame, not just one that already imports them.

---

## Dialog detection

JetBrains IDE dialogs (e.g. "Breakpoint Condition Error", "Safe Mode?") are **not
detectable or dismissable via MCP**. If such a dialog blocks the game:

- The user must click the button manually (or AssertAutoIgnore handles Safe Mode).
- If "No" is clicked on a condition error: the breakpoint is skipped for that hit;
  the game continues. Kill and restart if you need to catch the next occurrence.
- **Fallback:** kill the Bannerlord process and relaunch.

---

## A paused breakpoint is indistinguishable from a soft-lock

**Symptom:** the game looks hung — static screen, campaign time frozen, no menu ever binds,
`bannerlord.*` GABS calls time out, and breakpoints you just set on other classes never fire.

**Cause:** the process is not hung, it is *paused at a breakpoint* — often a stale one left
enabled by an earlier run. Breakpoints persist in Rider's workspace across game relaunches and
across Claude sessions, so a breakpoint someone set hours ago is still armed on the next launch.

**Workaround — check before you diagnose:**

```
mcp__jetbrains-debugger__get_debug_session_status   # state: "paused" vs "running"
```

If `state` is `paused`, call `resume_execution` and carry on. Only call it a freeze once you have
seen `running` while the symptoms persist.

**Prevention — audit breakpoints before any playtest run:**

```
mcp__jetbrains-debugger__list_breakpoints           # check enabledCount, not totalCount
```

Disabled breakpoints are harmless and accumulate by the hundred; only `enabled: true` line
breakpoints matter. Remove stale ones with `remove_breakpoint` before launching.

**Rules that follow from this:**

- **Every breakpoint you set is yours to remove.** Remove it *before* `resume_execution`, not after.
- Never leave a **per-frame** breakpoint (one in an `OnTick`/update path) enabled across a scenario
  boundary — it re-breaks on the very next frame and the game appears frozen again immediately.
- Exception breakpoints set to *uncaught only* are safe to leave on and are useful for catching a
  real CLR crash. Ones that break on *caught* exceptions will pause constantly during load — vanilla
  Bannerlord throws and swallows exceptions routinely.

This confound invalidated an entire save/load investigation on 2026-08-12: two "Critical" freeze
defects were filed against DADG church code, and the control run intended to disprove them started
while already paused at a leftover per-frame breakpoint.

---

## Getting an on-demand pause with the full campaign object graph

`evaluate_expression` and `set_variable` only work while **paused at a breakpoint**. To get a pause
whenever you want one, breakpoint DADG's own per-frame code rather than hunting for a call site:

```
<root>\src\DellarteDellaGuerra\DisplayCompilingShaders\CompilingShaderNotifier.cs   line 37
```

(`_tickCount += dt;`) It is DADG source, so it binds cleanly with no decompiled-path pain, and it
runs every frame on the main game thread with the whole campaign object graph in scope. Do **not**
use lines 43+ — they sit behind an early `return` and fire only intermittently.

Loop: `set_breakpoint` → `wait_for_pause` → all your `evaluate_expression` / `set_variable` calls at
that one pause → **`remove_breakpoint` FIRST** → `resume_execution`. Skipping the removal is exactly
how the soft-lock confound above gets created. While paused, the game is frozen and GABS
`bannerlord.*` calls will not respond — that is expected, not a failure.

---

## Decompiled source breakpoints

Rider decompiles Bannerlord DLLs on demand and caches them at:

```
%APPDATA%\JetBrains\Rider<version>\resharper-host\DecompilerCache\decompiler\<hash>\<hash>\<ClassName>.cs
```

Breakpoints set on decompiled files work the same as source breakpoints. The path
is stable within a Rider version but changes if Rider is updated or the DLL changes.

When setting breakpoints on decompiled Bannerlord code via MCP, always use the full
cached path (check the `file` field in `get_debug_session_status` after a pause at
that location).

---

## MBObjectManager CLR exception on save load

`MBObjectManager.LoadXml` at line 993:
```csharp
string objectName = node.Attributes["id"].Value;  // NullReferenceException if no id attr
```

- Fires during **save load** (campaign re-initialisation), **not** during module init.
- Exception is **swallowed** by the catch block at line 424 — the game continues.
- `typeName` (in scope at line 993) tells you the object type being loaded.
- `node.OuterXml` (evaluate expression) shows the offending XML element.
- To find which file the element comes from: set a breakpoint at line 421
  (`this.LoadXml(mergedXmlForManaged, isDevelopment)`) with condition
  `mergedXmlForManaged?.DocumentElement?.Name == "MusicTracks"` (or whatever
  the root element name is for the failing type) and inspect `id` when it pauses.
