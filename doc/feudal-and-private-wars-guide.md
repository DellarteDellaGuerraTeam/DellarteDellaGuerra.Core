# Titles, Claims and Private Wars — A Reader's Guide

*A plain-language tour of what the feudal system actually does in game. No code, no
architecture. For the technical documents see `feudal-titles.md` (architecture),
`feudal-private-wars-design.md` (design decisions) and
`feudal-private-wars-manual-test-guide.md` (how to test it).*

---

## The short version

Vanilla Bannerlord knows two things about a lord: which kingdom they belong to, and which
settlements they own. That's it. There is no such thing as a *title* separate from land, no
such thing as a lord owing service to another lord inside the same kingdom, and no way for two
lords of the same realm to fight each other without one of them defecting.

This mod adds all three. England is carved into a permanent ladder of dignities — baronies
under counties, counties under duchies, duchies under the crown — and every lord sits somewhere
on it. Lords owe service upward and command service downward. And when a lord believes a title
is rightfully theirs and the man holding it will not give it up, they can go to war over it
*without either of them leaving the kingdom*. That is the Wars of the Roses in one sentence,
and it is the thing vanilla cannot do.

---

## Part 1 — The ladder

There are four ranks in play (the system also understands Emperor, unused in England):

> **Barony → County → Duchy → Kingdom**

Every title is anchored to a **seat** — one specific settlement. Every county is seated at one
of the 28 towns, every barony at a castle, and the six duchies at castles held by their duke.
The tree is fixed at the start of the campaign and never changes shape: the County of York is
always inside the Duchy of York, whoever happens to hold either.

One deliberate oddity: **the crown has no seat.** London is held as an ordinary county
underneath it. This is so the throne can change dynasty without London changing hands, which is
exactly what happens in the 1471 Readeption when Henry VI is restored.

## Part 2 — A title is not the land

This is the single most important idea in the system, and the one that makes everything else
work.

Vanilla treats "owns the castle" and "is the Baron" as the same fact. Here they are two
separate facts, and they are allowed to disagree:

- The **holder** is who the dignity belongs to — the rightful Baron.
- The **occupant** is who is physically sitting on the seat.

When someone storms a castle whose title already has a holder, they become the **occupant**.
They do *not* become the Baron. The dispossessed holder keeps the dignity, and the title is now
flagged **contested**, with a clock running from the day the occupation began.

If the rightful holder retakes their seat, the contest simply evaporates — no ceremony, no
decision, they were the Baron the whole time.

If the seat was *vacant* — nobody held the dignity — then conquest does transfer it outright.
There is no one to dispossess.

## Part 3 — Claims

A **claim** is a clan's standing assertion that a title should be theirs. Claims come from
three places:

| Origin | When it appears | Strength |
|---|---|---|
| **Conquest** | A clan loses a dignity to a reassignment | Strong |
| **Inheritance** | A clan leader dies and an heir is passed over | Strong |
| **De jure** | Authored in the campaign's starting data | the strongest kind |

Claims are sticky. Take a barony off a rival and they will hold a grievance against you for the
rest of the campaign unless it is settled — and that grievance is what will eventually put an
army outside your gate.

## Part 4 — Tension, and the petition

Every day, every outstanding claim adds a point of **tension** to the clan holding it. Weak
claims accumulate at half speed; de jure claims at half again as fast. The moment a clan
actually gets the title, its tension on that title resets to zero.

At **10 points** — so roughly a week for a strong claim, three weeks for a weak one — the
claimant does something about it: they raise a **Feudal Petition** in the kingdom, asking their
suzerain to formally recognise the claim. Petitions cost nothing to propose. Only one petition
per clan can be pending at a time.

The kingdom rules on it:

- **Grant Petition** — the claim is recognised and strengthened.
- **Deny Petition** — tension surges. This is the road to open war.

If you are the suzerain, denying a vassal's petition is the cheap answer *today*. It is also
how you end up fighting them.

## Part 5 — The act of attainder

When a title has been contested — held by one clan, occupied by another — for **30 days**, and
both clans are in the same kingdom, the crown is forced to settle it. A **Feudal Attainder** is
put before the realm:

- **Attaint and Regrant** — strip the dispossessed holder and hand the dignity to whoever has
  been sitting on the seat. Possession becomes right.
- **Uphold the Dignity** — the holder keeps the title. Occupying a seat grants nothing.

The decision deliberately sits for a **full week** before it resolves, rather than the usual
kingdom-vote timing. That week is a gift to the dispossessed lord: retake your seat before
parliament assembles and the act is moot.

Attainder is *internal kingdom law*. If the occupant belongs to a foreign kingdom, no attainder
is raised — that has to wait for a peace treaty.

## Part 6 — Who decides who gets a vacant fief

Vanilla decides settlement grants by a kingdom-wide vote with its own rules. Here, no vote is held
at all when a fief is taken by siege, when its clan leaves the kingdom, or when its clan dies out:
the king keeps it. A clan that leaves or is destroyed keeps no claim on what it gave up. The votes
that remain (a relinquished fief, an annexed one) are replaced:

- **The chooser is the de jure Duke** of the settlement in question — or the King, for counties
  that sit directly under the crown. Not the ruler by default; the lord whose vassal the new
  holder will actually be.
- **Anyone holding a claim on the title is added as a candidate**, whether or not vanilla would
  have considered them.
- **Votes are weighted by feudal distance.** The seat's own suzerain speaks with triple weight,
  a peer under the same suzerain with one and a half, an outsider with a fifth of a voice.

The practical effect: land tends to stay inside the duchy it belongs to, and dukes matter.

## Part 7 — Rank is worth something

Holding high dignity is not just flavour. Your highest title feeds into four separate systems:

- **Influence per day** — Baron +0.5, Earl +1.0, Duke +2.0, King +3.0.
- **Overriding kingdom decisions** — a high-tier proposer pays 30% less influence to force a
  decision through; a very low-tier one pays 50% more.
- **Army influence** — the hourly award for riding in an army scales with your tier.
- **Settlement loyalty** — a town held by its rightful titular lord gains **+1 loyalty a day**
  ("De Jure Holder"). A town whose lord is *not* the rightful claimant loses **−1 a day**
  ("Contested Title"). Sitting on land you have no right to is quietly expensive.

And tension cuts the other way too: a clan sitting on unfulfilled claims becomes measurably
more likely to leave the kingdom entirely. Ignore your vassals' grievances long enough and they
walk.

## Part 8 — Levies

When a lord raises an army, every de jure vassal clan in the same kingdom is **called to the
levy**. They have **three days** to muster:

- **Answer** (join the army) → **+2 relation** with the lord who called you.
- **Refuse** (fail to join in time) → **−5 relation**, both directions.

This is the everyday texture of the hierarchy. It also means calling a levy you don't need is a
way to burn goodwill, and answering one you'd rather ignore is a way to build it.

## Part 9 — Private wars

Everything above is the powder. This is the spark.

### What it is

A **private war** is a real, map-represented war between two clans **who both remain in the same
kingdom**. Nobody defects, nobody forms a splinter faction, nobody loses their seat at the
king's council. They simply become enemies of each other, and of nobody else.

### Declaring one

A private war is pressed under a **casus belli** — currently one kind: *pressing a claim on a
title*. To declare, you need:

- an existing claim on the title, and
- a defender who actually **holds land** belonging to that title.

A landless defendant cannot be attacked. There is nothing to take.

### The main goal

At the moment of declaration the war picks its **main goal**: the richest settlement of the
claimed title that the defender currently holds. **This never changes.** Not if the defender
loses it, not if a third party grabs it, not if the war drags on for years. The war is *about*
that one place, permanently.

The war also quietly photographs who owns every fief in the realm, for reasons that become
clear when it ends.

### Who fights

You do not pick sides — the ladder does. Every clan's allegiance is worked out by walking
**up** their chain of lieges until it reaches one of the two principals. Whichever one it hits
first, that's the side they fight on.

Two consequences worth internalising:

1. **Attacking your own liege takes your vassals with you.** Their chain hits you before it
   hits him.
2. **Clans whose chain reaches neither principal are simply not in the war.** They trade with
   both sides, garrison for both sides, and notice nothing.

### What changes for the belligerents

Against a private enemy — and *only* against a private enemy — you can:

- besiege their castles and towns, and continue a siege in progress
- attack them in the field, and attack their armies
- raid and take hostile action against their villages

And you cannot:

- recruit troops from their settlements
- enter their settlements at all

Everyone else in your kingdom stays exactly as friendly as they were. Uninvolved lords will not
join in, will not take offence, and will not be dragged into your quarrel.

### Telling friend from foe on the map

Because a private enemy is still, technically, a member of your own kingdom, the map needs to
tell you what your eyes would otherwise get wrong:

- **Private enemies** are tinted **vivid orange**.
- **Clans called onto your side** are tinted **blue-violet**.
- Everything else keeps its ordinary colour — including real diplomatic allies, who stay blue.

The colours change the instant a war starts or ends, without reopening the map or reloading.
Both are configurable in `dadg.config.xml` if the defaults are hard for you to distinguish.

### Winning: the war score

The war keeps a single running score from **−100** (defender winning) to **+100** (attacker
winning). It is recomputed constantly from the state of the world:

| What | Worth |
|---|---|
| Holding the main goal | 50 |
| Each of the enemy's towns you hold | 30 |
| Each of the enemy's castles you hold | 10 |
| Each enemy lord in your dungeons | 5 |
| Battles won | accumulates over the war |

Plus one term that guarantees the whole thing ends: **fatigue**. Every day, one point drifts
toward whoever currently holds the main goal. Two lords who refuse to fight and refuse to
negotiate still get a result in about fifty days — the one sitting in the disputed castle wins
by simply *being there*, which is very nearly how these disputes actually resolved.

Taking the main goal back resets the fatigue clock to zero. The drift starts over, now running
in your favour.

### Sieges between kinsmen

Same-kingdom sieges are fought like any other siege. Once a besieger's preparations are complete
and it judges the walls winnable, it **assaults**: a real battle with casualties on both sides, which
you can watch or join. Nothing changes hands without a won assault, so a weak besieger may simply sit
outside a strong garrison. Lords of the kingdom who are not in the war stay out of it, and you may
join only the side opposite your own private-war enemy.

A won assault gives the fief to **the clan that took it**, not the king. It holds the fief as
**interim owner** while the war lasts and keeps scoring for its side; the war's end decides who
keeps it. If the claimant marched in the capturing army without leading it, the main goal goes
straight to the claimant. No kingdom vote follows the capture, and titles never move while the war
is live. Lords who fought and lost are taken by the usual post-battle rules; a lord who sat out the
assault inside the walls is not captured automatically.

This works identically in both directions: the dispossessed side can march straight back and
retake the same fief by the same route. Capturing the goal does *not* end the war — it moves
the score and flips the fatigue drift, but the loser is free to come back for it.

If a besieged garrison is strong enough — more than twice the besieger's strength, or one and a
half times with a relief force nearby — it will **sally out**, exactly as it would in a normal
siege. Militia stay behind the walls, as in vanilla. A garrison whose morale has fallen to nothing (for
instance one packed far beyond its size limit) stays inside however strong it is: a broken garrison
would be routed at the first clash and lose the castle.

### Prisoners

Lords taken in a private war are **held for the duration of the war and released when it ends**.
The usual "we're not at war any more, let everyone go" sweeps do not free them; neither does the
end of a battle. Ransom still works. Deliberate release still works. And nobody's escape is ever
blocked — a determined captive can still get over the wall on their own.

The player is never held by this rule.

### How it ends

The moment the score touches **+100** or **−100**, the war is over, and the peace is a **status
quo ante**: every fief that changed hands during the war goes back to the lord who held it on
the day war was declared.

With two exceptions:

- **If the attacker won, they keep the main goal — and the title with it.** That is the prize,
  and the entire point of the exercise.
- **Anything a third party grabbed in the confusion stays with the third party.** They were not
  in this war; it is not this war's business to move their land.

If the defender won, the attacker's claim is spent.

## Part 10 — Where to see all this in game

- **Encyclopedia → any clan page** gains a *Feudal* section: their titles, their suzerain, the
  full chain of lieges above them, their direct vassals, and any levies currently out.
  (Clans outside the hierarchy simply don't show it.)
- **Encyclopedia → hero pages** carry their feudal standing.
- A **Feudal Hierarchy screen**, reachable from any clan page, lays out the whole realm as one
  indented tree — every kingdom, duchy, county and barony, and who holds each.
- **Kingdom decisions** are where petitions and attainders arrive, alongside the ordinary
  kingdom business.
- **Map nameplates** carry the private-war colours described above.

## Part 11 — What isn't in yet

Being straight about the edges:

- **One casus belli.** Pressing a claim is the only reason you can start a private war. No
  border disputes, no personal grievances, no holy wars.
- **Titles belong to clans, not people.** Two brothers in the same house cannot hold different
  dignities. (A design study exists for moving to hero-level titles; it is not built.)
- **Naval private wars are out of scope.** Sea and port situations fall through to ordinary
  vanilla behaviour.
- **The player never triggers the automatic siege capture.** That path is for AI-run sieges; a
  player siege resolves the way any siege you fight resolves.
