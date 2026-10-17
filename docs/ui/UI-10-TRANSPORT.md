<a id="ui-10--rewizje-podgląd-i-odzyskanie-komendy"></a>
# UI-10 — revision, preview and recovery command

<a id="kontrakt-przed-implementacją"></a>
## Contract before implementation

Recovery of uncertain command takes precedence over the draft tips. The change of election message after the new revision cannot disable `Check result`. The availability of recovery depends on the connection and ongoing request; confirmation of the new action still requires an up-to-date preview. The actual `UI10NetworkTests.Receipt` also checks the availability of the button after the opponent's move when the old cost has disappeared from draft.

The range is based on R09/R10/R11/R17/R21, U03/U11, S20–S21 and [transactions T07](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/server/T07-TRANSACTIONS.md). The server remains the owner of legality, cost, draws and write. The client does not interpret the receipt as the new state of board; he collects a safe projection. We do not change the DTO nor the rules of the game.

| Entry | Conditions of acceptance and result |
| --- | --- |
| HTTP projection by WSS or reconnect | Consistent game/page; none of the public revisions, private politics and own-hands diminish. The identical set of versions is a duplicate and does not rebuild the decision or history of Undo. |
| Late message WSS | It only serves as a download signal for a newer projection. It does not overwrite the view, does not undo the revision and does not perform command. |
| New revision on draft prepared | Invalids the old preview. A compatible window retains legal choices by `Reconcile`; removed items need to be corrected. The new window battle, Hunt or effect starts the new draft. The change of private policy itself does not delete Undo. |
| Prev Answer | Accepted for the same local generation only, revisions, decisions and rodzaju/payloadu. Canceled or older responses do not change status or approval. |
| Conscious approval | One new click or cycle pressing/release Enter; for draft you need a legal current preview. Simple signed decisions still have one direct click. After changing the state you need to see a new preview and confirm again. |
| Timeout after POST | Sender retains a copy of the exact original command. `Check result` first GET receive, and only with 404 repeats the same identifier and payload. Another read error does not mean that you can repeat POST. |
| Several Sender Calls | During the ongoing trial they share one request. They do not replace uncertain command. Only the receiver with her identifier completes the wait. |
| Late Receipt | It does not clean draft of the new window, does not lower the revision. Until the fresh projection blocks sending another decision from the old view. The rejected intention is retained to a legal extent; its re-confirmation requires new confirmation. |

The version comparison will be a small transport class used by `MatchConnection` and defensively by `TurnInteraction`. The transport will only keep its counters and identify its own view. We do not serialize the entire view on each frame or compare the entire DTO tree.

The preview generation changes synchronously with editing, new revision and connection state. Sending stores an independent snapshot command; the subsequent edit of the original object cannot change the retra. The `sending` and the sender field protects different boundaries: the availability of interaction and a single network test. The camera, the cards inspection and local preparation of the unsent draft continue to work on the break.

`DraftInput` retains lock of the held Enter by changing the decision and losing focus until release. The second part of the two-click is rejected both when pressing and releasing the indicator; it does not activate the new control after response.

A temporary view of waiting on the opponent does not need to remove the local intent of the normal action. `TurnDraftSession` stores its copy and previous context, hiding outdated targets. After returning to the corresponding decision `Action` plays draft and filters it with current options. The new turn, phase, battle, Hunt, event loss or win removes the copy. The approval of its own command also removes its parked intent; it never automatically repeats.

Each draft has a identifier decision by which the user created it or edited it. The receiver cleans only this intention, also if it has been moved to the next compatible window. It does not clean the new mandatory draft created by WSS. `requiredRevision` blocks another commit until the projection reaches a revision of the saved receipt; the receiver older than the current view does not reverse any data.

<a id="plan-weryfikacji"></a>
## Verification plan

Editor: forced order change of two HTTP GET, revision duplicate, older politics/Hand, one POST at tho-current calls, unchanged payoad after timeout, GET receipt before retry, 404 versus server error and incompatible ID receipt. These targeted transport tests are not replacing the server.

PlayMode with real Kestrel/PostgreSQL coordinates root: delayed/old preview, loss of post-write response, race of two devices, late WSS, double click and held Enter, camera behaviour and attached card. A separate [audit draft](UI-10-DRAFTS.md) includes a correction of legal choices and Undo barrier. Results will be added after execution, without being counted on the basis of the plan itself.

<a id="implementacja-i-stan-weryfikacji"></a>
## Implementation and state of verification

Saved `ViewVersion`,  `TurnDraftSession` amendments `MatchConnection`,  `CommandSender`,  `TurnInteraction` and `DraftInput` New `UI10TransportTests` have six controlled border cases HTTP and version; `UI10DraftSessionTests` five waiting cases (also dice selected before action), a late receipt and a new mandatory window. These are isolated tests; acceptance the server and the actual Unity remains a separate step of the coordinator. The proper Unity tests did not start due to refusal `Licensing IPCGlobalMutex` in a new environment; log `.artifacts/UI10-editor.log` Additional whole compilation 168 files Runtime/Contracts/Fixtures/Editor/PlayMode by installed C# compiler and the actual Unity libraries ended **PASS**, exit  0  (0,91 s), no errors; others CS0649 for fields of fixtures dessertized with JSON. Accurate command and reference list: `.artifacts/ui10-unity-compile/COMPILE.md` and `compile.rsp`; result `.artifacts/UI10-unity-offline-compile.log` It's one additional library. NET   10, without Importer/asmdef nor the execution of the native Unity, so it is not acceptance   Editor/PlayMode. Additional mileage `python3 .artifacts/ui10-unity-assemblies/compile.py` ended **PASS   6/6**, with separate Contracts borders →  Client  →  Fixtures  →   EditorTests/PlayTests/Editor, real references/flags Unity and C# profile 9. Details are in the same COMPILE.md; no earlier own Wotr DLL does not replace current sources.

Coordinated runner has triggered real methods of new tests with NUnit and actual managed libraries: **42/42 PASS**, including five `UI10DraftSessionTests` and six `UI10TransportTests`, zero omissions. Log `.artifacts/UI10-draft-offline-run.log`, a list of methods and results `.artifacts/ui10-draft-runner/results.json`; exact commands in README runner. HTTPS address in tests with `QueueBackend` does not open the network and allows you to check the real `GameApi` without a native branch `Application.isEditor`. This is a separate managed control, still without a declaration of credit Unity Editor/PlayMode, WSS or rendering.
