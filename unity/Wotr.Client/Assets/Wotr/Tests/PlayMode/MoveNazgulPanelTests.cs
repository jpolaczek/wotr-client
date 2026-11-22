using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-567 (owner, 2026-11-22: "a Move Nazgûl button in the army window, active when the Shadow has the Character die
    // selected"): a Shadow force with Nazgûl and the Witch-king has Move Nazgûl beside its Select all; it is dark before a
    // Character die, lit with one, and its click selects the force's flyers (not its Orcs) and lights their destinations.
    // The Free Peoples never see it. A board without a server: the click builds the draft the die's flight sends.
    // Frame: ui567-move-nazgul.
    public sealed class MoveNazgulPanelTests
    {
        private const string Region = "gorgoroth";
        private const string Die = "ui567-character";
        private const string Orc = "ui567-orc";
        private const string Nazgul = "ui567-nazgul";
        private const string WitchKing = "base.character.witch-king";

        [UnityTest]
        public IEnumerator MoveNazgulIsDarkWithoutTheCharacterDieAndSelectsTheFlyersWithIt()
        {
            var view = Shadow(new BoardFixture(true).View);
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            var button = MoveNazgul(client);
            Assert.IsNotNull(button, "The force with flyers has Move Nazgûl.");
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, button.resolvedStyle.display);
            Assert.IsFalse(button.enabledSelf, "Dark before a Character die.");
            StringAssert.Contains("Choose a Character die first", button.tooltip);
            ArmyGestures.Die(client, Die, DieFace.Character);
            yield return Settle();
            button = MoveNazgul(client);
            yield return Settle();
            Assert.IsTrue(button.enabledSelf, "Lit with the Character die.");
            var needed = button.MeasureTextSize(button.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            Assert.GreaterOrEqual(button.contentRect.width + 1, needed, "Its name is not cut.");
            PointerGestureEvents.ClickAt(button);
            yield return Settle();
            client.Board.Hover.Open(Region);
            yield return Settle();
            var panel = client.Board.Hover.Panel.Root;
            Assert.IsTrue(Selected(panel, Nazgul) && Selected(panel, WitchKing), "The force's flyers are chosen.");
            Assert.IsFalse(Selected(panel, Orc), "Its Orcs stay behind.");
            Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains("osgiliath"), "Their destinations are lit.");
            LayoutTests.Save(client.Viewport, "ui567-move-nazgul.png");

            var free = new BoardFixture(true).View;
            free.Units.AddRange(view.Units.Where(u => u.Id.StartsWith("ui567-")));
            free.Characters.RemoveAll(c => c.Id == WitchKing);
            free.Characters.Add(view.Characters.Single(c => c.Id == WitchKing));
            using var theirs = new MountedMatchClient("http://localhost", free.MatchId);
            theirs.Apply(free);
            yield return Settle();
            var hidden = MoveNazgul(theirs);
            yield return Settle();
            Assert.IsTrue(hidden == null || hidden.resolvedStyle.display == DisplayStyle.None, "The Free Peoples never see it.");
        }

        private static Button MoveNazgul(MountedMatchClient client)
        {
            client.Board.Hover.Open(Region);
            return client.Board.Hover.Panel.Root.Query<Button>(className: "hover-move-nazgul").ToList().FirstOrDefault();
        }

        private static bool Selected(VisualElement panel, string id) =>
            panel.Q<Button>("hover-figure-" + id)?.ClassListContains("selected") == true;

        // The Shadow to act with a Character die; Gorgoroth holds an Orc, a Nazgûl and the Witch-king, who may fly to Nurn
        // or Osgiliath.
        private static PlayerView Shadow(PlayerView view)
        {
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Decision = new DecisionView { Id = "ui567-action", Kind = DecisionKind.Action };
            view.Options.Clear();
            view.Dice.Add(new DieView { Id = Die, Owner = Side.Shadow, Face = DieFace.Character, Source = "standard", Location = DieLocation.Available });
            view.Units.RemoveAll(u => u.Location == Region);
            view.Characters.RemoveAll(c => c.Id == WitchKing);
            view.Units.Add(new UnitView { Id = Orc, Nation = "sauron", Kind = UnitKind.Regular, Location = Region, Side = Side.Shadow });
            view.Units.Add(new UnitView { Id = Nazgul, Nation = "sauron", Kind = UnitKind.Nazgul, Location = Region, Side = Side.Shadow });
            view.Characters.Add(new CharacterView
            {
                Id = WitchKing, DefinitionId = WitchKing, Location = Region, Side = Side.Shadow, Level = 5, Leadership = 2
            });
            var travel = new ActionOption
            {
                Kind = CommandKind.MoveShadowCharacters, Enabled = true, EligibleDice = { Die }, EligibleIds = { Nazgul, WitchKing }
            };
            foreach (var id in new[] { Nazgul, WitchKing })
                travel.Travel.Add(new TravelOption { Id = id, Origin = Region, Flight = true, Targets = { "nurn", "osgiliath" } });
            view.Options.Add(travel);
            return view;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
