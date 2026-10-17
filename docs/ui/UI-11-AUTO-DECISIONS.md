<a id="ui-11--prywatne-pomijanie-kart-bitwy"></a>
# UI-11 — Private omission cards battle

The shortcut executes the U08/S13 requirement, not the new rule of the game. The order cards remains in accordance with B11 and [T18](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/rules/T18-BATTLES.md): attacking, defender, disclosure, effects. The lack of card is the same open choice regardless of its approval method. The availability of cards never decides to open the window.

<a id="wejście-i-legalność"></a>
## Entry and legality

`SetCombatCardPolicy` uses existing authenticated endpoint commands and transactions with a match lock and a receipt. The owner is from a session, never a payoad. The change includes `battleId`, the expected private version of the settings, the hand version and `enabled`. The inclusion requires the current battle and the unchanged hand. The disable compares the version of the setting; it does not require a current public revision or its current decision. This allows canceling future responses when selecting the opponent.

`AutoDecisionPolicy` binding `SkipCombatCards` with owner, one battle And the hand version, each real addition/usuni hair card increases the hand version and extinguishs the policy, also with the identical final set cards - That's it. battle/game It also extinguishs politics. battle requires reactivation.

<a id="zapis-kontynuacja-i-prywatność"></a>
## Recording, continuation and privacy

The change of preferences does not increase public review itself, adds no history and does not publish a notification to the opponent. It changes the private version of the owner. If the inclusion corresponds to its already open window card, this response increases the revision like a simple decision. The subsequent automatic responses are part of the atomic continuation of the triggering command. They always use `BattleCards.Commit` with the empty card, after the actual opening of the right decision. They never account for loss, spread, continuation, retreat or effects of card.

The projection only makes available its own policy, its version, the hand version and the last round omitted in this battle. Public logi/windows/choices remains like with the hand-held "No card". The change of preference without response does not change the enemy's projection even with direct GET. The client rejects the late projections with the lower private version on the same game revision.

Cancellation race determines the order of the transaction. Cancellation before opening the window stops the automatic. Cancellation after the saved reply disables future answers; the receiver and the owner view give the last missed round. We do not undo the selection. The execution of the reply does not change the version of the same setting, so the pending cancellation remains legal. Renewing the same `commandId` returns the previous receipt; other data with this ID are conflict.

<a id="interakcja"></a>
## Interaction

A small button in the own resource panel remains available during battle, also when the opponent decides. The label clearly distinguishes the inclusion of the “Skip cards this battle” and the “Ask for cards” switch. The condition is valid only after the server has responded. Short notice of the owner indicates the last missed round, and the tooltip explains the expiration after the hand change and the lack of undoing the answer. Without additional dialogue, scrolling or numerical field.

<a id="weryfikacja"></a>
## Verification

Domain tests: both roles, order of both slots, no automation of other decisions, cancellation after reply, hand change/draw-play-discard, End/new battle , illegal version/battleId And the same projection of the opponent. PostgreSQL/HTTP: reboot, retry, rollback, competitive cancellation and approval, privacy receipt/powiadomie Unity: real server, on while waiting, reconnet, cancel, saved result and layout 1366×768 and 1920×1080 with 100/125%.

The results of the checks performed will be added to [TESTING](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/TESTING.md). The migration of historical fingerprints of commands remains a separate T37 range; we do not declare the old receipt compliance after adding the fields of contract.
