# Sail durability adjustment — 2026-11-14

Changed only `max_sail_hitpoints` in Map `ModuleData/dadg_custom_ship_hulls.xml`:

| DADG hull | Hull HP (unchanged) | Previous sail HP | New base sail HP |
|---|---:|---:|---:|
| English boat B | 4,000 | 100,000 | 2,000 |
| Kogge | 16,000 | 100,000 | 8,000 |
| Carrack | 24,000 | 100,000 | 12,000 |

The installed War Sails ordinary fishing ship, trade cog and heavy roundship all use sail HP equal to half hull HP. Their native hull/sail values are 4,000/2,000, 16,000/8,000 and 42,000/21,000 respectively. The carrack retains its existing 24,000 hull HP, so the same ratio gives 12,000 rather than copying the larger roundship's 21,000 verbatim.

`adapt_sail_hp.py` verifies those installed native reference ratios, changes the three exact XML attribute values, reparses the result and asserts every other hull attribute and upgrade slot remains unchanged. No visual variant is required for this stat change. Normal native sail upgrade multipliers still apply.

Installed `TaleWorlds.CampaignSystem.Naval.Ship.MaxSailHitPoints` derives its maximum from the hull definition plus upgrade multipliers. The native current-health setter clamps to the maximum. This is a base-stat balance change, with no proactive rewrite of saved current-health fields. Existing-save loading and battle damage have not been runtime-tested.
