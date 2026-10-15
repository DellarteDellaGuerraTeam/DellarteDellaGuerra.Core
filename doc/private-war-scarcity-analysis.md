# Why so few private wars?

Date: 2026-10-14. Evidence comes from the code on the working tree and from `dadg.log` covering about one in-game year (campaign started Summer 1471, observed through Summer 1473).

## Summary

Across about one in-game year there were **3 organic private wars**: Campbell–Crichton, Munro–MacPhee and York–St Leger. The Woodville–Sinclair war was declared with a cheat, so it does not count. In that time, **25 prosecution checks** were run across only **5 claimant/title pairs**.

The scarcity is not caused by a single gate. Five filters stack, in order of how much they cut:

1. **Claim supply.** Almost every claim between two clans is *Weak*. A Weak claim can only be pressed against a woman or a holder under 18.
2. **Scoring.** The threshold of 0.8 is out of reach for Weak claims. For Strong claims it needs either a distracted defender or a strength ratio of about 2:1.
3. **Support bandwagon.** Neutral clans join the stronger side, which is usually the holder. That pushes the ratio back under 1.25.
4. **`CanProsecute`.** Most pairs that survive the score are vetoed here, because their best real army is far smaller than the goal's defence.
5. **The 30-day cooldown.** Each claimant/title pair is re-evaluated at most monthly, so a pair that fails gets about 12 attempts a year.

A sixth factor was cleared up today: **goal armies kept being dispersed while their realm was at peace** (vanilla `NoActiveWar`). This did not prevent declarations. It did stop declared wars from making progress, and it drained the influence the lords needed to raise the next army. It is now patched.

## The pipeline

`ClaimPressureCampaignBehavior.OnDailyTick` (`DellarteDellaGuerra/Titles/Api/Campaign/ClaimPressureCampaignBehavior.cs:130`) looks at every titled holder, takes the best claim per claimant clan, and applies these gates in order:

| # | Gate | Line | Effect |
|---|------|------|--------|
| 1 | Title has a holder whose clan is alive | 149–152 | Minor |
| 2 | `WeakClaimPolicy.IsPressable`: a Weak claim is only pressable against a woman or a holder under 18 | 158 | **Large.** Removes most claims between clans (see below) |
| 3 | Neither side is the player clan | 160 | Minor |
| 4 | An internal claim (on the claimant's own house's title) needs a living *pretender* hero | 164–166 | Removes clan-level internal claims |
| 5 | Same kingdom only | 168–169 | Large in a two-realm England (York/Lancaster) |
| 6 | Claimant is not already a belligerent | 171 | One war per clan at a time |
| 7 | 30-day pair cooldown, **stamped before evaluation, even when the pair is rejected** | 173–175 | Caps attempts at about 12 per pair per year |
| 8 | `SelectMainGoal` finds a goal settlement | 177–180 | Minor |
| 9 | `SolicitSupport`, then `EvaluatePressClaim` ≥ **0.8** | 221–225 | **Large** |
| 10 | `CanProsecute`: the side can field an army big enough for the goal today | 232–237 | **Largest, among pairs that reach it** |

## 1. Claim supply: nearly everything is Weak

`GenerateBloodClaimsUseCase` (`DellarteDellaGuerra.Domain/Titles/GenerateBloodClaimsUseCase.cs:19-21`):

- A holder's **son** gets a **Strong** claim, and a **daughter** gets a **Weak** one.
- A Strong claimant's children get a **Weak** claim.
- A Weak claim passes to nobody.

This means Strong claims nearly always stay inside the holder's own house: sons, and passed-over younger sons. Those are *internal* claims, so they need gate 4, the pretender/cadet path. That path has two built-in disadvantages:

- The attacker side is tiny. It is the pretender's own party plus whichever clans pledge to him (`ClaimPressureCampaignBehavior.cs:203-205`).
- The relation term works against it, because family members usually like each other.

Claims held by *another* clan come from daughters and grandchildren who married out, and those claims are all Weak. Gate 2 then drops them unless the holder happens to be a woman or a minor.

The other kinds of claim:

- **Conquest claims** are only created by a non-Conquest title transfer (`AssignTitleUseCase:111`). That is rare until private wars actually change who holds titles, so this is circular.
- **De jure claims**: nothing in the code creates them.
- **Seeded claims**: there are no XML seed claims.

## 2. The score: 0.8 is a high bar

`EvaluatePressClaimUseCase` (`DellarteDellaGuerra.Domain/Titles/EvaluatePressClaimUseCase.cs`):

```
score = appetite                                   Weak 0.35 / Strong 0.6 / DeJure 0.8
      + min(A/D − 1.25, 1) × 0.25                  0 below 1.25 : 1 → the claim scores 0 outright
      + (defender distracted ? 0.25 : 0)            defender belligerent, or realm at war abroad
      − relation/100 × 0.2
```

What each claim strength needs to reach 0.8:

| Claim | Ways to reach 0.8 |
|-------|-------------------|
| Weak (0.35) | Ratio ≥ 2.25 **and** distracted (0.85). Or ratio ≥ 2.25 **and** relation −100 (0.80). Otherwise impossible |
| Strong (0.6) | Distracted with ratio ≥ 1.25 and relation ≤ +25. Or ratio ≥ 2.05 with relation ≤ 0 |
| DeJure (0.8) | Ratio ≥ 1.25 and relation ≤ 0. But no de jure claims exist |

So a Weak claim that gets past gate 2 also needs an overwhelming 2.25:1 advantage. "Distracted" is often true in DADG, because York and Lancaster are usually at war with someone. That makes Strong claims viable on paper, but they are almost all internal (section 1).

## 3. Support goes to the stronger side

`SolicitSupport` gives the stronger side a bandwagon weight of 0.5, and the holder is normally the stronger side. Legitimacy pulls by claim strength: Weak 0, Strong 0.15, DeJure 0.3.

The calls to arms are priced on what the feudal hierarchy alone can field. The war is then **re-priced on the sides the calls produced** (lines 218–225). So a claimant who clears 1.25:1 on hierarchy strength alone often loses that edge once the neutrals have all pledged to the holder. A Weak claim has no legitimacy pull to set against this.

## 4. `CanProsecute` vetoes most survivors

`CanProsecute` asks whether the attacker side could field an army today that is big enough to besiege the goal. This is what was seen in the log:

| Pair | Checks | Result | Why |
|------|--------|--------|-----|
| Stafford vs Woodville | 18 | Failed every time | Best army 0–447 against a defence of about 700–2400 |
| Ross vs Macalister | — | Failed | Best army 0 |
| Munro vs MacPhee | 3 | Failed twice, then passed | War declared |

The score uses **whole-side strength on paper**: every vassal and supporter, wherever they are. `CanProsecute` uses **lords who are free, fed and have influence right now**. These two numbers disagree a lot:

- Lords are scattered across the realm's own kingdom war.
- Many lords are short of influence. The army churn in section 6 made this worse.
- Fortress garrisons in DADG are large.

A pair can pass the score month after month and fail here every time. Stafford is the clearest example.

## 5. The cooldown limits how often a pair can try

The 30-day stamp is written *before* the pair is evaluated (line 175), so a rejected pair waits a month even if the situation changes the next day. This is intentional: it stops a pair that sits just under the threshold from declaring the moment the numbers wobble. It does mean the whole realm gets only a few real chances per in-game year.

## 6. Goal-army dispersal at peace (fixed)

Vanilla `Army.CheckArmyDispersion` (1.4.8 `Army.cs:423`) disbands an army with a 25% chance every hour when its kingdom has no enemy holding fiefs. A private war is not a kingdom war, so while York was at peace (until Spring 19, 1473) the York vs St Leger goal army was dispersed and re-raised **7 times in 4 days**. Each raise cost influence; one clan ended at −552.

After the vanilla war resumed, York's lords could not raise any armies. This produced the lone-lord sieges at Carlisle and Skipton. Over all logs, `NoActiveWar` caused 33 of the 59 goal-army dispersals.

The fix is `GoalArmyNoActiveWarDispersalPatch` (a Harmony prefix on `DisbandArmyAction.ApplyInternal`). It skips the `NoActiveWar` dispersal for the goal army of an active private war. Live verification is in progress.

## Possible levers (not implemented)

Ordered by expected effect per unit of risk:

1. **Let foreign Weak claims count against adult male holders under some condition**, for example a holder with low honour or a defender who is distracted. Gate 2 is the biggest single filter.
2. **Have `CanProsecute` measure against a reachable goal**, such as a village, a weaker castle, or the defence the side would actually face, rather than the fixed main goal. Alternatively, let a failure here *not* consume the 30-day cooldown and retry after 7 days.
3. **Give legitimacy something against the bandwagon for Weak claims**, or price support on the side's relation to the holder rather than only on strength.
4. **Seed claims**: historical Wars of the Roses claims in XML (e.g. Neville/Percy, Courtenay/Bonville, Berkeley/Talbot), and de jure claims for vassals on titles inside their de jure liege's domain.
5. **Bring the 1.25 floor and 0.8 threshold down slightly** (e.g. 1.1 and 0.75). This is cheap and global, but it changes every pair at once.

Before tuning any of these, it would help to add debug logging of *which gate rejected each pair*. Today, only pairs that reach gate 10 produce a log line, so the number of pairs dropped at gates 2, 5 and 9 is not directly measured.
