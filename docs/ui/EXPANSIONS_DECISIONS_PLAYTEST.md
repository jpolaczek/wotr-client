# Expansions decisions — playtest notes (EXP-15)

The owner's answers of 2026-10-26 to decisions D1–D22 of the [expansions UI audit](EXPANSIONS_UI_AUDIT.md), with the
mechanics that needed an explanation, so that each shape can be tried in the game once its task lands. Every decision
took its recommended option; D14 adds an explicit console instruction.

How to use it:

- **Try** names a scenario of the in-game Test scenarios list (catalog in the server's
  [TEST_SCENARIOS](https://github.com/jpolaczek/wotr-server/blob/a68ceee8bbc7e9ac7b505add92b197c61ba939f1/docs/TEST_SCENARIOS.md)) where one exists;
  "no scenario yet" means reaching the situation in a game. An opponent's action is watched from the other side (a second
  client in the room, or Replay).
- **Expect** is what must happen; anything that jumps, opens by itself or needs an extra click is a finding. "Skip
  animations" must still show the end state at once.
- Tick a row when it passed in the build that carries its task, and note the build and anything that felt wrong.

## The mechanics behind the decisions

### Sovereigns (Kings) — D1, D2, D5, D16, D19

Five Free Peoples sovereigns: Brand (Dale, resistance 2), Dáin (Erebor, 4), Denethor (Minas Tirith, 3), Théoden
(Edoras, 3), Thranduil (Woodland Realm, 4); with Fate, Brand and Dáin are absent (KINGS pp. 3, 7–13; server rules T28).

- **Initial**: an ordinary Free Peoples Leader in his capital's army, not a Companion. He moves and dies as a Leader;
  his death returns his tiles to the Hunt pool.
- **Corruption attempt (Shadow)**: instead of an action die the Shadow spends an eligible Eye from the Hunt box (the
  Free Peoples have an unused die and the Hunt more than one Eye). It draws up to three Hunt tiles (no more than the Eyes
  left), attaches one face down to the sovereign, reveals and returns the others; a Kings tile ends the draw at once and
  counts zero. Once per sovereign per turn. The Free Peoples see how many tiles he holds, never their values.
- **Corrupted**: when his tiles reach his resistance, all are revealed and return to the pool. He is no figure (he
  neither moves nor dies in battle) and his weakness applies: recruitment bans, a Shadow combat substitution (Brand),
  Helm's Deep as an Isengard Stronghold (Théoden).
- **Politics**: activating or advancing his nation with a proper Companion in his region lets the Free Peoples return
  one random face-down tile.
- **Awakened (Free Peoples)**: with a Muster or Ruler die and his nation at war (Thranduil: the Elves), he becomes a
  Companion with abilities and his Ruler token, all his tiles return, and he may move along his level's route. He cannot
  be corrupted; his death gives the Shadow a Dismay token.
- **Removed**: a Character die and a Companion with his nation's (or the Free Peoples') icon remove a Corrupted
  sovereign and end his weakness.

On screen today: each sovereign has a fixed slot in the Characters panel's Sovereigns section (portrait, status badge,
name, a corruption meter with a pip per point of resistance — filled when known, hatched when hidden — the Shadow's
attempt button and the tiles of a running attempt). On the map an Initial king is a Leader in his army, an Awakened one
a character figure, a Corrupted one a dark marker by his capital, a Removed one nothing. The opponent's changes have no
presentation yet: the portrait jumps inside the collapsed panel (UI-344).

### Brand's combat replacement — D17

Each battle round both sides may play a face-down Combat card; they are revealed together and resolve by initiative.

- **Heir of Bard** (Awakened Brand, Free Peoples): once per battle, if Brand and a North unit take part, after the reveal
  the Free Peoples may give up their own card's effect to cancel the Shadow card's effect, at initiative 0 (the defender
  wins a tie at 0, so a defending Swarm can cancel it).
- **Shadows in the North** (Corrupted Brand, Shadow): the mirror — once per battle, if a North unit takes part, the
  Shadow may give up its card's effect to cancel the Free Peoples card; Brand need not be in the battle (he has no
  figure).
- The Dark Chieftains (Kings, UI-277) have a similar substitution, today a "Keep card effects" button.

### Dismay and the Ruler die — D3, D4, D20

The Shadow gains a Dismay token when the Free Peoples use a Ruler token or when an Awakened sovereign dies. After the
roll a Dismay token lets the Shadow reroll a Free Peoples Ruler die and is removed. Which Dismay token is spent makes no
difference in the rules (`Kings/KingsDismay.cs`).

### Integration rules of the EXP-14 merge — in the build now

- **Gothmog under UI-317**: a Muster die that cannot recruit opens no Reserve, except that it still backs the Reserve
  while it pays Gothmog's "fling into the fray", whose Regular and Elite tokens stand in his Reserve row
  (`MusterActions.BacksReserve`).
- **Lords reactions under UI-291**: The Gates are Closed at a declaration and Spear of Terror are declined with the
  console's Pass, whose tooltip names what it skips; there is no Decline button.

## Checklist

### In the build (EXP-14)

- [ ] **Gothmog with a Muster die that cannot recruit.** Try: `lords-sh-gothmog-muster`. Expect: selecting the Muster
  die lights the Reserve; Gothmog's row ("Gothmog · Minas Morgul") offers a Regular and an Elite; a token's click uses
  the ability at once. A Muster die that can neither recruit nor pay Gothmog lights only Politics.
- [ ] **The Gates and Spear of Terror declined with Pass.** Try: no scenario yet (a Shadow declaration with the Gates on
  the table; an Event's Palantír draw with the Chief). Expect: the console's Pass glows, its tooltip names the Gates or
  Spear of Terror, and its click declines; the Gates' × and the Character deck stay the ways to use them.

### The opponent's expansion actions — UI-344

- [ ] **D1 — An opponent's sovereign change plays on the map.** Try: `kings-fp-awaken`, `kings-sh-corruption`,
  `kings-fp-remove-corrupted`, watched from the other side. Expect: at the sovereign's region — at an awakening his
  Leader in the capital's army gives way to the sovereign's figure, which flares and emerges; a corruption makes the dark marker emerge by the capital; a
  removal fades it out. The Characters panel never opens by itself; the portrait, status and meter match at the release.
- [ ] **D2 — A hidden tile attached.** Try: `kings-sh-corruption`, watched by the Free Peoples. Expect: a tile back
  travels from the Hunt pool to the sovereign's region; a hatched pip appears on his meter; no value is ever shown.
- [ ] **D3 — The opponent's tokens.** Try: `kings-fp-ruler-token`, `lords-council-token`, watched from the other side (UI-344).
  Expect: fixed chips for the opponent's Ruler, Dismay and Council tokens in the top bar's opponent summary; a spent or
  gained token changes there in the presentation, not at the release.
- [ ] **D4 — Dismay rerolls my Ruler die.** Try: `kings-dismay` as the Shadow, watched by the Free Peoples (UI-344).
  Expect: the reroll of my own Ruler die is its own beat of the opponent's presentation, not a silent crossfade at the
  release; the Shadow's Dismay chip in the top bar counts down.
- [ ] **D5 — Tiles revealed at a corruption.** Try: `kings-sh-corruption` until the resistance is reached. Expect: the
  revealed tiles' faces stand briefly as a plate at the sovereign's region, then go to the pool.

### Glow and declines — UI-345, UI-347, UI-276, UI-277, UI-348

- [ ] **D6 — Lit art also pulses.** Try: `lords-special-dice-choice`, `lords-fp-vilya`, `lords-council-token`.
  Expect: the special dice, Keeper Ring sockets, Council medallions, Lore dice and the Spear's deck keep their lit art
  and pulse like every other usable choice; nothing pulses that a click cannot use.
- [ ] **D7 — Optional declines are the console's Pass.** Try: `lords-fp-vilya` (Spend the die), `lords-card-wind-finish`
  (Finish), `lords-card-balrog` (Finish) and `lords-card-orderly-withdrawal` (Keep the hit); the Lords part landed with
  UI-347, Brand's, the Chieftains' and Dismay's with UI-276–UI-277 and UI-348, the kings' Keep tiles with UI-278
  (`kings-card-wisdom`: inspect a king, then Pass). Expect: no "Spend the die", "Keep results", "Finish" (Wind, the Balrog card), "Keep the hit", "No
  Corruption", "Keep card effects", "Keep tokens" or "Keep tiles" button; the console's Pass declines, glows and its
  tooltip names what it skips.
- [ ] **D8 — Forced single steps stay automatic.** Try: `fate-card-book-of-mazarbul`, `fate-card-orcs-multiplying-again`,
  `fate-card-monsters-roused`. Expect: where only one region or plan is legal the server applies it without a click,
  and the console's effect trail and the log name the result. A step with two or more legal choices still waits.
- [ ] **D9 — Every Guide response in the Hunt's response row.** Try: `fate-gimli-guide` (Gimli, Dwarven Lord, as Guide;
  UI-349) and a Hunt with Merry, Pippin or Gollum as Guide (base part released in 0.9.0-rc.14, UI-360). Expect: every Guide's response is a portrait in the Hunt window's
  response row (portrait, name, what it does); none is a text rectangle in the tray. The base Guides' part is a base-game
  change and lands on `main` too.

### Lords — UI-347

- [ ] **D10 — Orderly Withdrawal.** Try: `lords-card-orderly-withdrawal` (UI-347). Expect: a "−1" pip on the hit in the battle window
  (`BattleChoicePip`, as UI-246); its click removes the hit; Pass keeps it.
- [ ] **D11 — A Balrog is Come!** Try: `lords-card-balrog` (UI-347). Expect: the Balrog's region glows; in its panel his figure moves
  him alone and a figure of his army moves it with him (the army's move then starts at once); Pass finishes. No console
  buttons.
- [ ] **D12 — Evil Knowledge.** Try: `lords-evil-knowledge` (UI-347). Expect: the Mouth of Sauron's
  portrait in the battle window's force column glows; its click extends the siege without downgrading an Elite.
- [ ] **D13 — Council tokens.** Try: `lords-council-token`. Expect: a medallion's click starts its printed use at once,
  with no panel step; when a Kings Chieftain can enter with it, his Characters row lights instead and a second click on the
  marked medallion starts its printed use; no "No token" buttons (the Chieftain case needs a game with Lords, the Council and Kings).
- [ ] **D14 — Homelands' keep.** Try: `fate-gimli-homeland` (the Council's homelands with Gimli,
  Dwarven Lord, chosen; UI-349). Expect: a second click on
  the companion already placed keeps him with the Fellowship, and the console's hint says so explicitly (for example
  "Click Merry again to keep him with the Fellowship") whenever that click is available. No button.

### Kings — UI-276, UI-278, UI-348

- [ ] **D15 — Card branches, object first.** Try: `kings-card-wisdom` (Wisdom of Elrond, It Is Not So Dark Here) and
  `kings-card-recruit-or-cleanse` (the Stewards, the Riders, Brand's Men, Dáin's Guard, Thranduil's Archers) (UI-278).
  Expect: a glowing Reserve figure recruits, a glowing king cleanses, nation tokens activate, a king's click otherwise
  informs; the console offers only a branch without an object. Check the interpretation recorded with UI-278: the
  branch's object carries into the card's next step — one click on Gondor's token activates and advances Gondor, one
  click on Dáin shows Dáin's tiles, one click on Thranduil's Elite recruits that Elite, and the Riders' Elite starts the
  plan with it. Say whether one click should do both or each step should wait for its own click.
- [ ] **D16 — Hope and His Power Waxes in battle.** Try: `kings-card-hope` and `kings-card-power-waxes` (UI-278).
  Expect: while the king takes part in a battle he glows in the battle window's force column; otherwise his Characters
  portrait glows (a Corrupted king has no figure).
- [ ] **The Palantír of Barad-dûr (owner's decision 17, 2026-10-19).** Try: `kings-card-palantir` (UI-278). Expect: the
  card glows in the Effects panel; its click, then a glowing Muster die (or the die, then the card), turns the die into
  an Event die for one card or draw this turn; no cost button is offered beside the die.
- [ ] **D17 — Brand's replacement.** Try: `kings-brand-heir` and `kings-brand-shadows` (UI-348), and for the Chieftains
  `kings-chieftain-substitution` (UI-277).
  Expect: after the reveal a gilt chip with Brand's arms stands beside the player's own Combat card, its tooltip naming
  the card it cancels; its click makes the replacement; Pass keeps the card's effect. The same for a Corrupted Brand
  (Shadow) and for the Chieftains (UI-277).
- [ ] **D18 — Far-sighted.** Try: `kings-far-sighted` (UI-276). Expect: Thranduil's ability
  picture glows with an eligible die; using it is Far-sighted, not using it is the standard move; no toggle or mode chip.
- [ ] **D19 — Where the Ruler tokens sit.** Try: `kings-fp-ruler-token`. Expect: the usable token stands in the dice
  pool beside the Ruler die; the king's portrait wears a badge that he holds it.
- [ ] **D20 — Spending Dismay.** Try: `kings-dismay` (UI-276). Expect: the Shadow clicks the Free Peoples Ruler die to reroll;
  the token is picked automatically and named in the tooltip and the log.

### Fate — UI-349

- [ ] **D21 — Monsters Roused, West or East.** Try: `fate-card-monsters-roused`. Expect: two console buttons with the
  shared glow; hovering one only highlights its branch's regions; a click chooses the branch.
- [ ] **D22 — Gimli's political abilities.** Try: `fate-gimli-renown`. Expect: they stay in the region panel and the
  Characters roster (one place per mechanic), not on the Dwarves' Politics row.

### Waiting room — UI-364

- [ ] **Expansion checkboxes.** Try: host a game, then in the waiting room tick and untick Fate, Lords and Kings under
  their medallions. Expect: each click ticks or unticks the box and lights or dims the medallion once the server
  answers; the guest sees the same boxes greyed out.
- [ ] **Ready locks the rules.** Try: as the host, press Ready, then hover a checkbox. Expect: the three checkboxes and
  the six variants are greyed out in place, the tooltip says "Press Not ready to change the rules."; Not ready makes
  them clickable again. The guest's Ready alone does not lock them.
