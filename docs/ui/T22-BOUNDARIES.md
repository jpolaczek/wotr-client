<a id="t22--graniczne-decyzje-polowania-twierdzy-i-zwycięstwa"></a>
# T22 — Border Decisions Hunt, Stronghold and Victory

The document describes the separate part of acceptance T22. Sources: [B11–B14, B18 and B20](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/LOGIC.md), BASE p. 28–32, 40–42 and 44; requirements R09–R11 and R15–R21. We do not change the interpretation of the rules or production randomness.

<a id="kontrakt-przed-implementacją"></a>
## Contract before implementation

`BaseBoundaryScenario.Create(IRandomSource)` prepares only the initial recording of the test tool, marked with the prefix `boundary-`. After the batch is released, both clients use the actual backend and command UI; the test does not modify the recording or substitute the results of RNG server. The targeted items are not proof of the regular full batch from the setup — this acceptance belongs to T23.

| Scenario | Entry and selection | Result and limit of confidentiality |
| --- | --- | --- |
| `boundary-random-companion` | Paid Fellowship movement in Mordor, three injuries, Gimli/Legolas/Boromir level equal to 2. FP click Random company and confirm. | The preview does not select or reveal the future victim. The server removes exactly one legal Companion. Reconnect retains the same result and cost. The other two have the highest level: FP explicitly approves Guide, retaining the existing one if he survives; only then ends Hunt. |
| `boundary-field-defense` | The Shadow Army attacks Minas Tirith. FP receives StrongholdDefense and a fight in the field click. | battle remains Field, defenders do not become garrison, cost remains Resolving. Both sides openly resign from cards, select the required losses and finish the round. We do not assume the results of the real dice. |
| `boundary-military-fp` | FP has four military points; the action phase is underway, both sides have one more dice. | The first rejection does not end the game. Only the rejection of the last dice and the transition to the 6 phase gives FreePeoplesMilitary. |
| `boundary-military-sh` | Shadow has the points; the remaining one is dice from both sides. | Similarly, the win of ShadowMilitary follows the end of action. |
| `boundary-military-priority` | At the same time, FP has four points, Shadow the. | In the 6 phase, priority is given to ShadowMilitary. Both clients show the same page and cause; the reconnect retains the result. |

<a id="interakcje-i-sprawdzenia"></a>
## Interactions and verifications

- All the mutatis mutandis commands are derived from the trays/maps/dice clicks and the usual
  approval. Direct prompting for preview is an additional response control, it does not replace
  clicks.
- Random loss checks the collection of permitted victims, numbers, other injuries, possible decision
  Guide and transfer the same paid dice from Resolving to Hunt. It does not require a specific drawn
  figure.
- The preview and reconconnect before approval do not change the revision, composition, injuries or
  cost. Reconconnect after approval retains the victim, history and pending decision.
- Field defense checks out the open choice instead of shelter, proper mode, lack of garrison,
  maintenance of control to settle and one cost of the entire battle. Random hits support a small,
  open test plan of losses of regular units.
- Military terminations check the absence of an untimely result, the Finished phase, the absence of
  further decision/Confirm and the same score and story after reconnection.

<a id="plan-weryfikacji-i-odpowiedzialność"></a>
## Verification plan and responsibility

Implementation files: `tests/Wotr.Scenarios/BaseBoundaryScenario.cs` and `unity/Wotr.Client/Assets/Wotr/Tests/PlayMode/T22BoundaryNetworkTests.cs`. Seeders registration, common `MatchTestRoom`, compilation and launching Unity belong to the main agent. Results will be added after the actual central startup; this document does not declare the tests already taken. Manual user tests remain postponed.

<a id="wynik-końcowy"></a>
## Final result

All **5/5 methods PASS** Full **172/172 PlayMode**, `.artifacts/t22-play-full.xml` , 2026-10-05 22:13:25–22:20:03 UTC , no omissions. Fixed waiting for tests for `DiscardDie` au `Discarded` ; the server rule was correct. Stronghold additionally verifies acceptance actual event PointerDown/Up. With two panels RenderTexture common helper plays the input of the indicator to the panel first. Full diagnosis, previous failures and artifacts gives [Protocol T22](T22-ACCEPTANCE.md) .
