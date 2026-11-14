# Gun battery upgrade proposal

Design proposal only; no gun definitions, slots, stats or counts changed.

Keep one complete battery as the weapon upgrade: at least three guns for the kogge and five for the carrack in every supported option. The base ship should also have an explicit standard-battery state if the minimum is intended to apply before buying upgrades. The current optional-slot cleanup can otherwise remove a battery when no piece is equipped.

| Battery option | Kogge / carrack guns | Role | Tradeoff |
|---|---|---|---|
| Standard falconet battery | 3 / 5 | Existing general-purpose armament | Baseline price and performance |
| Improved falconet battery | 3 / 5 | Better handling and accuracy, potentially faster reload | Higher purchase cost |
| Heavy battery | At least 3 / 5 | More hull/rigging damage and penetration | More mass, slower handling/reload; bigger models need clearance checks |

Use consecutive port levels 1, 2 and 3 where possible so the existing AI upgrade progression can advance through tiers. The first implementation can retain the current gun models and placements for standard/improved batteries. Heavy armament should remain within their measured footprint unless separately fitted and tested. Extra guns beyond the minimum require additional crew, deck space and boarding/stair clearance, rather than simply duplicating machines.

If several upgrades should stack, add a separate ammunition-supply slot: standard supply versus expanded supply, exchanging cargo space/weight for more ammunition. Leave battery type mutually exclusive. A mounting/durability option can follow if it adds a meaningful choice; it is not needed for the first version.

Price the entire battery using medium/heavy hull prices to account for three versus five guns. UI names/descriptions and campaign combat strength must describe a battery, even though the native API exposes one representative siege weapon.

Implementation requirement: discover every gun on the ship and apply the chosen battery definition and ammunition bonuses consistently. Existing ShipSwivelGunCrewMissionLogic already crews extra guns but does not make the native representative-weapon ammo path cover them all. Per-gun damage/reload/accuracy changes need explicit integration with the siege weapon/Cannons implementation; they are not arbitrary attributes that the existing ShipUpgradePiece XML automatically supports. Preserve native crew behavior and never reset ammunition on a recurring tick.

Visual changes are optional for stat improvements. If a later battery changes gun models, each selectable assembly must contain the complete minimum battery and preserve its pilot/AI/physics setup.
