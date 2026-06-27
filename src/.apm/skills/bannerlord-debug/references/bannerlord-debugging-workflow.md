# Bannerlord Debugging Workflow

How to handle the Rider debugger and GABS during a live Bannerlord session.

## CLR Exception Breakpoints

Rider is configured with CLR exception breakpoints that fire on first-chance exceptions —
exceptions the game does **not** handle, which will cause a crash on resume.

### Loading phase pauses

During game startup and save loading, a small number of CLR exception breakpoints fire on
exceptions that **are** handled by the engine (e.g. `MBObjectManager` XML parsing at
`MBObjectManager.cs:993`). These are safe to resume without investigation.

### Campaign gameplay pauses

If the debugger pauses unexpectedly **during campaign play** and no user-set breakpoint was
the cause, treat it as a crash signal — 95% of the time it is a first-chance exception
that **will** crash Bannerlord if resumed.

**Do not resume immediately. Investigate first:**

1. Read the stack trace and current location
2. Inspect variables in the current and parent frames
3. Evaluate expressions to probe game state
4. Search Bannerlord source (`mcp__bannerlord-search-*`) and mod code for context
5. Reason about the root cause

Only resume when:
- You have a diagnosis and intend to write a fix, **or**
- You are deliberately discarding the session to relaunch

This is the same workflow a developer would follow sitting at their debugger.

## JetBrains Debug Bridge (port 7777)

The `JetBrainsDebuggerPauseMonitor` global tool proxies `get_debug_session_status` and
exposes `GET /state?path=<projectPath>` → `{"isPaused": bool, "statusText": "..."}`.

- `isPaused: true` → the session state field is `"paused"` or `"suspended"`
- The pre-tool hook blocks GABS game commands when `isPaused: true` to prevent sending
  commands into a paused game

The MCP endpoint for direct calls: `http://127.0.0.1:29202/debugger-mcp/streamable-http`
(requires MCP session init before tool calls — see `Mcp-Session-Id` header protocol).
