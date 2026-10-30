using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): a click lifts exactly the chosen figures into the hand — the units
    // on a base of their own, each character on its stand — and their places show only what stays behind; a figure left
    // behind, Esc or undo puts them back, an attack's army comes back to its base, a sent march stays lifted until the view
    // takes it, and a planned flight's figure leaves its place for the ghost. Outside a panel nothing animates, so every
    // change shows at once.
    public sealed class MapFigureHandTests
    {
        private const string Region = "minas-tirith";
        private const string Army = "army-minas-tirith-FreePeoples";
        // Runtime classes (MapArmyStand, MapHandPiece), read here by name.
        private const string Lifted = "lifted-away";
        private const string Raised = "figure-lifted";

        [Test]
        public void AClickLiftsExactlyTheSelectionAndItsPlaceShowsWhatStaysBehind()
        {
            var (view, map, context) = Board();
            var draft = new TurnDraft();
            Assert.IsTrue(ContextArmySelection.SelectAll(draft, view, context, Region, false));
            map.DraftOverlay.Apply(view, draft, context);
            var source = map.Q<Button>(Army);
            var army = map.Q("hand-" + Army);
            Assert.IsNotNull(army, "The army's units are in the hand.");
            Assert.AreEqual("3", army.Q<Label>("army-count").text);
            Assert.AreEqual(PickingMode.Ignore, army.Q<Button>().pickingMode, "The hand never takes the pointer.");
            Assert.IsTrue(army.ClassListContains(Raised), "Outside a panel the raised pose shows at once.");
            Assert.IsTrue(source.ClassListContains(Lifted), "Every unit is lifted: the base disappears.");
            Assert.IsNotNull(map.Q("hand-character-strider"), "Strider lifts on his own stand.");
            Assert.IsTrue(map.Q<Button>("character-strider").ClassListContains(Lifted));

            var regular = view.Units.First(u => u.Location == Region && u.Kind == UnitKind.Regular);
            Assert.IsTrue(ContextArmySelection.Select(draft, view, context, regular.Id));
            map.DraftOverlay.Apply(view, draft, context);
            Assert.AreEqual("2", army.Q<Label>("army-count").text, "The hand holds what is still chosen.");
            Assert.IsFalse(source.ClassListContains(Lifted), "A figure left behind stands on the base again.");
            Assert.AreEqual("1", source.Q<Label>("army-count").text, "The base shows only what stays behind.");
            Assert.AreEqual(1, map.Query(className: "figure-hand-piece").ToList().Count(p => p.name == "hand-" + Army),
                "The figure left behind is back on its base, not in the hand.");

            map.DraftOverlay.Apply(view, new TurnDraft(), context);
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "Esc or undo: nothing is held.");
            Assert.AreEqual("3", source.Q<Label>("army-count").text);
            Assert.IsFalse(map.Q<Button>("character-strider").ClassListContains(Lifted));
        }

        [Test]
        public void ASentMarchStaysLiftedUntilTheViewTakesItAndAnAttackComesBackToItsBase()
        {
            var (view, map, context) = Board(attack: true);
            var draft = new TurnDraft();
            ContextArmySelection.SelectAll(draft, view, context, Region, false);
            Assert.IsTrue(ContextArmySelection.Target(draft, view, context, "osgiliath"));
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsNotNull(map.Q("hand-" + Army), "On its way: it stays in the hand while the step is sent.");
            foreach (var unit in view.Units.Where(u => u.Location == Region))
                unit.Location = "osgiliath";
            foreach (var character in view.Characters)
                character.Location = "osgiliath";
            map.Data.Apply(view);
            map.UpdateData(map.Data);
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "The view took it away: the flight shows it.");
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "Nothing lifts where the figures arrived.");

            (view, map, context) = Board(attack: true);
            draft = new TurnDraft();
            ContextArmySelection.SelectAll(draft, view, context, Region, false);
            Assert.IsTrue(ContextArmySelection.Target(draft, view, context, "minas-morgul"));
            Assert.AreEqual(CommandKind.AttackArmy, draft.Kind);
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsEmpty(map.Query(className: "figure-hand-piece").ToList(), "An attack's army comes back to its base.");
            Assert.IsFalse(map.Q<Button>(Army).ClassListContains(Lifted));
        }

        [Test]
        public void APlannedFlightsFigureLeavesItsPlaceForTheGhost()
        {
            var (view, data, map, draft) = ArmedNazgul();
            var source = map.Q<Button>("army-minas-morgul-Shadow");
            var piece = map.Q("hand-army-minas-morgul-Shadow");
            Assert.AreEqual("1", piece.Q<Label>("army-count").text, "Only the Nazgûl the next click flies lifts.");
            Assert.AreEqual("1", source.Q<Label>("army-count").text, "The other stays on the base.");
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "moria", data));
            map.PlannedFlights.Apply(NazgulFlightDraft.Planned(view, draft), Side.Shadow, false);
            map.DraftOverlay.Apply(view, draft);
            Assert.AreEqual("1", map.Q("hand-army-minas-morgul-Shadow").Q<Label>("army-count").text, "The next Nazgûl lifts.");
            Assert.IsTrue(source.ClassListContains(Lifted), "The flown one stands in its ghost now; none stays behind.");
            Assert.IsNotNull(map.Q<Button>("planned-flight-moria"));
            Assert.IsTrue(map.Q<Button>("planned-flight-moria").ClassListContains(MapPlannedRecruits.GhostClass));
        }

        // UI-405: the raised figures of the unsent move are what a press carries, from the army's piece or a character's.
        [Test]
        public void OnlyARaisedPieceOfTheUnsentMoveIsCarried()
        {
            var (view, map, context) = Board();
            var draft = new TurnDraft();
            Assert.IsFalse(map.Lifts(ArmyToken(Region)), "Nothing raised, nothing to carry.");
            Assert.IsFalse(map.GrabFigures(Vector2.zero));
            ContextArmySelection.SelectAll(draft, view, context, Region, false);
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsTrue(map.Lifts(ArmyToken(Region)), "The army's own piece holds the selection.");
            Assert.IsTrue(map.Lifts(new DraftToken(DraftObject.Miniature, "strider", side: Side.FreePeoples, region: Region)));
            Assert.IsFalse(map.Lifts(ArmyToken("dale")), "Another army has nothing raised.");
            Assert.IsFalse(map.Lifts(new DraftToken(DraftObject.Region, Region)), "A region is never carried.");

            // A split in the region panel: the figure left behind drops back onto the base and is not carried.
            var regular = view.Units.First(u => u.Location == Region && u.Kind == UnitKind.Regular);
            Assert.IsTrue(ContextArmySelection.Select(draft, view, context, regular.Id));
            map.DraftOverlay.Apply(view, draft, context);
            Assert.IsTrue(map.Lifts(ArmyToken(Region)), "The rest of the selection is still raised.");
            Assert.IsTrue(map.GrabFigures(map.Camera.ToScreen(Feet(map, Army))));
            var carried = map.DropFigures();
            CollectionAssert.AreEquivalent(LiftedFigures.Selection(view, draft), carried, "Exactly the selection is carried.");
            CollectionAssert.DoesNotContain(carried, regular.Id, "The figure left behind stays on its base.");
        }

        // UI-405: a card's Nazgûl carried to its destination leaves the hand from where it was dropped (dissolving into its
        // planned figure), and the next one rises at the stack on a piece of its own.
        [Test]
        public void ACarriedNazgulFlownByItsDropLeavesFromThereAndTheNextRisesAtTheStack()
        {
            var (view, data, map, draft) = ArmedNazgul();
            var carried = map.Q("hand-army-minas-morgul-Shadow");
            var feet = map.Camera.ToScreen(Feet(map, "army-minas-morgul-Shadow"));
            Assert.IsTrue(map.GrabFigures(feet));
            map.CarryFigures(map.Camera.ToScreen(map.Stands.Stands("moria").Field));
            CollectionAssert.AreEquivalent(new[] { NazgulFlightDraft.Next(view, draft).Id }, map.DropFigures());
            Assert.IsTrue(NazgulFlightDraft.Fly(view, draft, "moria", data));
            map.PlannedFlights.Apply(NazgulFlightDraft.Planned(view, draft), Side.Shadow, false);
            map.DraftOverlay.Apply(view, draft);
            map.HomeFigures();
            var next = map.Q("hand-army-minas-morgul-Shadow");
            Assert.IsNotNull(next, "The next Nazgûl rises.");
            Assert.AreNotSame(carried, next, "On a piece of its own: the dropped one left from where it was dropped.");
            Assert.AreEqual(1, map.Query(className: "figure-hand-piece").ToList().Count, "Outside a panel the flown one is gone at once.");
            Assert.AreEqual("1", next.Q<Label>("army-count").text);
        }

        private static DraftToken ArmyToken(string region) => new DraftToken(DraftObject.Army, region, side: Side.FreePeoples);

        // The board point a marker (by its button's name) stands on.
        private static Vector2 Feet(MapCanvas map, string marker) => ((MapStand)map.Q<Button>(marker).parent.userData).Feet;

        // A Shadow card's two Nazgûl in Minas Morgul, the stack armed: the first one is raised for its flight.
        private static (PlayerView View, BoardPresentation Data, MapCanvas Map, TurnDraft Draft) ArmedNazgul()
        {
            var data = BoardPresentation.Load();
            var view = new PlayerView
            {
                MatchId = "flights", You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "search", Kind = DecisionKind.CharacterEvent, SourceId = "base.sh.character.09" },
                Units =
                {
                    new UnitView { Id = "nazgul-1", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "minas-morgul" },
                    new UnitView { Id = "nazgul-2", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "minas-morgul" }
                },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.MoveShadowCharacters, Enabled = true, AllowEmptySelection = true,
                        EligibleIds = { "nazgul-1", "nazgul-2" },
                        Travel =
                        {
                            new TravelOption { Id = "nazgul-1", Origin = "minas-morgul", Flight = true, Targets = { "moria", "rohan" } },
                            new TravelOption { Id = "nazgul-2", Origin = "minas-morgul", Flight = true, Targets = { "moria", "rohan" } }
                        }
                    }
                }
            };
            data.Apply(view);
            var map = new MapCanvas(data);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            Assert.IsTrue(NazgulFlightDraft.Arm(view, draft, "minas-morgul"));
            map.DraftOverlay.Apply(view, draft);
            return (view, data, map, draft);
        }

        private static (PlayerView View, MapCanvas Map, ActionDieContext Context) Board(bool attack = false)
        {
            var fixture = new BoardFixture();
            fixture.View.Characters.Add(new CharacterView
            {
                Id = "strider", DefinitionId = "base.character.strider", Side = Side.FreePeoples, Location = Region, Level = 3, Leadership = 1
            });
            fixture.Refresh();
            var view = fixture.View;
            if (attack)
                AddAttack(view);
            var data = BoardPresentation.Load();
            data.Apply(view);
            var context = new ActionDieContext();
            context.Sync(view, "army");
            return (view, new MapCanvas(data), context);
        }

        // The army may also attack Minas Morgul (a fixture's target; the server decides the real ones).
        private static void AddAttack(PlayerView view)
        {
            var movement = view.Options.First(o => o.Kind == CommandKind.MoveArmies);
            var army = movement.Armies.First(a => a.RegionId == Region);
            view.Options.Add(new ActionOption
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
    }
}
