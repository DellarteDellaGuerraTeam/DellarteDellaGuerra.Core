# St Andrews episcopal ownership handoff, spring 1471

Recorded 3 November 2026 for `feature/add-church`. **Deferred by user request:**
document the issue here; do not change St Andrews ownership in the secular-clan pass.

## Existing placeholder

| Item | Current value to preserve in this pass |
| --- | --- |
| Settlement | `dadg_sco_st_andrews_town` |
| Starting settlement owner | `Faction.clan_lindsay` |
| Regional title | `county_fife`, geographic County of Fife |
| Configured regional title holder | `clan_stewart` |
| Regression exception | Only this exact settlement / Stewart title / Lindsay owner combination |

Neither placeholder represents the historical lord of St Andrews. The settlement
was an **episcopal burgh**, and St Andrews Castle was the bishop's residence.
**Patrick Graham was Bishop of St Andrews in early 1471**; the archbishopric dates
from 1472. Do not introduce an hereditary Graham family ownership or use the later
archiepiscopal title at this start. Fife's geographic region and the bishop's actual
estate must remain distinct.

Evidence: [University of St Andrews burgh records](https://collections.st-andrews.ac.uk/collection/records-of-the-burgh-of-st-andrews/2029969),
[Historic Environment Scotland, castle history](https://www.historicenvironment.scot/visit/all/st-andrews-castle/history-and-stories/).

## Work for the church branch

- Reconcile institutional estate ownership, the bishop's person, and the geographic
  Fife title. Appointment and church succession must not become ordinary hereditary
  clan inheritance merely because Bannerlord represents ownership through clans.
- Integrate with the existing `doc/church-v0.4-bishops-and-favour.md` and
  `doc/church-v0.6-diocese-view.md` on `feature/add-church`. Those documents are not
  present on this working branch; this handoff avoids importing their implementation.
- Review St Andrews as the cathedral seat for its diocese. The church configuration
  requires exactly one Cathedral per diocese; verify actual settlement eligibility
  rather than assuming every church estate is a Village.
- Audit the bound villages individually. Current bindings do not establish episcopal
  ownership of every estate in the regional rollup.
- Once the institutional representation is implemented, update both live starting
  ownership and title seeding as appropriate, then remove/revise the exact exception
  in `StartingOwnershipOverRealContentTests` and the independent political-map pin.
  Check that labels, borders and bishop succession follow the institutional model.

The [starting ownership audit](starting-ownership-audit-1471.md) corrects the other
26 mismatches. St Andrews is a known unresolved issue, not an historically approved
Lindsay holding. Existing saves and new campaigns retain this placeholder until the
church work is implemented.
