

<a id="effects-on-their-objects--ui-367-2026-10-27"></a>
## Effects on their objects — UI-367 (2026-10-27)

Owner request: the Renown pattern (UI-366) applies more widely — a die (or token) that can pay for an effect lights
the tab or panel where the effect's object is, the object glows with a tooltip naming the source and the effect, and
its click uses it; the result plays on the object. Second rule (UI-369): an informational element of something usable
glows, its first click selects it, and only then does Click to use appear. Read-only audit of client `expansions`
e31ff7b5, server `expansions` and `origin/main`; each accepted candidate becomes its own task.

Today every character ability takes one path: the die lights the Characters tab (`DieOptions`), the owner's region
glows, then the region panel's ability picture or the Characters inspector's button uses it at once.

| # | Effect | Line | Natural object | The die lights | Priority |
| --- | --- | --- | --- | --- | --- |
| C1 | Gimli (Erebor), Boromir (Gondor City/Stronghold), Legolas (Elven Stronghold): one political step | main | that nation's track | Politics; only that nation | high (UI-366's gesture) |
| C2 | Council Messengers (Boromir → Gondor, Gimli → Dwarves, Merry/Pippin → North): activate and advance | expansions | that nation's track (it glows even when only activation is left) | Politics; that nation | high |
| C3 | Council Legolas, Prince of Mirkwood: Elves one step | expansions | Elves track | Politics; Elves | high |
| C4 | Fate Gimli: Dwarf of Renown, and the Dwarven Lord's Messenger of Elrond (Iron Hills / Ered Luin) | expansions | Dwarves track | Politics; Dwarves | high (UI-366) |
| C5 | Saruman's Voice: recruit in each free Isengard settlement / two Elites in Orthanc | main | a "Saruman · Orthanc" row in the Reserve | Reserve | high |
| C6 | Mouth of Sauron: a Muster die as an Army die once per turn | main | the Shadow armies on the map | the map's armies | medium (an explicit choice; UX_SPEC forbids automatic abilities) |
| C7 | Great Sorcery (Lords Mouth): a Muster die as a Character die for his army | expansions | the Mouth's army | the map: that army | medium |
| C8 | Use Balrog: move him alone, or move/attack with his army | expansions | the Balrog figure / his army's figures | his region's panel | medium |
| C9 | Dwarvenking, Stewardship, Elvenking: a Regular becomes an Elite | expansions | the Elite in the Reserve ("Dáin · Erebor" row, as Gothmog's) | Reserve | medium |
| C10 | Ruler recruitments (Call to Arms, Iron Guard, Mustering of the Mark) | expansions | the king's nation row in the Reserve | Reserve | medium |
| C11 | Far-sighted (Thranduil) | expansions | the Fellowship's move | Fellowship | low (conflicts with D18: needs a decision) |
| C12 | The Black Captain Commands: recruit two Nazgûl / move Nazgûl | main | the Reserve's Nazgûl / the Nazgûl on the map | Reserve / map | medium |
| C13 | Scouts: withdraw into the siege | main | the Stronghold region | the map | low |
| C14 | Lure of the Ring: Corruption or eliminate | main | the lured Companion's portrait | Fellowship | low |
| C15 | The Nazgûl Strike!: roll for the Hunt | main | the Hunt Box | Hunt | low |
| C16 | Gandalf the Grey as Guide: draw from the matching deck | main | the deck (as Spear of Terror) | the deck | low |

Click to use (UI-369) elsewhere: the region panel's ability pictures use at their first click and duplicate the
object (remove them where C1–C11 move the use onto the object, otherwise select first); the king's hover inspector
lists abilities as text and never glows (give it the same chips); Narya's ring socket never glows and its Activate ✓
stands beside the token (fold it into the nation adapter); the Ruler coins select first but their use's object must
glow after the selection (C10–C11); the Fellowship tray still builds Standard/Far-sighted buttons (D18 says none).
These stay as they are by earlier decisions: Gothmog's Reserve tokens, Council medallions (D13), nation tokens and
arrows, table cards, the kings' portraits and tiles, figures with an ability, Ring sockets, Hunt responses, battle
portraits and chips, decks and dice (a first click completes, UI-142), hand cards (already select-first).

Shared building blocks proposed: one nation-effect adapter (Muster step, Council token, Narya, C1–C4, the Kings nation
branches, base card nation targets — one priority rule, one tooltip, open-on-commit and the token's move), built on
`main` first; one ability-place table (ability → nation track, Reserve row, figure, army, Fellowship or deck) that
drives `DieOptions`, the components and the inspector; a Reserve ability row generalising Gothmog's (C5, C9, C10); an
army ability cost on the map (C6–C8). Inconsistencies found: the Characters tab lights for abilities whose objects are
elsewhere; the Reserve tab does not light for a die that only pays Gothmog; `DieHints` says only "Character
abilities"; political options target a region while Muster and cards target nations; a selected Ruler token does not
feed `DieOptions`; three deck-draw abilities sit on three different objects.
