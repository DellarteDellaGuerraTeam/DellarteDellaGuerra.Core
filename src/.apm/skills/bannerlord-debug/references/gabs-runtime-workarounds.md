# GABS / Runtime Playtest Workarounds

Hard-won failure modes from live DADG playtest sessions, with the recovery that actually works.
Each entry is a trap that has cost at least one abandoned session. Verified 2026-08-11 on
Bannerlord **1.4.7 (War Sails)**.

---

## 0 — Using GABS: the tool surface and how to drive it

Setup (bridge.json, ports, launching) lives in `dadg-gabs-protocol.md` and the
`bannerlord-gabs-start` skill. This section is about **using** the bridge once it is up.

### Discovery — never dump the whole surface

There are ~101 game tools and the set varies by GABS build. Discover, don't guess:

```
games_tool_names  gameId="bannerlord" prefix="bannerlord.ui" brief=true   # cheap listing
games_tool_detail gameId="bannerlord" tool="bannerlord.ui.click_widget"   # exact schema
games_call_tool   gameId="bannerlord" tool="bannerlord.core.get_game_state" timeout=30
```

`games_tool_names` also takes `query=` for a substring match over names. Prefer it over
`games_tools`, which returns full schemas and floods context. **Read the schema before calling** —
argument names are not guessable (see below).

If `games_tool_names` returns `availableTotal: 0`, or a call reports *"Tool ... not found for
game"*, GABP is **disconnected** — the game is down or the bridge dropped. That is not a bad tool
name. Re-run `games_status` / `games_connect`.

### Argument shapes that are easy to get wrong

| Tool | Correct arguments |
|------|-------------------|
| `ui/click_widget` | `widgetId` — the button's Id **or its visible text**. Not `text`. Use `"__index:N"` to click the Nth button. Disabled buttons are blocked by default. |
| `ui/answer_inquiry` | `affirmative` (bool, required); `selectedIndices` as a comma-separated string for multi-select/incidents, or the text for a text-input inquiry. |
| `games_call_tool` | `tool` should be the fully qualified mirrored name (`bannerlord.core.ping`). Raise `timeout` for load-ready flows and screen waits; the default is 30 s. |

### Reading game state

```
bannerlord.core.ping           # liveness: {"message":"pong", ...}
bannerlord.core.get_game_state # {campaignTime, currentMenu, missionName, state}
bannerlord.core.check_blockers # {blockerCount, blockers[], clear, state}
```

`state` values seen in practice: `InitialState` / `main_menu`, `campaign_map`, `mission`.
`check_blockers` returns entries like `paused`, `inquiry_active`, `mission_active:<sceneName>`,
`menu_siege_strategies`. A bare `paused` after a load is normal.

**Poll; never conclude from one sample.** Many transitions take 10–15 s with nothing on screen.

### Screenshots are the ground truth

`ui/take_screenshot` returns a `filePath` under
`C:\Users\<you>\Documents\Mount and Blade II Bannerlord\Screenshots\GABS\`; open it with the `Read`
tool. When a UI tool and the screen disagree, the screenshot wins — that is exactly how the global
layer gap in §1 was found. Screenshot before concluding a scenario passed, and keep the paths in
your evidence trail.

### There are no raw input tools

Searching the surface for `input` or `key` returns nothing: GABS cannot send arbitrary keystrokes
or mouse events. Everything goes through the semantic tools — which is why §1's OS-level injection
is the only escape hatch when those tools cannot see a widget.

### Driving the campaign

Dev-console commands (`campaign.*`, `config.cheat_mode 1`) go through
`bannerlord.core.run_command`. Always read its return value — a rejected command reports the valid
ids rather than failing the call, so a "successful" call can still have done nothing.
Menu navigation is `bannerlord.menu.get_current` /
`bannerlord.menu.select_option`; screen transitions are `ui/wait_for_screen`.

Verified tool families this session: `bannerlord.core.*` (ping, get_game_state, check_blockers,
save_game, load_save, set_time_speed, get_campaign_time), `bannerlord.ui.*` (9 tools — the three
UI-reading ones carry the §1 caveat), `bannerlord.mission.*` (leave, list_agents, talk_to_agent),
`bannerlord.menu.*`, `bannerlord.battle.get_state`, `bannerlord.settlement.get_settlement`,
`bannerlord.conversation.*` (see §10).

`battle.get_state.enemyTroopCount` is an excellent assertion target — exact troop counts let you
prove which parties were admitted to a side without touching the UI at all.

---

## 1 — GABS UI tools are blind to global Gauntlet layers

`ui/get_screen`, `ui/click_widget` and `ui/get_inquiry` enumerate only the layers of the **current
screen** (e.g. `MissionScreen`). Bannerlord inquiries are added as **global** layers, so a dialog
that is plainly visible on screen is invisible to all three: `get_inquiry` returns `type:"none"`
and `click_widget` fails, listing only the underlying screen's buttons.

`check_blockers` reads a different source and reports it correctly.

**Rule: when `check_blockers` says `inquiry_active` but `get_inquiry` says `none`, believe
`check_blockers`.** A real dialog is sitting there unreachable. This is not a false positive, not
an "orphaned" or "zombie" dialog, and not a reason to abandon the session — two playtest runs died
concluding otherwise.

`click_widget` works normally for genuine in-screen UI (confirmed on a deployment "Ready" button),
so the gap is specifically global layers.

### Recovery — OS-level click injection

Screenshot the dialog, read the button's pixel coordinates off the image, then inject a real click.
Captures are 1920x1080 while the client rect is 1920x1061, so the scaling below is not optional.

```powershell
$sig = @'
using System;
using System.Runtime.InteropServices;
public class W2 {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
'@
Add-Type -TypeDefinition $sig
$p = Get-Process -Name "Bannerlord" | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$h = $p.MainWindowHandle
$r = New-Object W2+RECT; [void][W2]::GetClientRect($h, [ref]$r)
$pt = New-Object W2+POINT; $pt.X = 0; $pt.Y = 0; [void][W2]::ClientToScreen($h, [ref]$pt)
# IMGX/IMGY = button centre in the 1920x1080 screenshot
$sx = $pt.X + [int]([math]::Round(IMGX * $r.R / 1920.0))
$sy = $pt.Y + [int]([math]::Round(IMGY * $r.B / 1080.0))
[void][W2]::SetForegroundWindow($h); Start-Sleep -Milliseconds 600
[void][W2]::SetCursorPos($sx, $sy); Start-Sleep -Milliseconds 400
[W2]::mouse_event(0x0002,0,0,0,[IntPtr]::Zero); Start-Sleep -Milliseconds 80
[W2]::mouse_event(0x0004,0,0,0,[IntPtr]::Zero)
```

Confirm with `get_game_state` afterwards. Proven against a mission retreat confirmation
("Are you sure to retreat your party?") and to back out of a BarterScreen.

---

## 2 — A GABP timeout usually means the debugger is PAUSED, not that the game crashed

Exception breakpoints trap benign first-chance exceptions constantly. The symptom is GABP calls
timing out while CPU flatlines. Always call `get_debug_session_status` first; if `state ==
"paused"`, `resume_execution` and retry. Expect to do this repeatedly — it is normal, not a fault.
Only treat it as a crash once the session or process is confirmed gone.

---

## 3 — Never step in a playtest session

**Do not call `step_over`, `step_into`, or `step_out`.** Stepping has poisoned sessions into a
stuck single-step loop that freezes the game unrecoverably. Use `evaluate_expression` and
breakpoints only.

Related: an async `pause_execution` frequently lands the main thread outside managed code, where
evaluation fails with *"The thread is not at a GC-safe point"* or *"The name 'TaleWorlds' does not
exist in the current context"*. To evaluate reliably, pause at a **breakpoint in managed code**
rather than pausing arbitrarily.

---

## 4 — Never "self-repair" GABS state files

Do not delete, move, rename or rewrite `~/.gabs/bannerlord/bridge.json`, `runtime.json`, or any
other GABS state file as a recovery step when tools look unreachable. This has destroyed live
session state. Use the `bannerlord-gabs-session-reset` skill, which does it correctly, or ask.

---

## 5 — `project_path` must use forward slashes

Backslashes produce a misleading `project_not_found` from the JetBrains tools:

```
D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src
```

---

## 6 — `bannerlord.core.new_game` fails on DADG

It looks for a `SandBox` menu option, which DADG replaces. Available options are
`CampaignResumeGame`, `ContinueCampaign`, **`DADGNewGame`** (Start Dell'arte della Guerra),
`CustomBattle`, `Options`, `Credits`, `Exit`.

Use `bannerlord.menu.select_option` with `DADGNewGame`, then drive character creation with
`ui/get_screen`, `ui/click_widget` and `ui/wait_for_screen`. Poll `get_game_state` until
`state == "campaign_map"`. It is a slow multi-screen grind.

---

## 7 — Campaign-init NRE from a stale distance cache

Loading a campaign dies with an **unhandled** `NullReferenceException` in
`DefaultMapDistanceModel.GetDistance` ← `Campaign.CalculateAverageDistanceBetweenTowns` ←
`DoLoadingForGameType`. It is a hard process death, not a caught exception.

Cause: `DellarteDellaGuerraMap/ModuleData/DistanceCaches/settlements_distance_cache_Default.bin`
does not match the map's settlement set. This happens when a save was made against a different map
branch (e.g. `develop`) than the one loaded (e.g. `1.4/complete-navmesh`, which adds Scotland).
Reading the mismatched cache throws, the rebuild fallback cannot write because the file is locked,
and the resulting cache holds a default `NavigationCacheElement<Settlement>` whose `Settlement` is
null. Vanilla's line-76 overload has no `_navigationCache` null-check (the sibling overload at
line 46 does), so it dereferences null and the process dies.

**Deleting the file is NOT sufficient** — the in-memory rebuild produces the same null. Restore the
committed git-LFS version:

```powershell
git checkout -- ModuleData/DistanceCaches/settlements_distance_cache_Default.bin
```

Only valid when `ModuleData/settlements.xml` is unmodified — check that first. A healthy cache is
~1.9 MB of real binary; a 4 MB one is suspect. The real fix is to use a save matching the loaded
map branch.

---

## 8 — Saving fails: "Save Failed! Cannot create save data."

Signature in `C:\ProgramData\Mount and Blade II Bannerlord\logs\rgl_log_*.txt`:

```
Could not find type definition of type: System.Collections.Concurrent.ConcurrentDictionary...MBObjectExtensionDataStore+DataKey
```

**Resolved 2026-08-11 by upgrading ButterLib.** ButterLib v2.10.4 saved fine on game 1.3.15 with an
identical module set but fails on 1.4.7 — it was built against the older save-serializer contract,
not a DADG defect and not a missing definer DADG should register.

If this signature reappears after a game update, suspect a module built against the previous
version's save contract before suspecting DADG code. While it is broken, no fixture save can be
created and every session must redo character creation — treat a live campaign as precious and stay
off player-controlled missions that a scenario does not strictly require.

---

## 9 — Console / cheat gotchas

- **`campaign.force_besiege` does not reliably create a SiegeEvent.** It only issues a movement
  order; a party can camp at the gate for days taking silent attrition while `isUnderSiege` stays
  false. Use **`campaign.create_siege`** when the siege fixture is the point and pathing is not
  under test.
- **Do not run `campaign.force_battle` during a siege test** — it pulls parties off the siege.
- Long unattended runs can hit an unrelated *vanilla* come-of-age equipment crash around in-game
  day 39. Use `complete_siege_prep` to skip construction waits rather than running time forward.

---

## 10 — Other stuck-UI cases

- **`conversation.get_state` / `conversation.continue` fail with `invalid_union` schema errors**
  once a conversation transitions into a non-dialogue screen such as a real BarterScreen. Recover
  with a screenshot plus the click injection in §1.
- **Stuck `PartyScreen`** freezes campaign time with no inquiry or menu signal, so `check_blockers`
  reports clear. `PartyScreenLogic.IsDoneActive()` / `IsCancelActive()` silently gate an
  apparently-enabled button. Click `OtherPartyPrisonersTransferButton` before `ConfirmButton`.
- After a battle, `missionEnded:true` → actual `campaign_map` state can take 10–15 s with no
  visible overlay. Poll before concluding anything is wrong.
