---
name: bannerlord-gabs-battle-manager
description: >
  Manage a Bannerlord battle or siege from start to finish -- deployment through
  to returning to the campaign map. Use this skill whenever the user wants to
  fight a battle, defend a settlement, lead an assault, handle a siege,
  or get through a mission. Trigger on phrases like "fight the battle",
  "defend [settlement]", "lead the assault", "handle the siege", "do the battle",
  "delegate battle to AI", "manage the battle for me", or any time the user is
  in or about to enter a combat mission. Covers field battles, siege assaults,
  and siege defenses.
---

# bannerlord-gabs-battle-manager

Drive a Bannerlord battle from the campaign map all the way back to the campaign map.

A battle has four phases: Pre-battle, Deployment, Battle, Post-battle.

---

## Phase 1: Pre-battle

Call `bannerlord.core.check_blockers`. Look for:

**menu_siege_strategies active:**
Call `bannerlord.menu.get_current` to read the options.
- "You are commanding the defenders" -> siege defense: select `menu_siege_strategies_lead_assault`
- Attacking -> usually also `menu_siege_strategies_lead_assault`

**paused only:** No action needed, continue.

**No blockers / already in mission:** Skip to Phase 2.

After selecting from the siege menu, wait for mission state:
  bannerlord.core.wait_for_state { "expectedState": "mission", "timeout": 60 }

---

## Phase 2: Deployment

Call `bannerlord.battle.get_state` -- confirm mode is "Deployment".
Note playerSide (Defender/Attacker) and power ratios for context.

Call `bannerlord.battle.get_formations` and summarize troop counts to the user.

Ask the user: "Delegate to AI or issue orders?" (default: delegate)

**If delegating:**
1. Call `bannerlord.battle.order_delegate_to_ai`
2. Call `bannerlord.ui.get_screen` -- find "Ready" button in MissionOrderOfBattle layer
3. Call `bannerlord.ui.click_widget` with widgetId "Ready"

**If issuing orders** (then still click Ready after):
- `bannerlord.battle.order_advance` -- move toward enemy
- `bannerlord.battle.order_charge` -- charge
- `bannerlord.battle.order_hold` -- hold position
- `bannerlord.battle.order_fallback` -- fall back
- `bannerlord.battle.order_follow_me` -- follow player
- `bannerlord.battle.set_fire_order` -- fire-at-will or hold
- `bannerlord.battle.order_retreat` -- retreat/flee from the battle (triggers withdrawal toward exit)
All accept optional formationIndex, but NOTE: in practice orders broadcast to ALL formations regardless of the index. Do not promise per-formation targeting.

---

## Phase 3: Battle in progress

Poll `bannerlord.battle.get_state` every 20-30 seconds.
Each poll, report to the user:
- Elapsed time
- playerTroopCount vs enemyTroopCount
- playerDeaths and enemyDeaths (casualties since battle start)
- playerRemainingPowerRatio vs enemyRemainingPowerRatio
- playerAgent.health if the player is alive

Stop polling when `missionEnded` is true.

**To exit a battle early (retreat/flee):**
- `bannerlord.battle.order_retreat` -- orders all troops to retreat toward the exit; triggers defeat/withdrawal
- `bannerlord.mission.leave` -- immediately leaves the mission (same penalty as retreating mid-battle)

**Known quirks observed in live battles:**
- `playerAgent.position` often appears frozen between polls -- not a bug, just low update cadence. Do not use it as a movement indicator.
- `enemyRemainingPowerRatio` can spike to extreme values (e.g. 300+) in the final poll when player power hits zero. This is a display artefact -- ignore it and read `battleResult` instead.
- If the user says "just monitor" or similar, do NOT issue orders. Only report state each poll.

---

## Phase 4: Post-battle

Report result from final `battle.get_state`:
- `battleResult.battleState` (AttackerVictory, DefenderVictory, etc.)
- `battleResult.playerVictory` (true/false)
- Final playerDeaths vs enemyDeaths

Exit: call `bannerlord.ui.click_widget` with widgetId "QuitButton".
Wait: `bannerlord.core.wait_for_state` with expectedState "campaign_map", timeout 60.

**If wait_for_state times out:** call `bannerlord.core.get_game_state` directly to check.
**If that also fails / tools return "not found":** call `games_connect` to re-sync. If bridge.json is missing or `games_status` shows "stopped", the game process has exited -- inform the user and offer to restart.

Summarize: victory/defeat, casualties, confirmed return to campaign map.

## Edge cases

- missionEnded but no QuitButton: try `bannerlord.mission.leave` as fallback, then `bannerlord.ui.click_widget { widgetId: "Retreat!" }` as a second fallback
- No "Ready" button in deployment: game may need manual input -- tell the user
- `battle.get_state` fails mid-battle: check `core.get_game_state` -- if it's already campaign_map, the battle resolved silently
- `formationIndex` does NOT isolate a single formation -- all orders broadcast to all formations regardless of the index passed. Accept this and issue orders accordingly.
- After defeat, the game process may stop entirely (crash or auto-close). Always check `games_status` if the GABS connection drops post-battle.
