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

---

## Dialog detection

JetBrains IDE dialogs (e.g. "Breakpoint Condition Error", "Safe Mode?") are **not
detectable or dismissable via MCP**. If such a dialog blocks the game:

- The user must click the button manually (or AssertAutoIgnore handles Safe Mode).
- If "No" is clicked on a condition error: the breakpoint is skipped for that hit;
  the game continues. Kill and restart if you need to catch the next occurrence.
- **Fallback:** kill the Bannerlord process and relaunch.

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
