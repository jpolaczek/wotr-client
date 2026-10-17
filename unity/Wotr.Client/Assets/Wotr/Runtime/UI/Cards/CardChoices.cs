using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CardChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly BoardPresentation data;
        private readonly PlayerView view;
        private readonly TurnDraft draft;
        private readonly Action<Action> edit;
        private readonly Action<string> inspect;
        private readonly VisualElement footer;
        private readonly CardInspector inspector;
        private readonly Func<VisualElement> reinforcements;

        public CardChoices(VisualElement root, VisualElement footer, PlayerView view, TurnDraft draft, Action<Action> edit,
            BoardPresentation data, Action<string> inspect, CardInspector inspector = null, Func<VisualElement> reinforcements = null)
        {
            this.data = data;
            this.reinforcements = reinforcements;
            this.inspector = inspector;
            this.view = view;
            this.draft = draft;
            this.edit = edit;
            this.inspect = inspect;
            this.footer = footer;
            var option = DraftOptions.For(view, draft);
            if (draft.Kind == CommandKind.PlayEvent)
                Event(root, option);
            else if (draft.Kind == CommandKind.DiscardProtection)
                Protection(root, option);
            else if (draft.Kind == CommandKind.DiscardCards)
            {
                var count = new Label();
                root.Add(count);
                refresh.Add(() => count.text = $"Choose from your hand · {draft.Payload.Ids.Count} / {option.Minimum} to discard face down");
            }
            else
                Effect(root, option);
        }

        private void Protection(VisualElement root, ActionOption option)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            foreach (var id in option.Targets)
                row.Add(Choice(CardLibrary.Title(id), "remove-protection-" + id,
                    () => draft.Payload.CardId == id, () => draft.Change(p => p.CardId = id)));
            refresh.Add(() => row.style.display = draft.Payload.CardId.Length > 0 ? DisplayStyle.None : DisplayStyle.Flex);
            var cost = new Label { name = "protection-cost" };
            root.Add(cost);
            refresh.Add(() => cost.text = "From your hand · " + string.Join(" · ", new[] { "Army", "Character" }.Select(group =>
                group + (option.EffectChoices.Any(c => c.SelectionGroup == group && draft.Payload.Ids.Contains(c.Id)) ? " ✓" : " ○"))));
            cost.tooltip = "Choose one Army Event card and one Character Event card, then an action die. Cards are discarded face down.";
        }

        private void Event(VisualElement root, ActionOption option)
        {
            var card = CardDraft.Event(draft, option);
            if (card == null)
            {
                root.Add(new Label("Choose an available card from your hand."));
                return;
            }
            var title = BoardScreen.Button(CardLibrary.Title(card.CardId), () => inspect(card.CardId));
            title.AddToClassList("draft-card");
            root.Add(title);
            var costs = card.Costs.Where(c => c.DieId == draft.Payload.DieId).ToList();
            if (costs.Count > 1)
            {
                var row = BoardScreen.Row();
                row.Add(new Label("Use as"));
                root.Add(row);
                foreach (var cost in costs)
                    row.Add(Choice(CardDrawDraft.Label(cost), "card-cost-" + cost.Face,
                        () => draft.Payload.Face == cost.Face && draft.Payload.AbilityId == cost.AbilityId,
                        () => draft.Change(p => { p.Face = cost.Face; p.CardCost = cost.Face; p.AbilityId = cost.AbilityId; })));
            }
            if (card.NationTargets.Count == 0)
                return;
            var nations = BoardScreen.Row();
            root.Add(nations);
            foreach (var id in card.NationTargets)
                nations.Add(Choice(FigureGrid.Name(id), "card-nation-" + id,
                    () => draft.Payload.NationId == id, () => draft.Change(p => p.NationId = id)));
        }

        private void Effect(VisualElement root, ActionOption option)
        {
            if (view.Decision?.Presentation == "Deck")
            {
                root.Add(new Label("Choose a highlighted deck."));
                foreach (var choice in option.EffectChoices.Where(c => c.Id is not ("character" or "strategy")))
                    root.Add(EffectButton(option, choice));
                return;
            }
            // UI-131: reserve figures offered by a card are chosen in the Reinforcements panel.
            if (view.Decision?.Presentation == "ReserveFigures" && reinforcements != null)
            {
                root.Add(reinforcements());
                return;
            }
            if (view.Decision?.Presentation == "InspectedCorruption")
            {
                refresh.Add(CorruptionInspectionChoices.Render(root, view, option, draft, edit));
                return;
            }
            if (view.Decision?.Presentation is "ArmyFigure" or "ArmyFigures")
            {
                ArmyFigures(root, option);
                return;
            }
            if (view.Decision?.Presentation == "CompanionDestination")
            {
                CompanionDestination(root, option);
                return;
            }
            if (view.Decision?.Presentation == "RegionOrShelter")
            {
                var shelter = option.EffectChoices.FirstOrDefault(c => c.Id == "scouts.shelter");
                if (shelter != null)
                    root.Add(EffectButton(option, shelter));
                BoardChoice(root, option);
                return;
            }
            if (view.Decision?.Presentation is "Region" or "ActionDie")
            {
                // UI-214: a card decision's alternatives in one region are buttons in the console.
                if (view.Decision.Presentation == "Region" && !CardConsoleOptions.HostsAlternatives(view, draft))
                    RegionAlternatives(root, option);
                BoardChoice(root, option);
                return;
            }
            var figures = new EffectFigurePicker(root, view, option, draft, edit);
            refresh.Add(figures.Refresh);
            var currentGroup = "";
            VisualElement row = null;
            foreach (var id in option.EligibleIds)
            {
                // UI-245: a battle die or figure is chosen on itself in the Battle window; UI-249: a Companion of The
                // Breaking of the Fellowship on its portrait in the Hunt panel; UI-247: a battle reaction on its portrait.
                if (figures.Rendered.Contains(id) || BattleObjectChoices.Offered(view, id) || FellowshipActions.RosterChoice(view, id) ||
                    BattleReactions.OnPortrait(view, id))
                    continue;
                var choice = option.EffectChoices.FirstOrDefault(c => c.Id == id) ?? new EffectChoiceView { Id = id };
                if (row == null || choice.SelectionGroup != currentGroup)
                {
                    currentGroup = choice.SelectionGroup;
                    row = BoardScreen.Row();
                    row.name = "effect-group-" + currentGroup;
                    if (currentGroup.Length > 0)
                        row.Add(new Label(currentGroup));
                    var inlineHits = figures.Rendered.Count > 0 && currentGroup == "Hits";
                    if (inlineHits)
                        row.AddToClassList("effect-hit-controls");
                    (inlineHits ? footer : root).Add(row);
                }
                row.Add(EffectButton(option, choice));
            }
        }

        private void ArmyFigures(VisualElement root, ActionOption option)
        {
            var regions = option.EffectChoices.GroupBy(EffectChoiceDraft.FigureRegion).ToList();
            string Region() => draft.Payload.RegionId.Length > 0 ? draft.Payload.RegionId : regions.Count == 1 ? regions[0].Key : "";
            var heading = new Label { name = "army-figure-region" };
            root.Add(heading);
            refresh.Add(() => heading.text = Region().Length == 0 ? "Choose a highlighted region on the map."
                : RegionName(Region()) + (view.Decision.Presentation == "ArmyFigure" ? " · Choose one figure"
                    : " · " + draft.Payload.Ids.Count + " / " + option.Maximum + " selected"));
            foreach (var group in regions)
            {
                var row = BoardScreen.Row();
                refresh.Add(() => row.style.display = group.Key == Region() ? DisplayStyle.Flex : DisplayStyle.None);
                foreach (var choice in group)
                    row.Add(EffectButton(option, choice));
                root.Add(row);
            }
        }

        private void CompanionDestination(VisualElement root, ActionOption option)
        {
            var king = view.Characters.First(c => c.DefinitionId is "base.character.strider" or "base.character.aragorn");
            var row = BoardScreen.Row();
            row.Add(new Label(CharacterLibrary.Name(king.DefinitionId) + " travels"));
            foreach (var choice in option.EffectChoices.Where(c => c.SelectionGroup == "companions"))
                row.Add(EffectButton(option, choice));
            root.Add(row);
            BoardChoice(root, option);
        }

        private void RegionAlternatives(VisualElement root, ActionOption option)
        {
            var regions = option.EffectChoices.GroupBy(c => c.TargetId).ToList();
            foreach (var group in regions.Where(g => g.Key.Length > 0 && g.Count() > 1))
            {
                var row = BoardScreen.Row();
                foreach (var choice in group)
                    row.Add(EffectButton(option, choice));
                refresh.Add(() => row.style.display = regions.Count == 1 || draft.Payload.RegionId == group.Key
                    ? DisplayStyle.Flex : DisplayStyle.None);
                root.Add(row);
            }
        }

        private void BoardChoice(VisualElement root, ActionOption option)
        {
            var summary = new Label { name = "effect-board-choice" };
            root.Add(summary);
            refresh.Add(() =>
            {
                var selected = option.EffectChoices.FirstOrDefault(c => draft.Payload.Ids.Contains(c.Id) &&
                    (view.Decision.Presentation != "CompanionDestination" || c.SelectionGroup == "destination"));
                summary.text = view.Decision.Presentation == "ActionDie"
                    ? option.Maximum > 1 ? "Choose up to " + option.Maximum + " dice · " + draft.Payload.Ids.Count + " → Hunt"
                        : selected == null ? "Choose a Character die from your pool · C → W" : "Selected die · C → W"
                    : selected == null ? RegionPrompt(option) : selected.Title + (selected.TargetId.Length > 0 ? " → " + RegionName(selected.TargetId) : "");
                summary.tooltip = selected?.Detail ?? "";
            });
        }

        private string RegionPrompt(ActionOption option)
        {
            var region = draft.Payload.RegionId.Length > 0 ? draft.Payload.RegionId
                : option.EffectChoices.Select(c => c.TargetId).Distinct().Count() == 1 ? option.EffectChoices.First().TargetId : "";
            return region.Length > 0 && option.EffectChoices.Count(c => c.TargetId == region) > 1
                ? "Choose an Army · " + RegionName(region) : "Choose a highlighted region on the map";
        }

        private Button EffectButton(ActionOption option, EffectChoiceView choice)
        {
            var id = choice.Id;
            var reaction = option.Kind == CommandKind.RespondEffect;
            var source = choice.SourceId.Length > 0 ? choice.SourceId : id;
            var label = reaction ? (source.Contains(".character.") && !CardLibrary.Cards.ContainsKey(source)
                ? CharacterLibrary.Name(source) : CardLibrary.Title(source)) : ChoiceName(view, data, id);
            if (choice.Title.Length > 0)
                label = choice.Title;
            if (view.Decision?.Presentation == "DieAndResult" &&
                Enum.TryParse<DieFace>(choice.Title.Replace(" · Remove", ""), out var face))
                label = AssetCatalog.Current.Die(face) + (choice.Title.Contains(" · Remove") ? "*" : "");
            var die = view.Decision?.Presentation == "DieAndResult" ? view.Dice.FirstOrDefault(d => d.Id == id) : null;
            if (die != null && ExpansionLabels.Special(die))
                label += " · " + ExpansionLabels.DieSource(die);
            var leadership = view.Decision?.Presentation == "NazgulLeadership";
            if (leadership)
                label = (view.Characters.Any(c => c.Id == choice.SourceId) ? "WK" : "N") + " ◆";
            var compactCharacter = view.Decision?.Presentation == "FellowshipCompanions" ||
                ((choice.SelectionGroup is "Companion" or "Sacrifice" or "companions") || view.Decision?.Presentation is "LeaderOrCompanion" or "Companion") && view.Characters.Any(c => c.Id == id);
            var compactUnit = choice.SelectionGroup is "Sacrifice" or "recruits" || view.Decision?.Presentation is "ArmyFigure" or "ArmyFigures" or "ReserveFigures" or "LeaderOrCompanion"
                ? view.Units.FirstOrDefault(u => u.Id == id) : null;
            if (compactCharacter)
                label = CharacterLibrary.ShortName(view, id);
            if (compactUnit != null)
                label = FigureGrid.Symbol(compactUnit.Kind) + " " + FigureGrid.Name(compactUnit.Nation).Substring(0, 2);
            if (choice.TargetId.Length > 0 && compactUnit == null && !compactCharacter && view.Decision?.Presentation != "CorruptionRemoval")
                label += " → " + RegionName(choice.TargetId);
            var button = Choice(label, "effect-choice-" + id,
                () => reaction ? draft.Payload.ReactionId == id : draft.Payload.Ids.Contains(id),
                () => { if (reaction) draft.Change(p => p.ReactionId = id); else CardDraft.Choose(draft, option, id); });
            button.tooltip = choice.Title + (choice.Detail.Length == 0 ? "" : " · " + choice.Detail);
            if (die != null)
                button.tooltip = ExpansionLabels.DieDetail(die);
            if (leadership)
            {
                button.tooltip = choice.Title + " · " + choice.Detail;
                button.AddToClassList("effect-companion");
            }
            if (compactCharacter)
            {
                button.tooltip = CharacterLibrary.Name(view, id) + " · " + choice.Detail;
                button.AddToClassList("effect-companion");
            }
            if (compactUnit != null)
            {
                button.tooltip = FigureGrid.Name(compactUnit.Nation) + " " + compactUnit.Kind + " · " + choice.Detail;
                button.AddToClassList("effect-companion");
            }
            if (reaction || view.Decision?.Presentation is "NazgulStrike" or "CharacterCardDiscard" or "InspectedHand" or "TableCards" or "SmeagolCorruption")
                InspectSource(button, choice);
            if (reaction && view.Hunt != null)
            {
                button.AddToClassList("legal-target");
                if (view.DecisionOwner == view.You)
                    HuntReactionView.AddDetails(button, view, choice);
            }
            if (!reaction)
                refresh.Add(() => button.SetEnabled(EffectChoiceDraft.CanChoose(draft, option, id)));
            return button;
        }

        private void InspectSource(Button button, EffectChoiceView choice)
        {
            button.tooltip = choice.Detail.Length > 0 ? choice.Detail : choice.Cost == "DiscardTableCard" ? "Discard this table card to respond" : "Use this response";
            if (!CardLibrary.Cards.ContainsKey(choice.SourceId))
                return;
            inspector?.Observe(button, choice.SourceId,
                avoid: view.Hunt != null && draft.Kind == CommandKind.RespondEffect ? () => HuntReactionView.Panel(button) : null);
        }

        private Button Choice(string label, string name, Func<bool> selected, Action change)
        {
            var button = BoardScreen.Button(label, () => edit(change));
            button.name = name;
            refresh.Add(() => button.EnableInClassList("selected", selected()));
            return button;
        }

        private string RegionName(string id) => data.Regions.TryGetValue(id, out var region) ? region.Name : FigureGrid.Name(id);

        public static string ChoiceName(PlayerView view, BoardPresentation data, string id)
        {
            if (data.Regions.TryGetValue(id, out var region))
                return region.Name;
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == id);
            if (sovereign != null)
                return sovereign.Title;
            var unit = view.Units.FirstOrDefault(u => u.Id == id);
            if (unit != null)
                return FigureGrid.Symbol(unit.Kind) + " " + FigureGrid.Name(unit.Nation) + " " + unit.Kind;
            if (view.Characters.Any(c => c.Id == id))
                return CharacterLibrary.Name(view, id);
            var die = view.Battle?.Attacker.Dice.Concat(view.Battle.Defender.Dice).FirstOrDefault(d => d.Id == id);
            return die != null ? die.Result + (die.ConvertedHit ? " → Hit" : "") : CardLibrary.Title(id);
        }

        public static void Trail(VisualElement root, PlayerView view, BoardPresentation data, Action<string> inspect, CardInspector inspector = null) =>
            EffectTrailChoices.Render(root, view, data, inspect);

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
