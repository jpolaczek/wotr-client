using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ExpansionPresentationTests
    {
        [Test]
        public void EveryActionDieFaceHasADistinctVisibleSymbol()
        {
            var symbols = System.Enum.GetValues(typeof(DieFace)).Cast<DieFace>()
                .Select(face => AssetCatalog.Current.Die(face)).ToArray();
            Assert.IsTrue(symbols.All(symbol => !string.IsNullOrWhiteSpace(symbol) && symbol != "?"));
            Assert.AreEqual(symbols.Length, symbols.Distinct().Count());
        }

        [TestCase("lords.character.elrond", "Lord Elrond")]
        [TestCase("lords.character.gothmog", "Gothmog")]
        [TestCase("kings.character.theoden.awakened", "Théoden")]
        public void ExpansionCharactersHavePublicNamesAndNullableStats(string id, string name)
        {
            StringAssert.Contains(name, CharacterLibrary.Name(id));
            StringAssert.Contains(name, CharacterLibrary.Description(id));
        }

        [Test]
        public void SovereignInspectionDoesNotInferTheValueOrIdentityOfAHiddenTile()
        {
            var view = new PlayerView { You = Side.FreePeoples, Sovereigns =
                { new SovereignView { Id = "king", Title = "Brand", RegionId = "dale", Status = SovereignStatus.Initial,
                    Resistance = 2, Tiles = { new CorruptionTileView { Id = "slot-0" } } } } };
            var panel = new SovereignPanel(BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            panel.Show("king");
            var tile = panel.Inspector.Q<Label>("attached-tile-slot-0");
            Assert.AreEqual("▧", tile.text);
            Assert.AreEqual("Face-down Corruption tile", tile.tooltip);
            Assert.IsFalse(panel.Inspector.Query<VisualElement>().ToList().Any(e => e.name?.Contains("base.hunt") == true));
            view.Sovereigns.Clear();
            panel.Apply(view);
            Assert.IsEmpty(panel.Root.Children());
            Assert.AreEqual(DisplayStyle.None, panel.Inspector.style.display.value);
        }

        [Test]
        public void SequentialDrawStillShowsAnEnabledStopTileAndNoOpponentChoice()
        {
            var view = new PlayerView { CorruptionAttempt = new CorruptionAttemptView
                { Sequential = true, DrawnCount = 1, DrawLimit = 3, OwnChoices = { new CorruptionTileView { TileId = "one", Value = 1 } } },
                Options = { new ActionOption { Kind = CommandKind.DrawCorruptionTile },
                    new ActionOption { Kind = CommandKind.ChooseCorruptionTile, EligibleIds = { "one" } } } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DrawCorruptionTile);
            var root = new VisualElement();
            var refresh = CorruptionAttemptChoices.Render(root, view, view.Options[0], draft, action => action());
            refresh();
            var stop = root.Q<Button>("corruption-choice-one");
            Assert.IsTrue(stop.enabledSelf);
            StringAssert.StartsWith("Stop", stop.text);
            view.CorruptionAttempt.OwnChoices.Clear();
            root.Clear();
            CorruptionAttemptChoices.Render(root, view, view.Options[0], draft, action => action())();
            Assert.IsEmpty(root.Query<Button>().ToList());
        }

        [Test]
        public void SpecialDiceShowOriginRemovalAndPendingChoiceWhileKeeperEyeRemainsInItsPhysicalOwnersPool()
        {
            var view = new PlayerView { You = Side.FreePeoples, Configuration = new MatchConfiguration { Lords = true }, Dice =
                { new DieView { Id = "nenya", Source = "lords.die.nenya", Owner = Side.FreePeoples, Face = DieFace.Eye,
                    Location = DieLocation.Hunt, HuntOrigin = "keeper-eye", RemoveSymbol = true },
                  new DieView { Id = "gothmog", Source = "lords.die.gothmog", Owner = Side.Shadow, Face = DieFace.Army,
                    Location = DieLocation.AwaitingChoice, RemoveSymbol = true } } };
            var pool = new ActionPoolView(_ => { }, _ => { });
            var root = new VisualElement();
            pool.Apply(root, view, Side.Shadow, true);
            Assert.IsNotNull(root.Q<Button>("gothmog"));
            StringAssert.Contains("Gothmog", root.Q<Button>("gothmog").tooltip);
            // UI-267: the Remove star is a badge of the die's own art, not a text mark.
            Assert.IsNotNull(root.Q<Button>("gothmog").Q("die-remove-star"));
            Assert.IsNull(root.Q<Button>("gothmog").Q<Label>(className: "die-badge"));
            pool.Apply(root, view, Side.FreePeoples, true);
            Assert.AreEqual("1", root.Q<Button>("pool-Hunt").text, "UI-97: icon and count; the name leads the description.");
            StringAssert.StartsWith("Hunt\n", root.Q<Button>("pool-Hunt").tooltip);
            StringAssert.Contains("keeper", root.Q<Button>("pool-Hunt").tooltip.ToLowerInvariant());
            Assert.IsTrue(root.Q<Button>("pool-Hunt").ClassListContains(ReadOnlyControl.ClassName), "UI-230: the count is information only.");
            Assert.AreEqual("CHOOSE A DIE", root.Q<Label>("dice-ring-prompt").text, "UI-238: the band's prompt for an armed Ring.");
            Assert.IsNull(root.Q<Button>("nenya"), "A die in the Hunt box stays out of the pool's row without a decision.");
            view.DecisionOwner = Side.FreePeoples;
            view.Options.Add(new ActionOption { Kind = CommandKind.UseRing, Enabled = true, EligibleDice = { "nenya" } });
            pool.Apply(root, view, Side.FreePeoples, true);
            Assert.IsNotNull(root.Q<Button>("nenya"), "UI-230: a die a decision may choose stands in the pool's row.");
        }

        [Test]
        public void RecruitmentDistinguishesLocalSlotsFromSettlementsAndRejectsDuplicatePhysicalFigures()
        {
            var choice = new CharacterActionOption { TargetCount = 3, RulerRecruitment = new RulerRecruitmentOption
                { LocalRegion = "eastemnet", SettlementCount = 2, Settlements = { "edoras", "helms-deep" } }, Recruitment =
                { new RecruitmentOption { Nation = "rohan", Kind = UnitKind.Regular, ReserveIds = { "a", "b", "c" },
                    Targets = { "eastemnet", "edoras", "helms-deep" } } } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseCharacterAbility);
            Assert.IsTrue(CharacterRecruitDraft.Drop(draft, choice, "a", "eastemnet"));
            Assert.IsFalse(CharacterRecruitDraft.Drop(draft, choice, "a", "edoras"));
            Assert.IsFalse(CharacterRecruitDraft.Drop(draft, choice, "b", "eastemnet"));
            Assert.IsTrue(CharacterRecruitDraft.Drop(draft, choice, "b", "edoras"));
            Assert.IsFalse(CharacterRecruitDraft.Drop(draft, choice, "c", "edoras"));
            Assert.IsTrue(CharacterRecruitDraft.Drop(draft, choice, "c", "helms-deep"));
            Assert.IsTrue(CharacterRecruitDraft.Ready(draft, choice));
        }
    }
}
