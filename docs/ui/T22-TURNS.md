<a id="t22--odbiór-początku-tury-kosztów-i-zdolności"></a>
# T22 — acceptance start turn , costs and capacity

The description precedes the code. This is a supplement to acceptance existing rules, without changing the domain. Sources: BASE p.18–21 (order, pass and Rings), p.33–35 (politics), s.19/40-42 (suffrage Hunt) and full obverses of Gimli and Saruman of [SRC-02](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/sources/base-character-index.json), described in [T20](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T20-CHARACTERS.md). They apply R09–R11 and R15–R21 and [client contract](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/CONTRACTS.md).

<a id="wejścia-i-sprawdzane-zachowanie"></a>
## Inputs and checked behaviour

| Status saved by a separate seeder | Click sequence and conditions acceptance |
| --- | --- |
| `turn-hunt-optional` | A Regular Start turn → Continue Fellowship → allocation with minimum 0 → confirmation of empty selection. Throw and transfer first action FP executes the server. Random Eyes distinguish from intentionally assigned ID. |
| `turn-hunt-mandatory` | Recovery of dice traffic FP with Hunt causes the minimum 1. The empty selection is illegal and does not change revision; the selection of a specific dice opens a legal preview. die after approval has a public origin of `allocated`. |
| `turn-ring-politics` | Selected Ring + Specific die + Muster → one commit change of result, transfer of the Ring SH, still action FP. Then you must not fit or use the other Ring. The usual policy with the explicitly indicated cost uses exactly this die and moves the designated nation by one field. Rules still allow to use another dice; the test does not force anything in production. |
| `turn-pass` | FP has less available dice than SH. One direct click Pass passes the decision without changing any dice. SH having more dice cannot answer pass. |
| `ability-gimli` | Gimli in Erebor moves Dwarf politics behind the consciously indicated die Army. The wrong region does not change choice; before approving the position of the nation and the cost are unchanged. |
| `ability-voice-recruit-full/scarce` | Saruman recruits one regular Isengard in legal settlement. At the disadvantage, the choice of settlement is strategic; the illegal region does not change the plan. Full and limited reserves have separate saved parties. |
| `ability-voice-upgrade-full/scarce` | Saruman replaces two pairs and at one disadvantage. The preview is legal before the commit; both projections show the exact number of regular arrival elites and one issued die Muster. |

The election is created by the actual UI Toolkit, dice and map regions. The test expects a legal preview before confirmation. Negative payoadas are sent to the actual endpoint of preview/commands and compared with the pre-test view; they are not used to perform normal action instead of UI. Each accepted step increases the revision by one and goes to both clients through normal connections.

<a id="izolacja-i-weryfikacja"></a>
## Isolation and verification

`BaseTurnScenario.Create` and `BaseAbilityScenario.Create` are only the test states in the program `Wotr.Scenarios`; root records them in `BaseAcceptanceMatches`. The production server does not receive the status change endpoint. We use existing `MountedMatchClient` and `MatchTestRoom` with `ScenarioKind.BaseAcceptance`, real Kestrel/PostgreSQL and normal RNG server after the trial. We do not install the HTTP or the rule client.

PlayMode tests and compilation run centrally the T22 coordinator after freezing the files. Adding the script does not mean PASS. Results, actual commands and limitations detected will be added after execution. Own files are divided into turn/Capability seeds, tests of these two families and a small common asertion helper; we do not change the rules by the acceptance code.

<a id="implementacja-do-wspólnego-uruchomienia"></a>
## Implementation for joint launch

Nine independent saved states and seven PlayMode methods were added: `FellowshipCanContinueWithAnExplicitEmptyHuntAllocation`, `RecoveredFellowshipDieRequiresAnExplicitHuntDieBeforeRolling`, `RingChangesChosenDieThenPoliticsSpendsThatExactCost`, `LegalPassChangesActorWithoutSpendingADie`, `GimliAdvancesDwarvesWithTheChosenArmyDie`, `SarumanRecruitsAtExplicitSettlementsWithFullAndScarceReserves`, `SarumanUpgradeUsesTwoPairsOrTheOnlyAvailablePair`. The last two methods perform two variants on different batches. The recruitment additionally retains the selected region after reconnect. Upgrade compares the actual preview of returns and deployments with the later result.

The shortage of reserves results from figures placed on board, not by deleting the definition or changing the rule. Splitting them between Moria, Dol Guldur and Mount Gundabad avoids an untested army surplus. The compulsory allocation procedure causes the actual recovery of dice with the Fellowship movement designation.

Checks before transfer: `git diff --check` PASS ; `python3 scripts/tasks.py validate` PASS ( 88 at this point build and seven new methods PlayMode they have not yet been launched; the coordinator has exclusive access to these processes and record the final T22 result.

Final result: all **7/7 methods PASS** Full **172/172 PlayMode**, `.artifacts/t22-play-full.xml` , 2026-10-05 22:13:25–22:20:03 UTC , no omissions. The Ring Test additionally confirms the lighting only indicated Ring and dice . [Protocol T22](T22-ACCEPTANCE.md) document this detected presentation amendment and the entire course acceptance .
