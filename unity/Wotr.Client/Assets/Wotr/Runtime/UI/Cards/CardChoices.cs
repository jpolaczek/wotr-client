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
        private readonly CardInspector inspector;
        private readonly Func<VisualElement> reinforcements;

        public CardChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data,
            CardInspector inspector = null, Func<VisualElement> reinforcements = null)
        {
            this.data = data;
            this.reinforcements = reinforcements;
            this.inspector = inspector;
            this.view = view;
            this.draft = draft;
            this.edit = edit;
            var option = DraftOptions.For(view, draft);
            // UI-289: an Event is played from its hand card; its nation and its use are chosen on the board (the tray's
            // DirectCard), so the tray lists neither.
            if (draft.Kind == CommandKind.PlayEvent)
                return;
            if (draft.Kind == CommandKind.DiscardProtection)
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
            // UI-215/UI-289: a card's figures and the King's companions are chosen in the region panel and on the map
            // (CardFigureDraft), never in the tray.
            if (view.Decision?.Presentation is "ArmyFigure" or "ArmyFigures" or "CompanionDestination")
                return;
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
            var currentGroup = "";
            VisualElement row = null;
            foreach (var id in option.EligibleIds)
            {
                // UI-245: a battle die or figure is chosen on itself in the Battle window; UI-249: a Companion of The
                // Breaking of the Fellowship on its portrait in the Hunt panel; UI-247: a battle reaction on its portrait.
                if (BattleObjectChoices.Offered(view, id) || FellowshipActions.RosterChoice(view, id) ||
                    BattleReactions.OnPortrait(view, id))
                    continue;
                // UI-270: an alternate companion's Guide response is a response object in the Hunt's response row; UI-272: so
                // is a Lords table card's response, and We Won't Go Back's Character cards glow in the hand.
                if (HuntGuideResponses.InRow(view, id) || HuntCardResponses.InRow(view, id) ||
                    view.Decision?.Presentation == HuntCardResponses.Smeagol && view.Hand.Contains(id) || InspectedHand.Contains(view, id))
                    continue;
                // UI-268: Nenya's response is its glowing ring socket in the console.
                if (NamedRings.OnSocket(view, id))
                {
                    var hint = new Label("Nenya can replace this Eye tile: click the glowing Nenya in the console.") { name = "nenya-socket-hint" };
                    hint.AddToClassList("draft-status");
                    root.Add(hint);
                    continue;
                }
                var choice = option.EffectChoices.FirstOrDefault(c => c.Id == id) ?? new EffectChoiceView { Id = id };
                if (row == null || choice.SelectionGroup != currentGroup)
                {
                    currentGroup = choice.SelectionGroup;
                    row = BoardScreen.Row();
                    row.name = "effect-group-" + currentGroup;
                    if (currentGroup.Length > 0)
                        row.Add(new Label(currentGroup));
                    root.Add(row);
                }
                row.Add(EffectButton(option, choice));
            }
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
                var selected = option.EffectChoices.FirstOrDefault(c => draft.Payload.Ids.Contains(c.Id));
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
                ((choice.SelectionGroup is "Companion" or "Sacrifice") || view.Decision?.Presentation is "LeaderOrCompanion" or "Companion") && view.Characters.Any(c => c.Id == id);
            var compactUnit = choice.SelectionGroup is "Sacrifice" or "recruits" || view.Decision?.Presentation is "ReserveFigures" or "LeaderOrCompanion"
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
            // UI-285: a Hunt response sends with its click, so it pins no card that would read as a confirmation still
            // waiting; its tooltip says what it does and the Effects panel shows the card.
            if (!CardLibrary.Cards.ContainsKey(choice.SourceId) || view.Hunt != null && draft.Kind == CommandKind.RespondEffect)
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

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
