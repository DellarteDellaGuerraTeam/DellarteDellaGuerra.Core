# Oarless carrack playtest, 23 November 2026

The AI carrack made headway under automatic sail, tacked to a fixed upwind destination, and sailed around a skirmish target. It overshot the upwind destination after reaching it.

| Order or condition | In-game observation |
| --- | --- |
| Crosswind, automatic sail | In 13 m/s wind, the AI carrack accelerated from 0.62 to 7.36 m/s and sailed 124 m in 30 mission seconds. In 14 of 16 samples, the native controller requested furled sails and the ship received full sail. All five sails deployed. |
| Move directly upwind | The allied AI carrack approached within 2.6 m of a destination 100 m directly upwind in 12 m/s wind. It first passed within 7 m after 177 mission seconds. During the 45 seconds after that first approach it ranged as far as 24.7 m from the destination and ended 15.0 m away. |
| Fleet make-sail order | With the naval order menu open and the allied cog selected, simulated Left Ctrl+M enabled the override on the cog alone. A second press returned it to automatic control. The unselected allied carrack stayed on automatic control for both presses. |
| Stop | All five sail settings reached zero. Over 14 seconds after the order, horizontal speed fell from 2.77 to 0.48 m/s. The hull coasted about 24 m. |
| Skirmish | Against an anchored opponent, the carrack sailed 349 m and swept through 252 degrees in 124 mission seconds. Ship-centre distance ranged from 36 to 109 m. The ships stayed unconnected throughout the samples. |

The first fixed-destination test exposed a turn-back toward a tack waypoint after the ship passed it to one side. The course planner now measures remaining distance along the tack and releases the waypoint when the wind allows a direct approach. Two domain tests cover these cases. All 11 naval domain tests passed, and the solution built in Release.

## Test conditions

These readings come from separate War Sails custom battles on `battle_terrain_opensea_northern` using DADG carrack and cog hulls, vanilla combatants and the worktree assemblies. The test driver registered the fleet-order behavior, issued orders and set the scene wind. The game ran the AI, rudder, sails and physics. The crosswind run exercised the original automatic-sail change. The fixed upwind, Stop and skirmish measurements used the revised tack logic.

For the upwind run the driver kept the move destination fixed, moved other ships clear to prevent grappling, and used the game's fast-forward control. The skirmish run used normal game speed with a stationary target. The readings describe AI-controlled ships. Player-helm handling, campaign loading and combat damage were outside these trials.

The [sampled measurements](oarless-ship-ai-20261123.csv) record horizontal speed, position, native and applied sail requests, and mean sail deployment approximately every two mission seconds. Elapsed time starts at the first sample of each trial. In the DLC input enum, `Raised` means furled. A sail setting of 1 means fully deployed.
