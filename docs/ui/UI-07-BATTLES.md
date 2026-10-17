<a id="ui-07--bitwa-bez-formularza"></a>
# UI-07 — battle without form

<a id="przepływ-i-odpowiedzialności"></a>
## Flow and responsibility

Sources of rules and domain cases: [T18](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T18-BATTLES.md), [T19](https://github.com/jpolaczek/wotr-server/blob/777d590353667fe7b7a7b6f96f7498f7da3c1540/docs/rules/T19-SIEGES.md), BASE p. 26–32. UI uses `BattleView`, options and server preview. It does not draw, does not discover someone else's card and does not calculate the authoritative result.

The attack is a separate intention: die → army → participants → highlighted target → one approval. The default marked all available army; the player himself improves the composition when rear guard is needed. The attack does not transfer figures in preview. The Field/Siege/Sortie/Relief mode comes from the server option; the garrison remains a separate layer of the map.

The fixed tray shows the attacker on the left and the defender on the right, regardless of the player's side. Each column contains the composition (details on request), card/reverses and separate results of the first throw and reroll. The hit has a sign and color; the tooltip explains the threshold, modifiers and source. The lack of animation of the throw eliminates delay and is also a mode of limited movement.

If a special attack has occurred, the C/S switch in the column shows its actual throws and source instead of the regular pool. More than five results have subsequent groups available with the same button, without scrolling; tooltip describes the displayed group. The decision of losses does not disappear on switching. The list of many eliminated commanders is a full tooltip of a short summary, which does not increase the tray height.

card is sent to a local plan from hand, and one approval sends a secret selection. The clear `No card` button immediately sends a refusal. The other reverses do not have identifier or action inspection. The metastatic throws are selected directly in the column of their army; no choice is not automatic approval.

Losses: figure are buttons, not a numeric field. The selected elite has two separate `Downgrade` and `Remove` buttons. The order of local operations remains public (essential when recovering regular to later degradation). The server preview shows the replacement figure or its absence and elimination of remaining army-less commanders. The change of choice is reversible for approval; correction after approval does not reverse the earlier decision.

Continue/Stop/Hold/Fight in field/Retreat inside are standalone approvals. The retreat selects the target on the map. After the victory, the participants of the entry are indicated figure, and `Stay` is a separate informed decision. The extension of siege requires selecting one participating elite and previewing its degradation. We do not automatically select it.

<a id="odtwarzanie-i-czytelność"></a>
## Playback and readability

The change of decision removes the old plan, reconnecting with the same decision retains the legal choice. The history of local undoing does not exceed the revision. die costs remain marked as paid by the whole battle The camera doesn't move automatically.

The last revealed round has a limited to one public entry, also available after the automatic end of battle and reconnection. We do not fix the private selection of the spreads or undisclosed card. Reading the report does not create an additional decision of the game.

<a id="plan-sprawdzenia"></a>
## Verification plan

EditMode : undo/deep copy loss, local selection validation, attack versus move, no default input/refugee card , maintain a legal plan after reconnect. PlayMode : two real sessions HTTP/WebSocket , attack and all decisions on domain states; fixture of the presentation of reverses/disclosures and extremes, no regular, 1366×768 and 1920×1080 with 100/125%. Border control, available buttons, missing scrolla/p The domain tests check the privacy and durability of the report.

All cards Combat programs belong to T21. Fixture of card presentation does not mean implementation its effect. Automatic omitting of cards windows belongs to UI-11.
