using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ExpansionChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly TurnDraft draft;
        private readonly Action<Action> edit;

        public ExpansionChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit,
            BoardPresentation data, VersionComparison versions = null)
        {
            this.draft = draft;
            this.edit = edit;
            var option = DraftOptions.For(view, draft);
            if (CouncilDraft.Handles(draft.Kind))
            {
                var council = new CouncilChoices(root, view, draft, edit, data, versions);
                refresh.Add(council.Refresh);
                return;
            }
            if (draft.Kind == CommandKind.UseCouncilToken)
            {
                Council(root, view, data);
                return;
            }
            var sovereignContext = SovereignPresentation.OwnsDecision(view, draft);
            if (!sovereignContext && draft.Kind is CommandKind.AwakenSovereign or CommandKind.RemoveCorruptedSovereign or CommandKind.AttemptCorruption)
                Sovereigns(root, view, option);
            // UI-268: Vilya's die is the lit die in the pool.
            if (!sovereignContext && draft.Kind != CommandKind.UseVilya)
                Dice(root, view, option);
            if (draft.Kind == CommandKind.UseDismay)
                Dismay(root, view, option);
            if (draft.Kind == CommandKind.AwakenSovereign && draft.Payload.CharacterId.Length > 0)
            {
                var travel = new FellowshipChoices(root, view, draft, edit, data);
                refresh.Add(travel.Refresh);
            }
            if (draft.Kind == CommandKind.UseNarya)
                root.Add(new Label("Narya → " + string.Join(" · ", option.Targets.Select(FigureGrid.Name)) + " at war"));
        }

        private void Dice(VisualElement root, PlayerView view, ActionOption option)
        {
            if (view.Decision.Kind is DecisionKind.SpecialDieChoice or DecisionKind.RulerDiscard)
                return;
            var row = BoardScreen.Row("expansion-dice");
            root.Add(row);
            foreach (var id in option.EligibleDice)
            {
                var die = view.Dice.Single(d => d.Id == id);
                var origin = (die.Source == "ruler" || die.Owner != view.You ? die.Owner == Side.FreePeoples ? "FP " : "SH " : "") +
                    (ExpansionLabels.Special(die) ? ExpansionLabels.DieSource(die) : "");
                var button = Choice(ExpansionLabels.DieFace(die) + (origin.Length > 0 ? "\n" + origin : ""), "cost-" + id,
                    () => draft.Payload.DieId == id, () => draft.Change(p => p.DieId = id));
                button.AddToClassList("expansion-die");
                button.EnableInClassList("special-die", ExpansionLabels.Special(die));
                button.tooltip = ExpansionLabels.DieDetail(die);
                button.userData = new DraftToken(DraftObject.Die, id);
                refresh.Add(() => button.SetEnabled(draft.Kind != CommandKind.AwakenSovereign || draft.Payload.CharacterId.Length == 0 ||
                    option.CharacterActions.Any(c => c.CharacterId == draft.Payload.CharacterId && c.Dice.Contains(id))));
                row.Add(button);
            }
        }

        private void Sovereigns(VisualElement root, PlayerView view, ActionOption option)
        {
            var row = BoardScreen.Row("sovereign-choices");
            root.Add(row);
            foreach (var sovereign in view.Sovereigns.Where(s => option.EligibleIds.Contains(s.Id)))
            {
                var button = Choice(sovereign.Title, "sovereign-choice-" + sovereign.Id,
                    () => draft.Payload.CharacterId == sovereign.Id, () => draft.Change(p =>
                    {
                        p.CharacterId = sovereign.Id;
                        p.Route.Clear();
                    }));
                button.tooltip = sovereign.Status + " · Resistance " + sovereign.Resistance;
                row.Add(button);
            }
        }

        private void Dismay(VisualElement root, PlayerView view, ActionOption option)
        {
            var row = BoardScreen.Row();
            root.Add(row);
            foreach (var id in option.EligibleIds)
                row.Add(Choice("Dismay · " + ExpansionLabels.Token(view, id), "dismay-" + id,
                    () => draft.Payload.Ids.Contains(id), () => CardDraft.Choose(draft, id)));
        }

        private void Council(VisualElement root, PlayerView view, BoardPresentation data)
        {
            var tokens = BoardScreen.Row();
            root.Add(tokens);
            foreach (var option in view.Options.Where(o => o.Kind == CommandKind.UseCouncilToken && o.Enabled))
                foreach (var id in option.EligibleIds)
                    tokens.Add(Choice(ExpansionLabels.CouncilToken(id), "council-use-" + id,
                        () => draft.Payload.AbilityId == id, () => ExpansionDraft.CouncilToken(draft, id)));
            if (draft.Payload.AbilityId == ExpansionDraft.CouncilTravel)
            {
                var travel = new FellowshipChoices(root, view, draft, edit, data, tokens);
                refresh.Add(travel.Refresh);
            }
            if (draft.Payload.AbilityId == ExpansionDraft.CouncilPolitics)
                root.Add(new Label("Politics → click a nation’s arrow"));
        }

        private Button Choice(string label, string name, Func<bool> selected, Action change)
        {
            var button = BoardScreen.Button(label, () => edit(change));
            button.name = name;
            refresh.Add(() =>
            {
                button.EnableInClassList("selected", selected());
                // UI-345: a choice not yet taken glows (ChoiceGlow) and pulses with its row.
                ChoiceGlow.Apply(button, !selected());
            });
            return button;
        }

        public void Refresh()
        {
            foreach (var update in refresh)
                update();
        }
    }
}
