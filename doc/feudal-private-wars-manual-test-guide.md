# Private-War Manual Test Guide

A hands-on guide for playtesting the same-kingdom private-war feature directly (no automation),
especially its supported **preparation-complete ownership transfer**. Same-kingdom AI goal sieges do
not create an assault `MapEvent`; DADG resolves the frozen goal after preparations complete.

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
| force_besiege | `campaign.force_besiege <besiegerClanId> <settlementId>` | Move/order an eligible leader to besiege the frozen goal; let the engine form the camp. |
| create_siege | `campaign.create_siege <besiegerClanId> <settlementId>` | Directly create the same-kingdom siege fixture when pathing is not under test. |
| complete_siege_prep | `campaign.complete_siege_prep <settlementId>` | Complete preparations; advance about one hour to exercise the synthetic transfer. |
| ~~force_battle~~ | `campaign.force_battle <a> <b>` | ⚠️ **Do not use during a siege test** — it pulls parties off the siege and confounds the run. |

Both clans must share a kingdom (same MapFaction) or it is an ordinary war, not a private one;
`declare_private_war` prints a `WARNING` if they do not.

## 2. Use the exact frozen goal and isolate the transfer when needed

Natural preparation speed depends on the besieger's construction power, so a small party can take
many days. Defender strength is not an assault gate in the supported synthetic path.

- Use the immutable goal printed by `campaign.list_private_wars`; castles/towns only.
- For a natural AI smoke test, observe formation, pathing, army attachment, and preparation progress.
- To isolate ownership transfer from construction time, establish the siege with `force_besiege` or
  `create_siege`, run `complete_siege_prep`, then advance about one in-game hour.

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

Track the attacker (step 3), un-pause, let time run, re-check `list_private_wars` periodically. For a
deterministic transfer check, run the fixture commands from §2 after the war is active.

## 5. What confirms success

- The tracked attacker marches to the goal fief; its order reads **"Besieging …"**.
- When preparations are complete, the next DADG hourly callback transfers the fief exactly once.
  No assault `MapEvent` is expected in this supported AI path.
- In `list_private_wars`, `battle`/`score` move and `status` ultimately flips toward **Concluded**
  only when `score` crosses ±100 — **not** when the goal is taken. Capture transfers possession but
  the war stays **Active** so the dispossessed side can reclaim.
- **On transfer, the losing side's lords inside the goal become prisoners of the besieger.** DADG
  supplies the post-siege consequence because no assault `MapEvent` ran. Verify with
  `campaign.is_prisoner <losingLordStringId>` right after the fief changes hands — it should report
  the besieging hero as captor, and the lord must **not** be left idle inside the now-enemy fief.
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
- After the first transfer, keep time running and follow the dispossessed defender side. It must be
  able to form/retain its own siege against the unchanged frozen goal and reclaim it through the same
  preparation-complete path. The first holder must not trigger a second transfer on the next hourly
  tick merely because its completed camp was observed again.
- At each transfer verify the old garrison disappears and exactly one replacement garrison exists for
  the new owner. There must be no stale-party assertion/crash. Losing lords identified before the
  transfer must be prisoners afterward even if owner-change callbacks altered the settlement party list.
- While preparations run, inspect the army: attached members remain attached, and the leader's order
  remains **Besieging**. If the public scored objective retains both, no siege-retention patch is needed.

If the besieger reaches the fief but **camps indefinitely**, first distinguish slow preparations from
retention failure. A leader still ordered to **Besiege** with attached members is retained correctly;
wait or use `complete_siege_prep` to isolate the transfer. A leader that changes order and is ejected
despite the dominant hold score is the runtime evidence required before considering a focused
retention fix.

## 6. Nameplate start/end smoke test

With the player's clan on one side of the private war:

1. Before declaration, confirm an uninvolved same-kingdom party/settlement keeps its normal green
   same-faction presentation and a real diplomatically allied settlement remains blue.
2. Declare the war while the rival party and goal settlement are visible. Without reopening the map,
   both rival nameplates must immediately switch to the configured private-enemy tint.
3. Inspect a distinct clan called onto the player's side. Its party/settlement uses the configured
   private-war same-side tint while retaining SameFaction nameplate behavior, not Alliance behavior.
4. Conclude the war. The visible nameplates must immediately return to their vanilla faction/relation
   colors without reopening the map or reloading the save.
5. Pan far enough to recycle party nameplates, then return. Colors must remain correct and each war
   start/end should produce one refresh, with no duplicate-subscription symptoms.

UIExtenderEx must report no failed `PartyNameplateItem` or `SettlementNameplateItem*` prefab
extensions during this run. This runtime check owns selector and subscription-lifecycle verification;
there is intentionally no source-shape unit test for those framework details.

## 7. Pitfalls

- Don't run `force_battle` mid-siege.
- Long runs die ~**in-game day 39** on a *vanilla* come-of-age equipment crash (not DADG code). A
  slow preparation can reach that unrelated failure first; use `complete_siege_prep` when the
  construction delay itself is not the subject of the test.
