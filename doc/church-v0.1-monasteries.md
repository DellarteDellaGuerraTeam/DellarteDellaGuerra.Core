# Church v0.1 — Monasteries & Abbots

First slice of the DADG religious system. Scope: existing abbey/priory/cathedral villages get a
resident clergy notable (vanilla `Occupation.Preacher`), and the player can talk to them and make
a donation for relation and a little renown. Nothing else.

**Out of scope (later versions):** tithe/wealth economy (v0.2), Sunday mass (v0.2), sanctuary
(v0.3), church hierarchy characters and piety-like reputation (v0.4+). The hierarchy will be a
custom domain system, deliberately not built on Bannerlord clans.

## 1. Church settlements

Detection rule: the settlement id is listed in the dedicated config file
`config\dadg.church_settlements.xml` (`<ChurchSettlement id="..." kind="Abbey|Priory|Cathedral" />`),
loaded once at startup by `ChurchSettlementsXmlProvider`. The shipped file lists exactly the 16
villages below from `DellarteDellaGuerraMap\ModuleData\settlements.xml`; a missing or invalid file
logs a warning and leaves the list empty, so the church features simply stay inactive.

| Settlement id | Bound to | Clergy title |
|---|---|---|
| village_Malmesbury_Abbey | Bristol_town | Abbot |
| village_Tintern_Abbey | Bristol_town | Abbot |
| village_Hexham_Abbey | Newcastle_upon_Tyne_town | Abbot |
| village_Buckfast_Abbey | Launceston_town | Abbot |
| village_Evesham_Abbey | Coventry_town | Abbot |
| village_Whitland_Abbey | Pembroke_town | Abbot |
| village_Battle_Abbey | dadg_Rye_castle | Abbot |
| village_Byland_Abbey | dadg_Middleham_castle | Abbot |
| village_Walsingham_Abbey | dadg_Baconsthorpe_castle | Abbot |
| village_Rievaulx_Abbey | dadg_Helmsley_castle | Abbot |
| village_Lanercost_Priory | Carlisle_town | Prior |
| village_Lindisfarne_Priory | dadg_Bamburgh_castle | Prior |
| village_Finchale_Priory | dadg_Durham_castle | Prior |
| village_Ely_Cathedral | Bury_St_Edmunds_town | Bishop |
| village_Llandaff_Cathedral | Cardiff_town | Bishop |
| village_St_Asaph_Cathedral | dadg_Denbigh_castle | Bishop |

Title is derived from the `kind` attribute and only used in dialog text (v0.1 does not rename the
hero).

## 2. Abbot notable

- Spawn API (verified in 1.4.6): `HeroCreator.CreateNotable(Occupation.Preacher, settlement)`.
  `Hero.IsNotable` includes `IsPreacher` (verified), so the hero shows up in the village menu's
  notable list like a headman does.
- Spawn trigger: on session launched (covers new game and existing saves) and on daily tick,
  idempotently: for each church settlement, if `settlement.Notables` contains no living preacher,
  spawn one. The daily check doubles as respawn-on-death.
- Mirror `Helpers.SpawnNotablesIfNeeded` for placement (it pairs `CreateNotable` with settlement
  entry); confirm the hero lands in the settlement during implementation.
- Template source: the map module already ships preacher notable templates —
  `spc_notable_vlandia_2` (male + female, `occupation="Preacher"`, `Culture.empire`) in
  `DellarteDellaGuerraMap\ModuleData\characters\dadg_npccharacter_commoners.xml`.

## 3. Dialog (talk to the abbot)

Registered in the campaign behavior on `CampaignEvents.OnSessionLaunchedEvent` via
`starter.AddDialogLine` / `AddPlayerLine`, same pattern as `JoustTournamentCampaignBehavior`.
Entry condition: one-to-one conversation character has `Occupation.Preacher` (only our spawns
have it). Inline `TextObject("{=id}fallback")` localization, per existing convention.

Flow (tokens in parentheses):

1. **NPC greeting** (`start` → `dadg_church_talk`): "God keep you, my {?PLAYER.GENDER}lady{?}lord{\?}.
   What brings you to {MONASTERY_NAME}?"
2. **Player options** (from `dadg_church_talk`):
   - *"I wish to make a donation to the {abbey|priory|cathedral}. (500 denars)"* — shown only when
     the donation policy allows it (gold ≥ 500 and cooldown elapsed). → NPC thanks
     ("{TITLE} will remember your generosity…") → back to `dadg_church_talk`.
   - *"Bless me, Father."* — pure flavor; NPC replies with a short blessing → back to
     `dadg_church_talk`.
   - *"I must be on my way."* → `close_window`.

## 4. Donation rules (Domain)

Pure policy class, unit-tested, no Bannerlord references:

- Cost: **500 gold** (configurable — `DonationCost` in `<ChurchConfig>`, see the
  Configuration section of church-v0.2)
- Effects: **+2 relation** with the abbot (`ChangeRelationAction`; configurable
  `DonationRelation`), **+1 renown** (`GainRenownAction`, fixed) — applied by the behavior.
- Cooldown: **7 in-game days per abbot** (one Bannerlord week, fixed).
- Policy outcomes: `Allowed`, `InsufficientGold`, `OnCooldown`. Inputs are plain values
  (player gold, days since last donation) so no ports are needed for v0.1.

## 5. File layout (follows the Tournament feature)

- `src\DellarteDellaGuerra.Domain\Church\Donation\DonationPolicy.cs` — rules above.
- `src\DellarteDellaGuerra.Domain.Tests\DonationPolicyTests.cs` — xUnit, plain values.
- `src\DellarteDellaGuerra\Church\ChurchSettlements.cs` — DI-injected instance service
  (registered as a singleton in `RegisterChurchServices`): detection + title mapping, backed by
  the Domain port `IChurchSettlementsProvider`
  (`src\DellarteDellaGuerra.Infrastructure\Church\ChurchSettlementsXmlProvider.cs` reads
  `config\dadg.church_settlements.xml`).
- `src\DellarteDellaGuerra\Church\Api\Campaign\ChurchCampaignBehavior.cs` —
  `CampaignBehaviorBase`: spawn loop.
- `src\DellarteDellaGuerra\Church\Api\Campaign\AbbotDialogCampaignBehavior.cs` —
  dialog registration, applies donation effects.
  `SyncData`: `Dictionary<Hero, CampaignTime>` of last donation per abbot.
- `src\DellarteDellaGuerra.Integration\DI\DadgServiceContainer.cs` — `RegisterChurchServices`.
- `src\DellarteDellaGuerra.Integration\SubModule.cs` — `campaignGameStarter.AddBehavior(...)` in
  `InitializeGameStarter`, after the existing `is not Campaign` guard.

## 6. XML changes (map module) — likely one, conditional

`HeroCreator.CreateNotable` resolves templates through
`HeroCreationModel.GetRandomTemplateByOccupation(occupation, settlement)`, which draws from the
settlement culture's notable templates. Verify `Culture.empire` in
`DellarteDellaGuerraMap\ModuleData\culture\dadg_cultures.xml` lists the preacher templates in
`notable_and_wanderer_templates`; if not, add `spc_notable_vlandia_2` (+ female variant) there.
Fallback if the model can't be satisfied: `HeroCreator.CreateSpecialHero` with an explicit
template lookup and manual notable placement.

Free polish (no code): vanilla village missions spawn a "preacher notary" helper NPC in the scene
when a preacher notable exists, using `Culture.PreacherNotary` — the map module already defines
`preacher_notary_vlandia`. Check the culture wiring; if hooked up, abbots get ambient company.

## 7. Verification

1. `dotnet test DellarteDellaGuerra.sln` — `DonationPolicyTests`: happy path, insufficient gold,
   on cooldown, cooldown exactly elapsed.
2. `dotnet build DellarteDellaGuerra.sln --configuration Release` deploys DLLs.
3. Live playtest (bannerlord-debug-playtester):
   - New campaign: each of the 16 church villages has exactly one preacher notable; a control
     village (e.g. Axminster) has none.
   - At Tintern Abbey: greeting shows; donate → gold −500, relation +2, renown +1; donate option
     gone immediately after; save/load → cooldown persists; +7 days → option returns.
   - Save/load again: no duplicate abbots.

## 8. Known risks

- **Template resolution** (§6) is the main unknown — resolve before writing the spawn loop.
- **Notable-balance models**: vanilla notable spawn/death balancing may treat the preacher as an
  excess village notable and cull it. The daily respawn check masks this; if culling is observed
  in the playtest, patch the relevant model in v0.1.x rather than pre-emptively.
- **Relation UI**: relation with a notable is standard, but confirm the encyclopedia/notable bar
  reflects it so the donation feels rewarded.
