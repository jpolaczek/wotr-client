using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BattlePolicyControl
    {
        public VisualElement Root { get; } = new VisualElement { name = "battle-policy" };
        public Button Toggle { get; }
        public event Action Chosen;
        private readonly Label status = new Label { name = "battle-policy-status" };

        public BattlePolicyControl()
        {
            Root.AddToClassList("battle-policy");
            Toggle = BoardScreen.Button("Skip cards this battle", () => Chosen?.Invoke());
            Toggle.name = "battle-policy-toggle";
            Toggle.Add(BattleChoices.DirectIcon());
            Root.Add(Toggle);
            Root.Add(status);
            Root.style.display = DisplayStyle.None;
        }

        public void Apply(PlayerView view, bool available, bool sending, bool uncertain, string notice)
        {
            Root.style.display = view.Battle != null || uncertain ? DisplayStyle.Flex : DisplayStyle.None;
            var own = view.AutoDecision;
            var active = own.Policy?.BattleId == view.Battle?.Id && own.Policy != null;
            Toggle.text = uncertain ? "Check card setting" : active ? "Ask for cards" : "Skip cards this battle";
            Toggle.EnableInClassList("selected", active);
            var ended = view.Battle == null || view.Battle.Stage is "Finish" or "Advance" or "Complete";
            Toggle.SetEnabled(available && !sending && (uncertain || active || !ended));
            Toggle.tooltip = "Private setting. Skip your combat cards in this battle until your hand changes. " +
                "Ask for cards cancels future skips; a committed choice stays committed.";
            status.text = sending ? "Saving…" : notice.Length > 0 ? notice :
                own.LastSkippedBattleId == view.Battle?.Id ? "No card · Round " + own.LastSkippedRound : "";
            status.tooltip = status.text;
        }
    }
}
