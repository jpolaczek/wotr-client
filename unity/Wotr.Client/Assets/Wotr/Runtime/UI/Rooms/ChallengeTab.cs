using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // LOBBY-01: a challenge to this player, over the menu's window on any page: who challenges, Accept and Decline.
    // Accepting makes a private room with the challenger as host and this player in the other seat.
    public sealed class ChallengeTab
    {
        public VisualElement Root { get; } = new VisualElement { name = "challenge-tab" };
        public bool IsShown => Root.style.display.value == DisplayStyle.Flex;
        public string ChallengeId { get; private set; } = "";
        public event Action<string> Accept;
        public event Action<string> Decline;
        private readonly Label text = new Label { name = "challenge-text", pickingMode = PickingMode.Ignore };
        private readonly Button accept;
        private readonly Button decline;

        public ChallengeTab()
        {
            Root.AddToClassList("challenge-tab");
            text.AddToClassList("challenge-text");
            Root.Add(text);
            accept = BoardScreen.Button("Accept", () => Answer(Accept));
            accept.name = "challenge-accept";
            accept.AddToClassList("challenge-action");
            decline = BoardScreen.Button("Decline", () => Answer(Decline));
            decline.name = "challenge-decline";
            decline.AddToClassList("challenge-action");
            Root.Add(accept);
            Root.Add(decline);
            Root.style.display = DisplayStyle.None;
        }

        public void Show(ChallengeView challenge)
        {
            if (challenge == null)
            {
                Hide();
                return;
            }
            ChallengeId = challenge.Id;
            text.text = challenge.From + " challenges you to a game";
            accept.SetEnabled(true);
            decline.SetEnabled(true);
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        public void Hide()
        {
            ChallengeId = "";
            Root.style.display = DisplayStyle.None;
        }

        private void Answer(Action<string> answer)
        {
            if (ChallengeId.Length == 0)
                return;
            accept.SetEnabled(false);
            decline.SetEnabled(false);
            answer?.Invoke(ChallengeId);
        }
    }
}
