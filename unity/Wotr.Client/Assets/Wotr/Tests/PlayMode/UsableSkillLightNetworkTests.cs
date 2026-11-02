using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-441 (owner, 2026-11-02, with a screenshot of Gothmog's lit Fling Them into the Fray beside his unlit row): "make
    // sure the character always lights up whenever its skill can be used at that moment". On real seeds, the character's
    // Characters row, its map figure (its miniature, or the force an Initial king leads), the Characters tab, its window's
    // header and its portraits glow exactly while its skill icon does — or, for a reaction (its icon read only), while the
    // reaction is offered — and go out with it (GlowingTargetChecks). UI-443 (owner): before a die or token is chosen no
    // skill that needs one lights, even when one die alone could pay. Each test plays its own seed once.
    public sealed class UsableSkillLightNetworkTests
    {
        [UnityTest] public IEnumerator GothmogsFlingThemIntoTheFrayLightsHimWhileItsIconGlows() => Wait(Gothmog());
        [UnityTest] public IEnumerator SarumansVoiceLightsHimWhileItsIconGlows() => Wait(Saruman());
        [UnityTest] public IEnumerator GimlisPoliticalAbilityLightsHimWhileItsIconGlows() => Wait(Gimli());
        [UnityTest] public IEnumerator TheodensMusteringOfTheMarkLightsHimWhileItsIconGlows() => Wait(Theoden());
        [UnityTest] public IEnumerator GandalfTheGreysDrawLightsHimWhileItIsOffered() => Wait(GreyDraw());
        [UnityTest] public IEnumerator TheWhiteRiderLightsGandalfWhileItIsOffered() => Wait(WhiteRider());

        private const string Usable = "Usable now: ";

        // One character under test: its Characters row's id, its window's skill icon, the screenshots' name.
        private sealed class Case
        {
            public MatchTestRoom Room;
            public MountedMatchClient Client;
            public string Figure;
            public string Icon;
            public string Shots;
            public string Skill;
            // A reaction's icon is read only (UI-440): the character's light follows the offer.
            public bool Reaction;
            // The map figure may be checked uncovered (no battle window over the map).
            public bool MapInView = true;
            // The step's lit places are pictured (ui441-<shots>-<place>.png), each in the pulse's bright phase.
            public bool Capture;
        }

        private static async Task Gothmog()
        {
            using var room = await Connect("t34-gothmog-upgrade", ScenarioKind.CardPack, openPanels: false);
            var choice = Choice(room.Sh, "gothmog-upgrade");
            var c = Character(room, room.Sh, choice, "character-ability-fling-them-into-the-fray-gothmog-upgrade", "gothmog",
                "Fling Them into the Fray");
            await AbilityByDie(c, choice.Dice[0], () => Layout(c, "minas-morgul"));
        }

        // The owner's case pictured whole at 1920×1080 at 125 % under Reduced motion (every light bright and still): his
        // lit row with its icons, his window with the lit header and Fling's two variants, his miniature in Minas Morgul.
        private static async Task Layout(Case c, string region)
        {
            var board = c.Client.Board;
            using var viewport = new BoardCompletionLayoutTests.LayoutViewport(c.Client, 1920, 1.25f);
            await c.Room.Settle();
            await Task.Delay(300);
            board.Characters.Open();
            await c.Room.Settle();
            await Task.Delay(300);
            LayoutTests.Save(viewport.Texture, "ui441-" + c.Shots + "-row-1920-1.25.png");
            board.Characters.Show(c.Figure, true);
            await c.Room.Settle();
            await Task.Delay(300);
            Assert.IsTrue(board.Characters.Inspector.HeaderLit, "His window's header glows.");
            LayoutTests.Save(viewport.Texture, "ui441-" + c.Shots + "-window-1920-1.25.png");
            await CloseCharacters(c);
            board.Map.Reveal(new[] { region });
            await c.Room.Settle();
            await Task.Delay(300);
            LayoutTests.Save(viewport.Texture, "ui441-" + c.Shots + "-map-1920-1.25.png");
        }

        private static async Task Saruman()
        {
            using var room = await Connect("ability-voice-recruit-full", ScenarioKind.BaseAcceptance, openPanels: false);
            var choice = Choice(room.Sh, "voice-recruit");
            var c = Character(room, room.Sh, choice, "character-ability-muster-each-isengard-settlement-or-upgrade-two-voice-recruit",
                "saruman", "The Voice of Saruman");
            await AbilityByDie(c, choice.Dice[0]);
        }

        private static async Task Gimli()
        {
            using var room = await Connect("ability-gimli", ScenarioKind.BaseAcceptance, openPanels: false);
            var choice = Choice(room.Fp, "politics-dwarves-at-erebor");
            var c = Character(room, room.Fp, choice, "character-ability-politics-dwarves-at-erebor", "gimli", "Dwarf of Erebor");
            await AbilityByDie(c, choice.Dice[0]);
        }

        // A king's skill after his Ruler token: his row, his portrait among the Sovereigns, his figure (or the force he leads).
        private static async Task Theoden()
        {
            using var room = await Connect("ui67-ruler", ScenarioKind.Interaction, openPanels: false);
            var client = room.Fp;
            var c = new Case
            {
                Room = room, Client = client, Figure = "kings.sovereign.theoden", Icon = "character-ability-mustering-of-the-mark",
                Shots = "theoden", Skill = "Mustering of the Mark"
            };
            await AssertLights(c, false, "before the token");
            var token = client.Board.OwnDice.Q<Button>("ruler-token-kings.token.theoden");
            PointerGestureEvents.ClickAt(token);
            await room.Settle();
            await Pictured(c, () => AssertLights(c, true, "after his Ruler token"));
            PointerGestureEvents.ClickAt(token);
            await room.Settle();
            await AssertLights(c, false, "after the token's release");
        }

        // A reaction on the Guide's portrait (UI-247): Gandalf in the Fellowship has no map figure; his Fellowship portrait,
        // his row and the tab glow while the draw is offered, and go out once it is used.
        private static async Task GreyDraw()
        {
            using var room = await Connect("grey", ScenarioKind.Character, openPanels: false);
            var c = Reaction(room, room.Fp, ReactionDecks.GreyDraw, "grey-draw");
            await Pictured(c, () => AssertLights(c, true, "while the draw is offered"));
            var revision = room.Fp.Board.View.Revision;
            await ReactionGestures.Use(room, room.Fp, ReactionDecks.GreyDraw);
            await room.At(revision + 1);
            await room.Settle();
            await AssertConsistent(c, "after the draw");
        }

        // A reaction on a battle portrait (UI-247): Gandalf the White's miniature glows under the battle window too.
        private static async Task WhiteRider()
        {
            using var room = await Connect("white-rider", ScenarioKind.Character, openPanels: false);
            var c = Reaction(room, room.Fp, "character.white-rider", "white-rider");
            c.MapInView = false;
            await Pictured(c, () => AssertLights(c, true, "while the White Rider is offered"));
            var revision = room.Fp.Board.View.Revision;
            await ReactionGestures.Use(room, room.Fp, "character.white-rider");
            await room.At(revision + 1);
            await room.Settle();
            await AssertConsistent(c, "after the White Rider");
        }

        // The skill's die lights the character with its icon; a die that pays none of its skills puts both out; before a
        // die and once it is released nothing is lit (UI-443).
        private static async Task AbilityByDie(Case c, string die, System.Func<Task> whileLit = null)
        {
            await AssertLights(c, false, "before a die");
            await ComponentGestures.Cost(c.Room, c.Client, die);
            await Pictured(c, () => AssertLights(c, true, "with its die"));
            if (whileLit != null)
                await whileLit();
            var other = OtherDie(c);
            if (other.Length > 0)
            {
                await ComponentGestures.Cost(c.Room, c.Client, other);
                await AssertLights(c, false, "with a die that pays none of its skills");
                T22NetworkActions.Die(c.Client, other);
            }
            else
                T22NetworkActions.Die(c.Client, die);
            await c.Room.Settle();
            await AssertLights(c, false, "with no die chosen");
        }

        // Whatever the icon shows now, the character agrees.
        private static async Task AssertConsistent(Case c, string step)
        {
            var (icon, _) = await Window(c, step);
            await AssertLights(c, c.Reaction ? ReactionOffered(c) : icon, step);
        }

        private static async Task AssertLights(Case c, bool lit, string step)
        {
            var label = c.Shots + " " + step;
            var (icon, header) = await Window(c, step);
            if (!c.Reaction)
                Assert.AreEqual(lit, icon, label + ": the skill icon");
            Assert.AreEqual(lit, header, label + ": the window's header");
            await AssertTab(c, lit, label);
            await AssertMap(c, lit, label);
            await AssertRow(c, lit, label);
            await AssertFellowship(c, lit, label);
        }

        // The window's skill icon and its header, read in the open window, which then closes.
        private static async Task<(bool Icon, bool Header)> Window(Case c, string step)
        {
            var board = c.Client.Board;
            board.Characters.Show(c.Figure, true);
            await c.Room.Settle();
            var inspector = board.Characters.Inspector;
            Assert.AreEqual(c.Figure, inspector.FigureId, step);
            // A companion in the Fellowship opens on his next version (UI-438); his skills are the version in play's.
            var current = board.View.OwnCharacterRoster.Single(row => row.FigureId == c.Figure).CurrentDefinitionId;
            if (inspector.DefinitionId != current)
            {
                Assert.IsFalse(inspector.HeaderLit, step + ": another version's card does not light the header.");
                inspector.SelectVersion(current);
                await c.Room.Settle();
            }
            var icon = inspector.Root.Q<Button>(c.Icon);
            Assert.IsNotNull(icon, c.Icon + " in " + string.Join(", ", inspector.Abilities.Icons.Select(b => b.name)));
            var lit = icon.ClassListContains(ChoiceGlow.ClassName);
            var header = inspector.HeaderLit;
            var glow = inspector.Root.Q(CharacterInspector.HeaderGlowClass);
            Assert.AreEqual(header, glow.resolvedStyle.display == DisplayStyle.Flex, step + ": the header's light shows exactly while lit.");
            if (header)
            {
                Assert.IsTrue(await GlowingTargetChecks.Pulses(glow), step + ": the header breathes with the window.");
                await Save(c, glow, "window");
            }
            await CloseCharacters(c);
            return (lit, header);
        }

        private static async Task Pictured(Case c, System.Func<Task> step)
        {
            c.Capture = true;
            try
            {
                await step();
            }
            finally
            {
                c.Capture = false;
            }
        }

        // The screen while the element's light is at its brightest: just before its host's pulse dims it again.
        private static async Task Save(Case c, VisualElement element, string place)
        {
            if (!c.Capture)
                return;
            bool Dim() => Hosts(element).Any(host => host.ClassListContains(ChoiceGlow.PulseClass) || host.ClassListContains(BoardEdgeLayout.TabPulseClass));
            for (var waited = 0; waited < 1600 && !Dim(); waited += 25)
                await Task.Delay(25);
            for (var waited = 0; waited < 1600 && Dim(); waited += 25)
                await Task.Delay(25);
            await Task.Delay(620);
            LayoutTests.Save(c.Client.Viewport, "ui441-" + c.Shots + "-" + place + ".png");
        }

        private static System.Collections.Generic.IEnumerable<VisualElement> Hosts(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                yield return current;
        }

        // The panel and the window close; the panel's fade ends before anything under it is clicked.
        private static async Task CloseCharacters(Case c)
        {
            var board = c.Client.Board;
            board.Characters.Close();
            await c.Room.Settle();
            await c.Room.Until(() => board.Characters.Root.resolvedStyle.display == DisplayStyle.None);
        }

        // The rolled-up panel's tab glows while the panel holds a light, this character's among them.
        private static async Task AssertTab(Case c, bool lit, string label)
        {
            var board = c.Client.Board;
            var tab = board.Characters.Tab;
            Assert.AreEqual(PanelLit(board), tab.ClassListContains("legal-target"), label + ": the tab glows exactly while the panel holds a light.");
            if (!lit)
                return;
            await GlowingTargetChecks.AssertTabLit(board, tab, label + " Characters tab");
            await Save(c, tab, "tab");
        }

        private static async Task AssertRow(Case c, bool lit, string label)
        {
            var board = c.Client.Board;
            board.Characters.Open();
            await c.Room.Settle();
            var row = board.Characters.Root.Q<Button>("character-roster-" + c.Figure);
            Assert.IsNotNull(row, label + ": the row");
            Assert.AreEqual(lit, row.ClassListContains(ChoiceGlow.ClassName), label + ": the Characters row");
            var plain = CharacterRowTooltip.Plain(row.tooltip);
            Assert.AreEqual(lit, plain.StartsWith(Usable), label + ": the row says why: " + plain);
            if (lit)
            {
                StringAssert.StartsWith(Usable + c.Skill + " · ", plain);
                await GlowingTargetChecks.AssertVisiblyOffered(board, row, label + " row");
                await Save(c, row, "row");
            }
            var king = board.Characters.Root.Q<Button>("sovereign-" + c.Figure);
            if (king != null)
            {
                Assert.AreEqual(lit, king.tooltip.StartsWith(Usable), label + ": the king's portrait says why: " + king.tooltip);
                if (lit)
                    await GlowingTargetChecks.AssertVisiblyOffered(board, king, label + " king's portrait");
            }
            await CloseCharacters(c);
        }

        // The miniature (or the force an Initial king leads) wears the map's choice glow and says why.
        private static async Task AssertMap(Case c, bool lit, string label)
        {
            var board = c.Client.Board;
            var (figure, region) = MapFigure(c);
            if (figure == null)
                return;
            Assert.AreEqual(lit, figure.ClassListContains("ability-owner"), label + ": the map figure " + figure.name);
            Assert.AreEqual(lit, figure.tooltip.Contains(Usable + c.Skill), label + ": the map figure says why: " + figure.tooltip);
            if (!lit)
                return;
            Assert.IsNotEmpty(GlowingTargetChecks.Glow(figure), label + ": the map figure's glow shows.");
            Assert.IsTrue(await GlowingTargetChecks.Pulses(figure), label + ": the map figure breathes.");
            if (!c.MapInView)
                return;
            board.Map.Reveal(new[] { region });
            await c.Room.Settle();
            GlowingTargetChecks.AssertShownUncovered(board, figure, label + " map figure");
            await Save(c, figure, "map");
        }

        // A companion's Fellowship portrait glows with it.
        private static async Task AssertFellowship(Case c, bool lit, string label)
        {
            var board = c.Client.Board;
            var portrait = board.Fellowship.PortraitButton(c.Figure);
            if (portrait == null || c.Client.Board.View.OwnCompanions.All(companion => companion.Id != c.Figure))
                return;
            Assert.AreEqual(lit, portrait.ClassListContains(ChoiceGlow.ClassName), label + ": the Fellowship portrait");
            if (!lit)
                return;
            board.Fellowship.Open();
            await c.Room.Settle();
            await GlowingTargetChecks.AssertVisiblyOffered(board, portrait, label + " Fellowship portrait");
            await Save(c, portrait, "fellowship");
        }

        // The character's own miniature on the map, else the force an Initial king leads; (null, "") off the map.
        private static (Button Figure, string Region) MapFigure(Case c)
        {
            var view = c.Client.Board.View;
            var map = c.Client.Board.Map;
            var character = view.Characters.FirstOrDefault(item => CharacterRosterActions.Figure(view, item.Id) == c.Figure);
            if (character != null && map.Q<Button>("character-" + character.Id) is { } miniature)
                return (miniature, character.Location);
            var king = view.Sovereigns.FirstOrDefault(item => item.Id == c.Figure);
            if (king == null || king.RegionId.Length == 0)
                return (null, "");
            return (map.Q<Button>("army-" + king.RegionId + "-" + Side.FreePeoples), king.RegionId);
        }

        private static bool PanelLit(BoardScreen board) => board.Characters.Root.Query<VisualElement>().ToList()
            .Any(element => element.ClassListContains(ChoiceGlow.ClassName) || element.ClassListContains("legal-target"));

        private static bool ReactionOffered(Case c) => UsableSkills.Characters(c.Client.Board.View, new TurnDraft(),
            Enumerable.Empty<ComponentAction>(), true).ContainsKey(c.Figure);

        // A die of the player's that pays none of the character's skills, or "".
        private static string OtherDie(Case c)
        {
            var view = c.Client.Board.View;
            var paying = view.Options.SelectMany(o => o.CharacterActions)
                .Where(a => CharacterRosterActions.Figure(view, a.CharacterId) == c.Figure).SelectMany(a => a.Dice).ToHashSet();
            return view.Dice.Where(d => d.Owner == view.You && d.Location == DieLocation.Available && !paying.Contains(d.Id) &&
                c.Client.Board.OwnDice.Q<Button>(d.Id) != null).Select(d => d.Id).FirstOrDefault() ?? "";
        }

        private static CharacterActionOption Choice(MountedMatchClient client, string ability) => client.Board.View.Options
            .Where(o => o.Kind == CommandKind.UseCharacterAbility).SelectMany(o => o.CharacterActions).First(a => a.AbilityId == ability);

        private static Case Character(MatchTestRoom room, MountedMatchClient client, CharacterActionOption choice, string icon,
            string shots, string skill) => new Case
        {
            Room = room, Client = client, Figure = CharacterRosterActions.Figure(client.Board.View, choice.CharacterId), Icon = icon,
            Shots = shots, Skill = skill
        };

        private static Case Reaction(MatchTestRoom room, MountedMatchClient client, string reaction, string shots)
        {
            var view = client.Board.View;
            var choice = view.Options.Where(o => o.Kind == CommandKind.RespondEffect).SelectMany(o => o.EffectChoices).First(e => e.Id == reaction);
            var figure = CharacterRosterActions.Figure(view, choice.SourceId);
            Assert.IsNotEmpty(figure, reaction + "'s source " + choice.SourceId + " is a character of the player's.");
            var definition = view.OwnCharacterRoster.Single(row => row.FigureId == figure).CurrentDefinitionId;
            var ability = AbilityArt.Printed(reaction);
            return new Case
            {
                Room = room, Client = client, Figure = figure, Icon = "character-ability-" + ability, Shots = shots, Reaction = true,
                Skill = CharacterAbilityLibrary.Find(definition, ability).Title
            };
        }
    }
}
