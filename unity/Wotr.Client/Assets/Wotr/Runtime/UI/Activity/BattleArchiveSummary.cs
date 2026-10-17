using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattleArchiveSummary
    {
        public VisualElement Root { get; } = new VisualElement { name = "archived-battle" };
        public BattleReportView Displayed { get; private set; }
        public event System.Action<BattleReportView> Opened;
        private readonly BoardPresentation data;
        public BattleArchiveSummary(BoardPresentation data) { this.data = data; Root.AddToClassList("action-card-step"); }

        public void Apply(BattleReportView report)
        {
            if (Displayed == report) return;
            Displayed = report;
            Root.Clear();
            Root.style.display = report == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (report == null) return;
            var open = BoardScreen.Button("View battle rounds", () => Opened?.Invoke(report));
            open.name = "open-archived-battle";
            open.AddToClassList("battle-direct");
            open.Add(BattleChoices.DirectIcon("continue"));
            Root.Add(open);
            Add("Battle report · " + ActionCardText.Words(report.Status.ToString()));
            if (report.PartialHistory) Add("Earlier details unavailable");
            var initial = report.Initial;
            if (initial != null)
            {
                Add(ActionCardText.Region(initial.RegionId, data) + " · " + initial.Mode);
                Force("Attacker", initial.Attacker);
                Force("Defender", initial.Defender);
            }
            foreach (var round in report.Rounds)
            {
                Add("Round " + round.Number + (round.Completed ? " · Completed" : " · In progress"));
                var result = round.Result;
                if (result != null)
                    Add(ActionCardText.Actor(result.Attacker.Side) + " " + result.Attacker.Hits + " hits · " +
                        ActionCardText.Actor(result.Defender.Side) + " " + result.Defender.Hits + " hits");
            }
            if (report.Place is { } place)
                Add("Recorded control · " + (place.Controller.HasValue ? ActionCardText.Actor(place.Controller) : "Neutral"));
            if (report.Outcome is { } outcome)
                Add("Victory points · Free Peoples " + outcome.FreePeoplesPoints + " · Shadow " + outcome.ShadowPoints);
        }

        private void Force(string role, BattleForceView force)
        {
            Add(role + " · " + ActionCardText.Actor(force.Side));
            foreach (var group in force.Units.GroupBy(u => (u.Nation, u.Kind)))
                Add(group.Count() + "× " + FigureGrid.Symbol(group.Key.Kind) + " " + group.Key.Kind + " · " + FigureGrid.Name(group.Key.Nation));
            foreach (var character in force.Characters)
                Add(CharacterLibrary.Name(character.DefinitionId));
        }

        private void Add(string text)
        {
            var label = new Label(text) { enableRichText = false };
            label.AddToClassList("action-detail");
            Root.Add(label);
        }
    }
}
