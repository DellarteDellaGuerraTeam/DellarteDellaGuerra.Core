# Private-War Manual Test Guide

A hands-on guide for playtesting the same-kingdom private-war feature directly (no automation),
primarily to witness the **siege assault transition** that automated long runs have failed to
capture (the game crashes ~in-game day 39 on an unrelated vanilla bug, and an unattended driver
agent gets killed during the long quiet waits of a maturing siege).

## 0. Setup

1. Launch the game and load the **`privatewartest`** save.
2. Open the dev console (BLSE console — **`Alt + ~`** or `~`). All commands below are typed there.
3. Enable cheats if needed: `config.cheat_mode 1`

## 1. DADG private-war console commands

All live under the `campaign.` namespace (registered in
`src/DellarteDellaGuerra/PrivateWars/Api/Cheats/PrivateWarDebugCommands.cs`). Clan ids look like
`clan_percy`; settlement ids like `dadg_Pontefract_castle`. On a bad id, each command prints the
valid ids to choose from.

| Command | Usage | Purpose |
|---|---|---|
| **declare_private_war** | `campaign.declare_private_war <attackerClanId> <defenderClanId> [goalSettlementId]` | Registers an active same-kingdom private war and starts the AI drive. Omitting the goal auto-picks the defender's first fortification. |
| **list_private_wars** | `campaign.list_private_wars` | Main monitor — `score`, `battle`, `status`, goal per active war. |
| capture_settlement | `campaign.capture_settlement <settlementId> <newOwnerClanId>` | Force-transfer a fief's owner (set up / repair the test). |
| capture_lord | `campaign.capture_lord <captiveClanId> <captorClanId>` | Create a same-kingdom captive (prisoner-retention test). |
| is_prisoner | `campaign.is_prisoner <heroStringId>` | Report if/by whom a hero is held. |
| force_peace | `campaign.force_peace <captorClanId>` | Fire the peace prisoner-sweep. |
| change_kingdom | `campaign.change_kingdom <clanId> <kingdomId\|none>` | Move a clan between kingdoms (auto-conclude test). |
| ~~force_battle~~ | `campaign.force_battle <a> <b>` | ⚠️ **Do NOT use during an assault test** — spawns a field battle that pulls parties off the siege and confounds the run. |

Both clans must share a kingdom (same MapFaction) or it is an ordinary war, not a private one;
`declare_private_war` prints a `WARNING` if they do not.

## 2. The one variable that makes the assault fire: pick a WEAK goal fief

Past runs never assaulted because a **lone besieger (~100 troops) will never assault a castle held
by 150+ garrison + 300 militia** — it camps forever. So before declaring:

- Click candidate castles/towns (or open them in the Encyclopedia) and read the **garrison count**.
  Pick a same-kingdom fief whose garrison is *below* the attacker lord's party size.
- Click the attacker clan leader's party to read its troop count for comparison.
- Castles/towns only — villages cannot be besieged.

## 3. Following the besieger ("follow party camera")

There is no auto-lock follow-cam for AI parties, but:

1. Open the **Encyclopedia** (`N`), find the **attacker clan's leader**, open their page.
2. Click the **Track** (pin) icon — a persistent banner marker appears on the campaign map.
3. Pan to the marker and keep it in view; click it to read the party's current order
   (e.g. *"Besieging X"*).
4. **Space** pauses; use the speed controls to fast-forward between checks.

## 4. Run it

```
campaign.declare_private_war clan_percy <weakDefenderClanId> <weakGoalFiefId>
campaign.list_private_wars        # confirm status=Active, note the goal
```

Track the attacker (step 3), un-pause, let time run, re-check `list_private_wars` periodically.

## 5. What confirms success

- The tracked attacker marches to the goal fief; its order reads **"Besieging …"**.
- The siege **matures into an assault / the fief changes hands** (AI-vs-AI sieges auto-resolve —
  capture of the goal *is* the assault gate firing).
- In `list_private_wars`, `battle`/`score` move and `status` ultimately flips toward **Concluded**
  only when `score` crosses ±100 — **not** when the goal is taken. Capture transfers possession but
  the war stays **Active** so the dispossessed side can reclaim.
- **On capture, the losing side's lords inside the goal become prisoners of the besieger** (mirrors a
  real assault). Verify with `campaign.is_prisoner <losingLordStringId>` right after the fief changes
  hands — it should report the besieging hero as captor, and the lord must **not** be left sitting
  idle inside the now-enemy fief.
- **Prisoners are held *during* the war, freed *when it ends* — not auto-released mid-war.** While the
  war is Active, `campaign.force_peace` (the engine's peace prisoner-sweep) must **not** free a
  private-war captive (`is_prisoner` still reports held). Escape attempts are never blocked, so a
  captive may still break out on its own over time. When the war concludes (score crosses ±100, or
  the principals split kingdoms via `campaign.change_kingdom`), every captive the war put behind bars
  on the opposing side is released — `is_prisoner <losingLordStringId>` should flip to "not a prisoner"
  immediately after the "Private war concluded" message.
- **Fatigue resets on every capture.** `score` should *not* jolt to ±87 when the goal falls; it lands
  near the objective value (≈ ±50 for goal control) and then climbs over ~50 in-game days toward the
  new holder. Retaking the goal wipes the previous holder's accumulated drift.
- For a hard confirmation, set a logpoint at `StartSettlementEncounterSiegePatch.cs:94`
  (the `IsAtWarOrPrivateWar` check, a Transpiler) and watch its hitCount go above 0.

If the besieger reaches the fief but **camps indefinitely**, the garrison was still too strong →
pick a weaker fief, or that is the signal to build the feud-army stack (design §5/§15 fallback:
`willGatherArmy` + `Kingdom.CreateArmy` for the attacker + a disband guard).

## 6. Pitfalls

- Don't run `force_battle` mid-siege.
- Long runs die ~**in-game day 39** on a *vanilla* come-of-age equipment crash (not DADG code). A
  weak fief should assault well before then; if you crash first, the fief was too strong and the
  siege dragged.
