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

        // UI-446 (owner): an ability's tile is read only — its descriptions unroll as plain text on hover (EffectsLayoutTests)
        // — and neither a click nor Inspect opens details or a card text block for it.
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
            var tile = table.Root.Q<Button>("effect-source-" + effect.SourceId);
            Assert.IsTrue(ReadOnlyControl.Is(tile), "A click on the ability does nothing.");
            Assert.AreEqual("Théoden · A public ability in play", tile.tooltip);
            Assert.IsFalse(tile.ClassListContains("effect-card-tile"), "An ability is not a card.");
            table.Inspect(effect.SourceId);
            Assert.IsTrue(table.IsOpen);
            Assert.IsEmpty(table.SelectedSource, "Reading a source selects nothing.");
            Assert.AreEqual(DisplayStyle.None, table.Details.style.display.value, "No details open for a read source.");
            Assert.IsNull(table.Root.Q<Button>("effect-locate-helms-deep"), "Outside a removal no details hold a place.");
            Assert.IsNull(table.Root.Q("effect-card"), "An ability has no card text block.");
        }

        // UI-414 (owner): each titled ability of a source has its own button, and a card's effects keep one.
        [Test]
        public void EachTitledAbilityHasItsOwnButtonAndACardKeepsOne()
        {
            var view = new PlayerView();
            view.Effects.Add(Ability("ruler", "edoras", "Corrupted ruler · Théoden", "No Leader or Elite is recruited in Rohan."));
            view.Effects.Add(Ability("mark", "helms-deep", "Mark of the White Hand · Théoden", "Helm's Deep is an Isengard Stronghold."));
            view.Effects.Add(Effect("one", "rivendell", "EndTurn"));
            view.Effects.Add(Effect("two", "lorien", "EndBattle"));
            var sources = EffectsPresentation.Sources(view);
            Assert.AreEqual(3, sources.Count);
            CollectionAssert.AreEquivalent(new[] { "Corrupted ruler · Théoden", "Mark of the White Hand · Théoden", "A Power too Great" },
                sources.Select(s => s.Title));
            Assert.AreEqual(3, sources.Select(s => s.Key).Distinct().Count());
            Assert.AreEqual(2, sources.Single(s => s.Title == "A Power too Great").Effects.Count);
            Assert.AreEqual(EffectsPresentation.Key("base.fp.strategy.02", Side.FreePeoples), sources.Single(s => s.Title == "A Power too Great").Key);
        }

        // UI-414 (owner): a place shows its button once in a source, and none where the effect's text names it. UI-446: a
        // source's details, with its places' buttons, show only while its removal is paid, so each check prepares one (the
        // removal is only the way to the details here).
        [Test]
        public void APlaceShowsItsButtonOnceAndNotWhereTheTextNamesIt()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("one", "rivendell", "EndTurn"));
            view.Effects.Add(Effect("two", "rivendell", "EndBattle"));
            view.Effects.Add(Ability("ban", "erebor", "Corrupted ruler · Dáin", "Leaders and Elites cannot be recruited in Erebor."));
            var table = Payable();
            table.Apply(view);
            table.Inspect("base.fp.strategy.02");
            Assert.IsEmpty(table.Root.Query<Button>("effect-locate-rivendell").ToList(), "Reading a source shows no details.");
            PrepareRemoval(table, view, "base.fp.strategy.02");
            Assert.AreEqual(DisplayStyle.Flex, table.Details.style.display.value);
            Assert.AreEqual(1, table.Root.Query<Button>("effect-locate-rivendell").ToList().Count, "Rivendell shows once.");
            PrepareRemoval(table, view, "kings.character.dain.corrupted");
            Assert.AreEqual("Corrupted ruler · Dáin", table.Root.Q<Label>("effect-detail-title").text);
            Assert.IsNull(table.Root.Q<Button>("effect-locate-erebor"), "The text names Erebor.");
        }

        // UI-446 (owner): a plain click opens nothing, so a source's details show only while its removal is paid; there they
        // follow the source's updates, say when it ended and go with the payment. The pinned details of a source read and
        // then ended (UI-94's "Effect ended" outside a payment) no longer exist, so this case no longer asserts them.
        [Test]
        public void UpdatesKeepSourceControlsAndPinnedEndedInformationButRemoveTheirActions()
        {
            var view = new PlayerView { MatchId = "effects" };
            view.Effects.Add(Effect("local", "rivendell"));
            var table = Payable();
            table.Apply(view);
            var source = table.Root.Q<Button>("effect-source-base.fp.strategy.02");
            var card = CardLibrary.Cards["base.fp.strategy.02"];
            table.Inspect("base.fp.strategy.02");
            Assert.IsTrue(table.IsOpen);
            Assert.IsEmpty(table.SelectedSource, "Reading a source selects nothing.");
            Assert.AreEqual(DisplayStyle.None, table.Details.style.display.value, "No details open for a read source.");
            Assert.IsTrue(ReadOnlyControl.Is(source), "A click on a source without a removal does nothing.");
            Assert.AreEqual("A Power too Great · A card in effect", source.tooltip);
            var draft = PrepareRemoval(table, view, "base.fp.strategy.02");
            var removal = table.Root.Q<Button>("remove-table-base.fp.strategy.02");
            Assert.IsFalse(ReadOnlyControl.Is(source), "The lit tile prepares the removal as its × does.");
            Assert.AreEqual(removal.tooltip, source.tooltip);
            Assert.AreEqual("base.fp.strategy.02", table.SelectedSource);
            Assert.AreEqual(DisplayStyle.Flex, table.Details.style.display.value, "The payment shows the source's details.");
            Assert.AreEqual("Free Peoples · In play", table.Root.Q<Label>("effect-detail-state").text);
            // Its card text block (hidden during the payment by LeftRail.uss) holds only the part in effect: the Event text.
            Assert.AreEqual(card.Event, table.Details.Q<Label>("effect-card-event").text);
            Assert.AreEqual(CardTextFormat.Kind(card), table.Details.Q<Label>("effect-card-kind").text);
            Assert.AreEqual(DisplayStyle.None, table.Details.Q("effect-card-combat").style.display.value, "The Combat part is not in effect.");
            view.Revision++;
            view.Effects[0].ExpiryDescription = "At the end of this turn.";
            table.Apply(view);
            Assert.AreSame(source, table.Root.Q<Button>(source.name));
            Assert.AreEqual("At the end of this turn.", table.Root.Q<Label>("effect-expiry-local").text);
            view.Effects.Clear();
            table.Apply(view);
            table.Context(System.Array.Empty<ComponentAction>(), true);
            Assert.IsNull(table.Root.Q<Button>(source.name));
            Assert.AreEqual("Effect ended", table.Root.Q<Label>("effect-detail-state").text, "The paid source ended.");
            Assert.IsTrue(table.Details.Q("effect-card").ClassListContains("effect-card-ended"));
            Assert.IsNull(table.Root.Q<Button>("remove-table-base.fp.strategy.02"));
            Assert.AreEqual("Effects · 0", table.Tab.text);
            table.Draft(view, draft);
            Assert.IsEmpty(table.SelectedSource, "The payment ends with its source.");
            Assert.AreEqual(DisplayStyle.None, table.Details.style.display.value, "Nothing pinned stays after the payment.");
        }

        // UI-446 (owner): a card shows only its part in effect — its Combat text while every effect it has lasts a battle or
        // a combat round, otherwise its Event text.
        [Test]
        public void ACardShowsItsCombatPartOnlyWhileEveryEffectLastsABattleOrARound()
        {
            var view = new PlayerView();
            view.Effects.Add(Effect("battle", "rivendell", "EndBattle"));
            view.Effects.Add(Effect("round", "lorien", "EndCombatRound"));
            Assert.IsTrue(EffectsPresentation.CombatPart(EffectsPresentation.Sources(view).Single()));
            view.Effects.Add(Effect("turn", "grey-havens", "EndTurn"));
            Assert.IsFalse(EffectsPresentation.CombatPart(EffectsPresentation.Sources(view).Single()), "A lasting effect is the Event's.");
            view.Effects.Clear();
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { "base.fp.strategy.02" } });
            Assert.IsFalse(EffectsPresentation.CombatPart(EffectsPresentation.Sources(view).Single()), "A table card shows its Event.");
            Assert.IsFalse(EffectsPresentation.CombatPart(null));
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
            StringAssert.Contains("give one Elven Ring to the Shadow", EffectsPresentation.RemovalCost(view, action));
            StringAssert.DoesNotContain("Will of the West", EffectsPresentation.RemovalCost(view, action));
        }

        private static ActiveEffectView Ability(string id, string region, string title, string description)
        {
            var effect = Effect(id, region);
            effect.SourceId = id == "ban" ? "kings.character.dain.corrupted" : "kings.character.theoden.corrupted";
            effect.SourceKind = "Ability";
            effect.Owner = Side.Shadow;
            effect.Title = title;
            effect.Description = description;
            effect.Modifier = "";
            return effect;
        }

        private static TableEffectsView Panel() => new TableEffectsView(new VisualElement(), new MapCanvas(BoardPresentation.Load()));

        // A panel with the decision tray it takes in while a removal is paid.
        private static TableEffectsView Payable()
        {
            var table = Panel();
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            table.Attach(tray, dock);
            return table;
        }

        // The removal of this source is offered and being paid, as its × or its lit tile prepares it.
        private static TurnDraft PrepareRemoval(TableEffectsView table, PlayerView view, string source)
        {
            table.Context(new[] { new ComponentAction { Kind = CommandKind.DiscardProtection, CardId = source, Enabled = true } }, true);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DiscardProtection, "die");
            draft.Change(p => p.CardId = source);
            table.Draft(view, draft);
            return draft;
        }

        private static ActiveEffectView Effect(string id, string region, string duration = "UntilRemoved") => new ActiveEffectView
        {
            Id = id, SourceId = "base.fp.strategy.02", Title = "A Power too Great", SourceKind = "Card",
            Modifier = "PreventShadowArmyEntry", Duration = duration,
            Targets = { new EffectTargetView { Kind = "Region", Id = region, Label = FigureGrid.Name(region) } }
        };
    }
}
