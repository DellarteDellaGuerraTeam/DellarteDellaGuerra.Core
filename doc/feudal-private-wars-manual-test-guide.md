# Private-War Manual Test Guide

A hands-on guide for playtesting the same-kingdom private-war feature directly (no automation),
especially its **siege assaults**. A same-kingdom AI goal siege ends in a real wall-assault `MapEvent`
with casualties, which the player can join; the fief changes hands only if the assault wins
(`feudal-private-wars-design.md` §19).

## 0. Setup

1. Launch the game and load the **`privatewartest`** save.
2. Open the dev console (BLSE console — **`Alt + ~`** or `~`). All commands below are typed there.
3. Enable cheats if needed: `config.cheat_mode 1`

## 1. DADG private-war console commands

All live under the `campaign.` namespace (registered in
`src/submodules/PrivateWars/Bannerlord.PrivateWars/Cheats/PrivateWarDebugCommands.cs`). Clan ids look like
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
| complete_siege_prep | `campaign.complete_siege_prep <settlementId>` | Complete preparations; advance a few hours for the AI to assault (only if it judges the assault winnable). |
| ~~force_battle~~ | `campaign.force_battle <a> <b>` | ⚠️ **Do not use during a siege test** — it pulls parties off the siege and confounds the run. |

Both clans must share a kingdom (same MapFaction) or it is an ordinary war, not a private one;
`declare_private_war` prints a `WARNING` if they do not.

## 2. Use the exact frozen goal and isolate the assault when needed

Natural preparation speed depends on the besieger's construction power, so a small party can take
many days. Defender strength **is** an assault gate: the AI assaults only once preparations are
complete and it out-strengths the defenders, so pick a weak goal or a strong besieger.

- Use the immutable goal printed by `campaign.list_private_wars`; castles/towns only.
- For a natural AI smoke test, observe formation, pathing, army attachment, and preparation progress.
- To skip construction time, establish the siege with `force_besiege` or `create_siege`, run
  `complete_siege_prep`, then advance a few in-game hours.

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
- When preparations are complete and the besieger is strong enough, it **assaults the walls**: a real
  siege battle with casualties on both sides. Nothing transfers without a won assault.
- **The player can join.** Ride up to the besieged fief: the `join_siege_event` menu offers joining the
  besiegers, breaking in to help the defenders, or attacking the siege camp, depending on which side is
  the player clan's private-war enemy. During an assault, joining the side opposite your enemy works.
- **A won assault gives the fief to the interim owner**, not the king: the capturing clan, or the
  claimant when they march in the capturing army without leading it. The title does not move while the
  war is live, and **no kingdom claimant vote** appears afterwards.
- In `list_private_wars`, `battle`/`score` move and `status` ultimately flips toward **Concluded**
  only when `score` crosses ±100 — **not** when the goal is taken. Capture transfers possession but
  the war stays **Active** so the dispossessed side can reclaim.
- **Prisoners come from the assault itself.** Defeated lords who fought are taken by vanilla
  post-battle rules (`campaign.is_prisoner <lordStringId>`). A lord who sat out the assault inside the
  town is no longer captured automatically.
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
  able to form/retain its own siege against the unchanged frozen goal and reclaim it by assault.
- At each transfer verify the old garrison disappears and exactly one replacement garrison exists for
  the new owner. There must be no stale-party assertion/crash.
- Neutral lords of the same kingdom must not join either side of the assault.
- While preparations run, inspect the army: attached members remain attached, and the leader's order
  remains **Besieging**. If the public scored objective retains both, no siege-retention patch is needed.

If the besieger reaches the fief but **camps indefinitely**, first distinguish slow preparations from
retention failure, and both from a besieger too weak to assault. A leader still ordered to **Besiege**
with attached members is retained correctly; wait, strengthen it, or use `complete_siege_prep`. A leader that changes order and is ejected
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
