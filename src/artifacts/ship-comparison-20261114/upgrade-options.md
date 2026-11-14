# DADG ship upgrade options

Suggestions only, 14 November 2026. No upgrades, stats or slot layouts changed. Stat-only upgrades are fully valid; visual variants are optional. The user specifically queried this distinction. Start with stat-only improvements to sails/rigging, caulking and internal reinforcement if they provide the desired gameplay choices. Add visual art only when wanted or when an installed object should physically appear.

Use a small set of meaningful choices. Keep the English boat simple; let the kogge choose between cargo and fighting equipment; give the carrack the widest selection.

| Slot / package | English boat | Kogge | Carrack | Implementation approach |
|---|---|---|---|---|
| Sails and rigging | Basic / improved | Basic / improved / best | Basic / improved / best | Existing native bonuses work without visual changes. Optional later art can use `sail` upgrade anchors, retaining the custom sail shape, rig, cloth settings and furl animation. |
| Hull reinforcement | One modest option | Two levels | Two levels | Custom-named repair/reinforcement pieces using native hull-HP, weight and seaworthiness bonuses; fit visible reinforcement to each hull. Treat weight changes as physics changes requiring waterline/stability checks. |
| Deck outfit | Small stowage option | Cargo **or** combat outfit | Cargo **or** combat outfit | Use one deck slot to make the alternatives mutually exclusive: cargo capacity versus boarding-weapon / ammunition bonuses. Fit containers and stands away from navigation, helm, stairs and boarding paths. |
| Castle protection | None | One level | Two levels | Rail protection or fitted defensive panels on raised decks. Visual protection must have appropriate collision/destruction; decorative meshes alone do not prove projectile protection. |
| Gun installation | None | Existing 3-gun battery | Existing 5-gun battery | Keep one battery upgrade entry if the guns are bought together. Name, description, price and campaign representation must describe the battery, and extra guns need consistent ammo/perk/control handling. |

Recommended order: stat-only sail/rigging choices first; reinforcement or cargo/combat choices second; accurate battery presentation third. Visible deck props and castle protection can follow when desired, after clearance checks. Any weight bonus needs stability verification. Do not add oar slots unless the ships actually gain functioning oars. Figureheads can remain optional until there is a design reason and a fitted mesh.

## Existing support in the installed game

The installed `ShipUpgradePiece` type already reads bonuses for hull HP, sail HP, weight, seaworthiness, inventory, combat factor, melee damage, quivers, throwing ammunition, sail force, sail rotation, furl/unfurl speed and rudder force/surface area. Most proposed stat effects therefore fit existing XML rather than new gameplay code.

Current native sail pieces demonstrate a restrained progression:

- `sails_lvl2`: +20% sail rotation, +20% furl/unfurl speed, +4% campaign speed; port level 2.
- `sails_lvl3`: +30% sail rotation, +30% furl/unfurl speed, +8% campaign speed; port level 3.

Those are references, not proposed final DADG balance. The unusually high base sail HP should be settled separately before adding health bonuses.

Native deck options already distinguish cargo/weather cover, boarding weapons and ammunition supplies. Native hull pieces demonstrate the use of extra weight alongside survivability benefits. Use DADG names and art appropriate to the intended ships instead of copying every native material/appearance option.

## Wiring and acceptance criteria

Each hull slot must point to a registered slot object. **Only upgrades with visual alternatives need matching prefab anchors:** the hull `tag_id` matches that anchor, which also carries `upgrade_slot`. Each piece's `slot_prefab_child_id` selects the intended visible child; a `base` child preserves the unequipped appearance. When visual variants are implemented, keep every supported level present, including every carrack sail. Do not wrap the whole sail controller in a slot that disappears when unequipped.

Add port preview camera targets so the selected upgrade can be inspected. Test unequipped, each equipped variant, port preview, AI purchase progression, save/reload and mission spawn. Verify only the intended alternative remains active, and that hull collision, SDF, rudder and burning systems survive upgrade cleanup.

The [official ship creation guide](https://moddocs.bannerlord.com/war-sails/ship_creation/) documents visual upgrade holders, the port preview camera, and the need to fit deck upgrade props around navigation. Native source evidence is in installed NavalDLC `ModuleData/ship_upgrade_pieces.xml` and `ship_slots.xml`, and the locally decompiled 1.5.7 `ShipUpgradePiece.cs`.

## Plain-language explanations

**Buoyancy:** water pushes the hull upward. The multiplier scales that lift, while ship weight pulls downward. DADG currently declares 0.50 on its mission object, but the current campaign-battle code reads the hull object instead. Since DADG omits the hull value, it uses 0.451. This is a configuration mismatch; actual draft and stability must be measured before deciding the correct final value. No buoyancy settings were changed by this request.

**Boarding bottleneck:** soldiers can cross only where a connection has been made between the ships. DADG has one launch/receiving setup per side, concentrating traffic. Additional well-spaced eligible connection points can allow more ways to connect, but their count does not guarantee simultaneous bridges or good AI movement: deck width, navmesh, hull alignment and the receiving ship matter. Native serialized prefab counts are a clue about coverage, not a measured soldier throughput. No boarding changes were requested or made.
