using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-344 (EXP-15 D1-D5): the opponent's expansion actions make cues from their recorded facts — a sovereign's change
    // with the tiles it revealed, an awakening as the Leader's transformation, the Balrog's level in place, the player's
    // own Ruler die rerolled by Dismay, Sméagol leaving the Fellowship — and the opponent's tokens have fixed chips.
    public sealed class OpponentExpansionCueTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();
        private const string Dain = "kings.sovereign.dain";

        [Test]
        public void AnAwakeningIsTheLeadersTransformationAndHisChangeFollows()
        {
            var model = Model(Side.Shadow);
            var step = Step("awaken", 2, 1, Side.FreePeoples, "SovereignAwakened");
            step.Facts.Figures.Add(new ActionFigureChange
            {
                Before = new ActionParticipant { Id = Dain, Side = Side.FreePeoples, NationId = "dwarves", UnitKind = UnitKind.Leader },
                After = new ActionParticipant { Id = Dain, Side = Side.FreePeoples, DefinitionId = "kings.character.dain.awakened" },
                From = "erebor", To = "erebor"
            });
            step.Facts.Sovereigns.Add(King(SovereignStatus.Awakened, 0));
            var cues = Merge(model, step);
            CollectionAssert.AreEqual(new[] { PresentationCueKind.Transformation, PresentationCueKind.Sovereign }, cues.Select(cue => cue.Kind));
            Assert.AreEqual("erebor", cues[0].Regions.Single());
            Assert.AreEqual(SovereignStatus.Awakened, cues[1].Sovereign.Status);
        }

        [Test]
        public void ACorruptionShowsTheAttachedTileOnceAndItsRevealedTilesWithIt()
        {
            var model = Model(Side.FreePeoples);
            var attached = Step("attached", 2, 1, null, "CorruptionAdded");
            attached.Facts.Sovereigns.Add(King(SovereignStatus.Initial, 1));
            var first = Step("reveal-1", 2, 2, null, "CorruptionTileRevealed");
            first.Facts.Targets.Add("base.hunt.standard.1.0.1");
            var second = Step("reveal-2", 2, 3, null, "CorruptionTileRevealed");
            second.Facts.Targets.Add("base.hunt.standard.2.0.1");
            var corrupted = Step("corrupted", 2, 4, null, "SovereignCorrupted");
            corrupted.Facts.Sovereigns.Add(King(SovereignStatus.Corrupted, 0));
            var repeated = Step("primary", 2, 5, null, "ActionDecision");
            repeated.Facts.Sovereigns.Add(King(SovereignStatus.Corrupted, 0));
            var page = Page(attached, first, second, corrupted, repeated);
            page.Actions.Add(new ActionLogSummary { Id = "action-2", Initiator = Side.Shadow });
            model.Advance(2);
            var cues = model.Merge(new[] { page }).Where(cue => cue.Kind == PresentationCueKind.Sovereign).ToList();
            Assert.AreEqual(2, cues.Count, "The primary step's repeated state shows once.");
            Assert.IsEmpty(cues[0].Tiles, "The attached tile's value stays hidden.");
            CollectionAssert.AreEqual(new[] { "base.hunt.standard.1.0.1", "base.hunt.standard.2.0.1" }, cues[1].Tiles);
            Assert.AreEqual("erebor", cues[1].Regions.Single());
        }

        [Test]
        public void DismayRerollingMyRulerDieIsTheOpponentsBeat()
        {
            var model = Model(Side.FreePeoples);
            var step = Step("dismay", 2, 1, Side.Shadow, "DismayUsed");
            step.Facts.Dice.Add(new ActionDieChange
            {
                Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Before = DieFace.Character, After = DieFace.Muster,
                From = DieLocation.Available, To = DieLocation.Available
            });
            step.Facts.Dice.Add(new ActionDieChange
            {
                Id = "spent", Owner = Side.FreePeoples, Before = DieFace.Army, After = DieFace.Army,
                From = DieLocation.Available, To = DieLocation.Spent
            });
            var cues = Merge(model, step);
            Assert.AreEqual("ruler", cues.Single(cue => cue.Kind == PresentationCueKind.Die).Die.Id,
                "Only a die rerolled in place is the opponent's beat; my own dice otherwise stay mine.");
            var same = Step("dismay-same", 3, 1, Side.Shadow, "DismayUsed");
            same.Facts.Dice.Add(new ActionDieChange
            {
                Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Before = DieFace.Muster, After = DieFace.Muster,
                From = DieLocation.Available, To = DieLocation.Available
            });
            Assert.AreEqual(PresentationCueKind.Die, Merge(model, same).Single().Kind, "A reroll to the same face is still its beat.");
        }

        [Test]
        public void TheBalrogsLevelChangesInPlace()
        {
            var model = Model(Side.FreePeoples);
            var step = Step("balrog", 2, 1, Side.Shadow, "CharacterAbilityUsed");
            step.Facts.Figures.Add(new ActionFigureChange
            {
                Before = new ActionParticipant { Id = "balrog", Side = Side.Shadow, DefinitionId = "lords.character.balrog", Level = 0 },
                After = new ActionParticipant { Id = "balrog", Side = Side.Shadow, DefinitionId = "lords.character.balrog", Level = 2 },
                From = "moria", To = "moria"
            });
            var cue = Merge(model, step).Single();
            Assert.AreEqual(PresentationCueKind.Transformation, cue.Kind);
            Assert.AreEqual("moria", cue.Regions.Single());
            Assert.IsEmpty(FigureChangePresenter.Deltas(cue), "The same figure in place needs no badge.");
        }

        [Test]
        public void SmeagolLeavingTheFellowshipLeavesItsRoster()
        {
            var model = Model(Side.Shadow);
            var step = Step("smeagol", 2, 1, Side.FreePeoples, "CompanionDiscarded");
            step.Facts.Figures.Add(new ActionFigureChange
            {
                After = new ActionParticipant { Id = "smeagol", Side = Side.FreePeoples, DefinitionId = "lords.character.smeagol" },
                From = "fellowship", To = "removed"
            });
            var cue = Merge(model, step).Single();
            Assert.AreEqual(PresentationCueKind.Removal, cue.Kind);
            Assert.AreEqual("fellowship", cue.Regions.Single(), "No map region: the roster shows it.");
        }

        [Test]
        public void TheExpansionsAbilityCommandsNameTheirCharacter()
        {
            Assert.AreEqual("balrog", CharacterLibrary.AbilityHolder("activate-balrog"));
            Assert.AreEqual("balrog", CharacterLibrary.AbilityHolder("deactivate-balrog"));
            Assert.AreEqual("gothmog", CharacterLibrary.AbilityHolder("gothmog-recruit"));
            Assert.AreEqual("theoden", CharacterLibrary.AbilityHolder("kings.mustering-of-the-mark"));
            Assert.IsNull(CharacterLibrary.AbilityHolder("kings.palantir.muster-as-event"), "A table card's conversion names no one.");
        }

        [Test]
        public void TheOpponentsTokensHaveFixedChipsForTheirModules()
        {
            var summary = new OpponentSummary();
            var view = new PlayerView
            {
                You = Side.Shadow, Configuration = new MatchConfiguration { Kings = true, Lords = true, Council = true },
                RulerTokens = { new RulerTokenView { Id = "kings.token.dain", Location = "free-peoples" },
                    new RulerTokenView { Id = "kings.token.theoden", Location = "dismay" } },
                CouncilTokens = { new CouncilTokenView { Id = "politics" }, new CouncilTokenView { Id = "travel", Spent = true } }
            };
            summary.Apply(view, Side.FreePeoples);
            Assert.AreEqual(DisplayStyle.Flex, summary.Root.Q("opponent-ruler").style.display.value);
            Assert.AreEqual(DisplayStyle.None, summary.Root.Q("opponent-dismay").style.display.value, "Dismay tokens are the Shadow's.");
            StringAssert.EndsWith("Ruler tokens: 1", summary.Root.Q("opponent-ruler").tooltip);
            view.You = Side.FreePeoples;
            summary.Apply(view, Side.Shadow);
            Assert.AreEqual(DisplayStyle.None, summary.Root.Q("opponent-ruler").style.display.value);
            StringAssert.EndsWith("Dismay tokens: 1", summary.Root.Q("opponent-dismay").tooltip);
            StringAssert.EndsWith("Council tokens: 1", summary.Root.Q("opponent-council").tooltip);
            Assert.AreEqual("opponent-dismay", OpponentSummary.ChipName(ActionCostKind.RulerToken, Side.Shadow));
            view.Configuration = new MatchConfiguration();
            summary.Apply(view, Side.Shadow);
            Assert.AreEqual(DisplayStyle.None, summary.Root.Q("opponent-council").style.display.value, "A base game shows no token chips.");
        }

        private static OpponentPresentationModel Model(Side you)
        {
            var model = new OpponentPresentationModel(you, Data);
            model.Baseline(new ActionLogPage[0], 1);
            return model;
        }

        private static System.Collections.Generic.List<PresentationCue> Merge(OpponentPresentationModel model, ActionLogStep step)
        {
            model.Advance(step.Revision);
            return model.Merge(new[] { Page(step) }).ToList();
        }

        private static SovereignView King(SovereignStatus status, int tiles)
        {
            var king = new SovereignView { Id = Dain, Title = "Dáin Ironfoot", Nation = "dwarves", RegionId = "erebor", Status = status, Resistance = 4 };
            for (var index = 0; index < tiles; index++)
                king.Tiles.Add(new CorruptionTileView { Id = Dain + ":tile:" + index });
            return king;
        }

        private static ActionLogPage Page(params ActionLogStep[] steps) => new ActionLogPage { Steps = steps.ToList() };

        private static ActionLogStep Step(string id, long revision, int order, Side? actor, string kind) => new ActionLogStep
        {
            Id = id, ActionId = "action-" + revision, Revision = revision, Order = order, Actor = actor, Kind = kind, Automatic = !actor.HasValue
        };
    }
}
