using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): a click lifts exactly the chosen figures off the table into the
    // hand — the same miniature at the same scale, grown 1.10 and raised 12 px × its scale, its shadow left on the table
    // at .8 and .35 — while the rest of the army stays on its base; a hovered legal target shows the landing mark (green for
    // a move, red for an attack) and the caption above the lifted figure, with no translucent copy anywhere; Esc puts the
    // figures back; Reduced motion shows the raised pose at once.
    public sealed class FigureHandTests
    {
        private const string Region = "minas-tirith";
        private const string Army = "army-minas-tirith-FreePeoples";
        // Runtime classes (MapArmyStand, MapLandingMark, MapHandPiece), read here by name.
        private const string Lifted = "lifted-away";
        private const string MoveMark = "landing-move";
        private const string AttackMark = "landing-attack";
        private const string Returning = "hand-returning";
        // Longer than any of the pose's transitions.
        private const float Transition = .45f;

        [UnityTest]
        public IEnumerator AClickLiftsTheChosenFiguresAndTheRestStaysOnItsBase()
        {
            var fixture = Fixture();
            using var client = Mounted(fixture);
            yield return Armed(client);
            var map = client.Board.Map;
            var source = map.Q<Button>(Army);
            PointerGestureEvents.ClickAt(source);
            yield return Settle();
            var regular = fixture.View.Units.First(u => u.Location == Region && u.Kind == UnitKind.Regular);
            CardNetworkTests.Click(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + regular.Id));
            yield return Settle();
            client.Board.Hover.Close();
            yield return new WaitForSecondsRealtime(Transition);
            LayoutTests.Save(client.Viewport, "ui403-lifted-army.png");
            var piece = map.Q("hand-" + Army);
            Assert.IsNotNull(piece, "The chosen units are in the hand.");
            Assert.AreEqual("2", piece.Q<Label>("army-count").text, "Exactly the chosen figures lift.");
            Assert.AreEqual("1", source.Q<Label>("army-count").text, "The rest of the army stays on its base.");
            Assert.IsFalse(source.ClassListContains(Lifted));
            AssertRaised(piece, source);
            Assert.IsNotNull(map.Q("hand-character-strider"), "Strider, chosen with the army, lifts from his own place.");
            Assert.IsTrue(map.Q<Button>("character-strider").ClassListContains(Lifted), "His stand is empty.");
            Assert.AreEqual(PickingMode.Ignore, map.Q("map-hand").pickingMode);
            Assert.IsTrue(map.IndexOf(map.Q("map-hand")) > map.IndexOf(map.Stands.Layer), "The hand is above the stands.");
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.6f);
            yield return Settle();
            AssertRaised(piece, source);
            LayoutTests.Save(client.Viewport, "ui403-lifted-army-close.png");
        }

        [UnityTest]
        public IEnumerator AHoveredTargetShowsTheLandingMarkAndTheCaptionAboveTheLiftedFigure()
        {
            var fixture = Fixture();
            AddEnemyAndAttack(fixture);
            using var client = Mounted(fixture);
            yield return Armed(client);
            var map = client.Board.Map;
            PointerGestureEvents.ClickAt(map.Q<Button>(Army));
            yield return Settle();
            client.Board.Hover.Close();
            var between = (map.Stands.Stands(Region).Field + map.Stands.Stands("osgiliath").Planned) / 2;
            map.FocusOn(between, map.FitZoom * 2.6f);
            yield return new WaitForSecondsRealtime(Transition);
            map.SetHovered("osgiliath");
            yield return Settle();
            var ring = map.Q("landing-mark");
            var caption = map.Q<Label>("landing-caption");
            Assert.AreEqual(DisplayStyle.Flex, ring.resolvedStyle.display, "A legal target shows the landing mark.");
            Assert.IsTrue(ring.ClassListContains(MoveMark), "Green for a move.");
            Assert.AreEqual(PickingMode.Ignore, ring.pickingMode);
            Assert.AreEqual("Move 4 figures", caption.text);
            AssertOnPlace(map, ring, map.Stands.Stands("osgiliath").Planned, "Beside Osgiliath's own army, on its planned place.");
            var raised = map.Q("hand-" + Army);
            Assert.LessOrEqual(caption.worldBound.yMax, RaisedTop(raised), "The caption hangs above the lifted figure.");
            var lifted = map.Query(className: "figure-hand-piece").ToList().Select(piece => piece.Q("hand-lift").worldBound).ToList();
            var middle = (lifted.Min(box => box.xMin) + lifted.Max(box => box.xMax)) / 2;
            Assert.Less(Mathf.Abs(middle - caption.worldBound.center.x), 16, "Over the lifted army and Strider.");
            AssertNoTranslucentCopy(map);
            LayoutTests.Save(client.Viewport, "ui403-landing-move.png");
            map.FocusOn((map.Stands.Stands(Region).Field + map.Stands.Stands("minas-morgul").Planned) / 2, map.FitZoom * 2f);
            map.SetHovered("minas-morgul");
            yield return Settle();
            Assert.IsTrue(ring.ClassListContains(AttackMark), "Red for an attack.");
            Assert.AreEqual("Attack · Siege", caption.text);
            AssertOnPlace(map, ring, map.Stands.Stands("minas-morgul").Planned, "Beside the defenders.");
            LayoutTests.Save(client.Viewport, "ui403-landing-attack.png");
            map.SetHovered("dale");
            yield return Settle();
            Assert.AreEqual(DisplayStyle.None, ring.resolvedStyle.display, "No mark where the click does nothing.");
            Assert.AreEqual(DisplayStyle.None, caption.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator ACharacterLiftsOnItsOwnAndEscapePutsItBack()
        {
            var fixture = Fixture();
            using var client = Mounted(fixture);
            yield return Armed(client);
            var map = client.Board.Map;
            var strider = map.Q<Button>("character-strider");
            PointerGestureEvents.ClickAt(strider);
            yield return new WaitForSecondsRealtime(Transition);
            LayoutTests.Save(client.Viewport, "ui403-lifted-character.png");
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.6f);
            yield return Settle();
            LayoutTests.Save(client.Viewport, "ui403-lifted-character-close.png");
            var piece = map.Q("hand-character-strider");
            Assert.IsNotNull(piece, "Only Strider lifts.");
            Assert.IsNull(map.Q("hand-" + Army), "His army stays on its base.");
            Assert.IsTrue(strider.ClassListContains(Lifted));
            AssertRaised(piece, strider);
            ((VisualElement)map).Focus();
            BoardMenuLayoutTests.Key(client.Board.Root, true);
            BoardMenuLayoutTests.Key(client.Board.Root, false);
            yield return Settle();
            Assert.IsTrue(piece.ClassListContains(Returning), "Escape drops him back onto his base.");
            yield return new WaitForSecondsRealtime(Transition);
            Assert.IsNull(map.Q("hand-character-strider"), "Then the hand is empty.");
            Assert.IsFalse(strider.ClassListContains(Lifted), "He stands on his place again.");
        }

        // UI-405: a press on the chosen army carries everything the click lifted — its units' piece and Strider's — under
        // the pointer, each the same miniature in the same raised pose; Escape puts both back over their places.
        [UnityTest]
        public IEnumerator UI405TheChosenArmyAndStriderAreCarriedTogetherAsTheirOwnMiniatures()
        {
            var fixture = Fixture();
            using var client = Mounted(fixture);
            yield return Armed(client);
            var map = client.Board.Map;
            var source = map.Q<Button>(Army);
            PointerGestureEvents.ClickAt(source);
            yield return Settle();
            client.Board.Hover.Close();
            yield return new WaitForSecondsRealtime(Transition);
            var army = map.Q("hand-" + Army);
            var strider = map.Q("hand-character-strider");
            var armyAt = army.Q("hand-lift").worldBound.center;
            var striderAt = strider.Q("hand-lift").worldBound.center;
            var press = source.worldBound.center;
            MapContourProbe.LiftFigure(client.Board, source);
            var to = press + new Vector2(150, 70);
            MapContourProbe.Carry(client.Board, to);
            yield return Settle();
            Assert.Less(Vector2.Distance(armyAt + to - press, army.Q("hand-lift").worldBound.center), 1.5f, "The army follows…");
            Assert.Less(Vector2.Distance(striderAt + to - press, strider.Q("hand-lift").worldBound.center), 1.5f, "…with Strider.");
            AssertRaised(army, source);
            AssertRaised(strider, map.Q<Button>("character-strider"));
            AssertNoTranslucentCopy(map);
            LayoutTests.Save(client.Viewport, "ui405-carried-army-and-strider.png");
            ((VisualElement)map).Focus();
            BoardMenuLayoutTests.Key(client.Board.Root, true);
            BoardMenuLayoutTests.Key(client.Board.Root, false);
            MapContourProbe.Release(map, to);
            yield return new WaitForSecondsRealtime(Transition);
            Assert.Less(Vector2.Distance(armyAt, army.Q("hand-lift").worldBound.center), 1.5f, "Escape: the army is back over its place…");
            Assert.Less(Vector2.Distance(striderAt, strider.Q("hand-lift").worldBound.center), 1.5f, "…and Strider over his.");
            Assert.IsTrue(source.ClassListContains("selected-force"), "Still chosen, as before the press.");
            LayoutTests.Save(client.Viewport, "ui405-escape-back.png");
        }

        [UnityTest]
        public IEnumerator ReducedMotionShowsTheStaticRaisedPose()
        {
            var fixture = Fixture();
            using var client = Mounted(fixture);
            client.Interface.Preferences.SetReducedMotion(true);
            yield return Armed(client);
            var map = client.Board.Map;
            var source = map.Q<Button>(Army);
            PointerGestureEvents.ClickAt(source);
            yield return Settle();
            AssertRaised(map.Q("hand-" + Army), source);
            client.Board.Hover.Open(Region);
            ((VisualElement)map).Focus();
            BoardMenuLayoutTests.Key(client.Board.Root, true);
            BoardMenuLayoutTests.Key(client.Board.Root, false);
            yield return Settle();
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "The figures are back at once.");
            Assert.IsFalse(source.ClassListContains(Lifted));
        }

        // The pose: grown 1.10 round the feet and raised 12 design points, the shadow on the table at .8 and .35, the
        // miniature drawn at its source's scale.
        private static void AssertRaised(VisualElement piece, VisualElement source)
        {
            var lift = piece.Q("hand-lift");
            var shadow = piece.Q("hand-shadow");
            Assert.AreEqual(-12, lift.resolvedStyle.translate.y, .01f, "Raised 12 px × its scale.");
            Assert.AreEqual(1.1f, lift.resolvedStyle.scale.value.x, .001f, "Grown 1.10.");
            Assert.AreEqual(0, shadow.resolvedStyle.translate.y, .01f, "The shadow stays on the table.");
            Assert.AreEqual(.8f, shadow.resolvedStyle.scale.value.x, .001f);
            Assert.AreEqual(.35f, shadow.resolvedStyle.opacity, .001f);
            Assert.AreEqual(1, lift.resolvedStyle.opacity, .001f, "The lifted figure is opaque.");
            var rim = piece.Q("army-rim") ?? piece.Q("character-rim");
            Assert.AreEqual(DisplayStyle.Flex, rim.resolvedStyle.display, "The chosen figures wear the gold rim.");
            Assert.AreEqual(Visibility.Hidden, (piece.Q("army-contact-shadow") ?? piece.Q("character-contact-shadow")).resolvedStyle.visibility,
                "The miniature's own shadow does not rise with it.");
            var drawn = piece.parent.worldBound.width / piece.parent.resolvedStyle.width;
            var table = source.parent.worldBound.width / source.parent.resolvedStyle.width;
            Assert.AreEqual(table, drawn, .002f, "The same miniature at the same scale as on the table.");
        }

        // The top of a lifted army's figures (map-world), under which the caption must not reach.
        internal static float RaisedTop(VisualElement piece) => piece.Query<Image>(className: "army-silhouette").ToList()
            .Where(image => !image.ClassListContains(MapArmyArtwork.UnusedClass)).Min(image => image.worldBound.yMin);

        internal static void AssertOnPlace(MapCanvas map, VisualElement ring, Vector2 place, string message)
        {
            var point = map.LocalToWorld(map.Camera.ToScreen(place));
            Assert.AreEqual(point.x, ring.worldBound.center.x, 1.5f, message);
            Assert.AreEqual(point.y, ring.worldBound.center.y, 4, message);
        }

        // UI-403: nothing on the map is drawn faintly for the move: no army ghost and every shown army opaque.
        private static void AssertNoTranslucentCopy(MapCanvas map)
        {
            Assert.IsNull(map.Q("army-ghost"));
            Assert.IsEmpty(map.Query(className: MapPlannedRecruits.GhostClass).ToList());
            foreach (var army in map.Query<Button>(className: "army").ToList().Where(b => b.resolvedStyle.display == DisplayStyle.Flex))
                Assert.AreEqual(1, army.resolvedStyle.opacity, .001f, army.name);
        }

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture();
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = Region, Level = 3,
                Leadership = 1
            });
            fixture.Refresh();
            return fixture;
        }

        // A Shadow army in Minas Morgul the selection may attack (a fixture's target; the server decides the real ones).
        private static void AddEnemyAndAttack(BoardFixture fixture)
        {
            foreach (var unit in fixture.View.Units.Where(u => u.Nation == "sauron" && u.Location == "reserve").Take(2))
                unit.Location = "minas-morgul";
            var movement = fixture.View.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var army = movement.Armies.First(a => a.RegionId == Region);
            fixture.View.Options.Add(new ActionOption
            {
                Kind = CommandKind.AttackArmy,
                EligibleDice = movement.EligibleDice.ToList(),
                MovementCosts = movement.MovementCosts.ToList(),
                Armies =
                {
                    new ArmyOption
                    {
                        RegionId = Region, UnitIds = army.UnitIds.ToList(), CharacterIds = army.CharacterIds.ToList(),
                        CommanderIds = army.CommanderIds.ToList(),
                        Targets =
                        {
                            new ArmyTargetOption
                            {
                                RegionId = "minas-morgul", BattleMode = BattleMode.Siege,
                                EligibleIds = army.UnitIds.Concat(army.CharacterIds).ToList()
                            }
                        }
                    }
                }
            });
        }

        private static MountedMatchClient Mounted(BoardFixture fixture)
        {
            var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            return client;
        }

        // The Army die chosen and Minas Tirith in view at the figures' level.
        private static IEnumerator Armed(MountedMatchClient client)
        {
            yield return Settle();
            client.Board.Hover.Close();
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("army"));
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            map.FocusOn(map.Stands.Stands(Region).Field, map.FitZoom * 2.2f);
            yield return Settle();
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
