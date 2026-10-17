using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class EffectsPresentationTests
    {
        [Test]
        public void SourcesGroupBothSidesWithoutLosingIndependentScopesOrExpiry()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("one", "rivendell", "EndTurn"));
            view.Effects.Add(Effect("two", "lorien", "EndBattle"));
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { "base.fp.strategy.02" } });
            view.Players.Add(new PlayerSummary { Side = Side.Shadow, TableCards = { "lords.sh.special.05" } });
            var sources = EffectsPresentation.Sources(view);
            Assert.AreEqual(2, sources.Count);
            Assert.AreEqual(2, sources[0].Effects.Count);
            Assert.AreEqual(2, sources[0].Effects.Select(e => e.Duration).Distinct().Count());
            Assert.AreEqual(2, sources[0].Effects.SelectMany(e => e.Targets).Select(t => t.Id).Distinct().Count());
            Assert.AreEqual("lords.sh.special.05", sources[1].SourceId);
        }

        [Test]
        public void GeneralScopesNeverSpreadAcrossTheMapAndExpirySubjectsAreNotTargets()
        {
            var view = new PlayerView();
            view.Characters.Add(new CharacterView { Id = "saruman", Location = "orthanc" });
            var effect = new ActiveEffectView
            {
                SourceId = "base.sh.character.22", TargetId = "saruman",
                Targets = { new EffectTargetView { Kind = "Nation", Id = "rohan" } }
            };
            view.Effects.Add(effect);
            Assert.IsEmpty(EffectsPresentation.LocalRegions(view, effect));
            Assert.IsNull(new RegionViewIndex(view).Get("rohan"));
            Assert.IsEmpty(new RegionViewIndex(view).Get("orthanc").Effects);
            effect.Targets.Clear();
            effect.Targets.Add(new EffectTargetView { Kind = "NationRegions", Id = "rohan" });
            Assert.IsEmpty(EffectsPresentation.LocalRegions(view, effect));
        }

        [Test]
        public void LocalEffectDoesNotMakeAnEmptyRegionHoverable()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("local", "rivendell"));
            var index = new RegionViewIndex(view);
            Assert.AreEqual(1, index.Get("rivendell").Effects.Count);
            Assert.IsFalse(index.Occupied("rivendell"));
        }

        [Test]
        public void EmptyPanelStillHasItsTabAndAReadableEmptyState()
        {
            var table = Panel();
            table.Apply(new PlayerView());
            Assert.AreEqual("Effects · 0", table.Tab.text);
            table.Open();
            Assert.IsTrue(table.IsOpen);
            Assert.AreEqual("No active effects", table.Root.Q<Label>("effects-empty").text);
        }

        [Test]
        public void PublicAbilityHasDistinctReadableDescriptionsAndNoCardAssumption()
        {
            var view = new PlayerView();
            var effect = Effect("ability", "helms-deep");
            effect.SourceId = "kings.character.theoden.corrupted";
            effect.SourceKind = "Ability";
            effect.Title = "Théoden";
            effect.Description = "Shadow-controlled Helm’s Deep is an Isengard stronghold.";
            view.Effects.Add(effect);
            var table = Panel();
            table.Apply(view);
            table.Inspect(effect.SourceId);
            Assert.AreEqual(effect.Description, table.Root.Q<Label>("effect-description-ability").text);
            Assert.IsNotNull(table.Root.Q<Button>("effect-locate-helms-deep"));
            Assert.IsNull(table.Root.Q("effect-card"), "An ability has no card text block.");
        }

        [Test]
        public void UpdatesKeepSourceControlsAndPinnedEndedInformationButRemoveTheirActions()
        {
            var view = new PlayerView { MatchId = "effects" };
            view.Effects.Add(Effect("local", "rivendell"));
            var table = Panel();
            table.Apply(view);
            table.Inspect("base.fp.strategy.02");
            var source = table.Root.Q<Button>("effect-source-base.fp.strategy.02");
            var card = CardLibrary.Cards["base.fp.strategy.02"];
            Assert.AreEqual("Free Peoples · In play", table.Root.Q<Label>("effect-detail-state").text);
            Assert.AreEqual(card.Event, table.Root.Q<Label>("effect-card-event").text, "UI-94: the card's text is in the panel.");
            Assert.AreEqual(CardTextFormat.Kind(card), table.Root.Q<Label>("effect-card-kind").text);
            Assert.AreEqual(CardTextFormat.CombatHeading(card), table.Root.Q<Label>("effect-card-combat-heading").text);
            Assert.AreEqual(card.Combat, table.Root.Q<Label>("effect-card-combat-text").text);
            view.Revision++;
            view.Effects[0].ExpiryDescription = "At the end of this turn.";
            table.Apply(view);
            Assert.AreSame(source, table.Root.Q<Button>(source.name));
            Assert.AreEqual("At the end of this turn.", table.Root.Q<Label>("effect-expiry-local").text);
            view.Effects.Clear();
            table.Apply(view);
            table.Context(System.Array.Empty<ComponentAction>(), true);
            Assert.IsNull(table.Root.Q<Button>(source.name));
            Assert.AreEqual("Effect ended", table.Root.Q<Label>("effect-detail-state").text);
            Assert.AreEqual(card.Event, table.Root.Q<Label>("effect-card-event").text, "An ended pinned source keeps its card text.");
            Assert.IsTrue(table.Root.Q("effect-card").ClassListContains("effect-card-ended"));
            Assert.IsNull(table.Root.Q<Button>("remove-table-base.fp.strategy.02"));
            Assert.AreEqual("Effects · 0", table.Tab.text);
        }

        [Test]
        public void EnemySourceUsesOnlyTheViewersActualRemovalOptionAndExactCost()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "own", Kind = DecisionKind.Action },
                Options = { new ActionOption { Kind = CommandKind.DiscardProtection, Enabled = true,
                    EligibleDice = { "die" }, Targets = { "base.fp.strategy.02" } } }
            };
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "die");
            var action = ComponentActions.For(view, draft).Single();
            Assert.IsTrue(action.Enabled);
            StringAssert.Contains("one Army Event and one Character Event", EffectsPresentation.RemovalCost(view, action));
            view.DecisionOwner = Side.FreePeoples;
            Assert.IsEmpty(ComponentActions.For(view, draft));
        }

        [Test]
        public void ClosingAPaymentPreservesItsDraftAndARefreshDoesNotForceTheDrawerOpen()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("local", "rivendell"));
            var table = Panel();
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            table.Attach(tray, dock);
            table.Apply(view);
            table.Context(new[] { new ComponentAction { Kind = CommandKind.DiscardProtection, CardId = "base.fp.strategy.02", Enabled = true } }, true);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardProtection, "chosen-die");
            draft.Change(p => p.CardId = "base.fp.strategy.02");
            draft.Toggle("base.sh.strategy.01");
            table.Draft(view, draft);
            Assert.IsTrue(table.IsOpen);
            table.Close();
            table.Draft(view, draft);
            Assert.IsFalse(table.IsOpen);
            Assert.AreSame(table.DecisionHost, tray.parent);
            Assert.AreEqual("chosen-die", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { "base.sh.strategy.01" }, draft.Payload.Ids);
            table.Open();
            Assert.AreSame(table.DecisionHost, tray.parent);
        }

        [Test]
        public void PinnedCompleteCardKeepsItsTextWhenThePublicSourceExpires()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("local", "rivendell"));
            var inspector = new CardInspector();
            inspector.Update(view);
            inspector.Show("base.fp.strategy.02", true, allowPlay: false);
            var text = inspector.Root.Q<Label>("card-text").text;
            Assert.AreEqual("In play", inspector.Root.Q<Label>("card-window").text);
            view.Effects.Clear();
            inspector.Update(view);
            Assert.IsTrue(inspector.Pinned);
            Assert.AreEqual(text, inspector.Root.Q<Label>("card-text").text);
            Assert.AreEqual("Effect ended", inspector.Root.Q<Label>("card-window").text);
        }

        [Test]
        public void PalantirCostExplainsTheAlternativesBeforeSelectionAndOnlyTheChosenCostAfterwards()
        {
            var view = new PlayerView();
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardPalantir, DiceWithoutRing = { "will" } });
            var action = new ComponentAction { Kind = CommandKind.DiscardPalantir };
            StringAssert.Contains("Will of the West, or", EffectsPresentation.RemovalCost(view, action));
            action.DieId = "will";
            StringAssert.Contains("No Ring", EffectsPresentation.RemovalCost(view, action));
            action.DieId = "army";
            StringAssert.Contains("one chosen Elven Ring", EffectsPresentation.RemovalCost(view, action));
            StringAssert.DoesNotContain("Will of the West", EffectsPresentation.RemovalCost(view, action));
        }

        private static TableEffectsView Panel() => new TableEffectsView(new VisualElement(), new MapCanvas(BoardPresentation.Load()));

        private static ActiveEffectView Effect(string id, string region, string duration = "UntilRemoved") => new ActiveEffectView
        {
            Id = id, SourceId = "base.fp.strategy.02", Title = "A Power too Great", SourceKind = "Card",
            Modifier = "PreventShadowArmyEntry", Duration = duration,
            Targets = { new EffectTargetView { Kind = "Region", Id = region, Label = FigureGrid.Name(region) } }
        };
    }
}
