using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // LOBBY-01: what the menu knows of the lobby between polls — the chat's messages in order (at most the server's
    // MessagesKept), the players online, the incoming challenge to show, and which outcomes of this player's own
    // challenges were already acted on, so an accepted one opens its room once and a refusal is told once.
    public sealed class LobbyState
    {
        private readonly List<LobbyMessage> messages = new List<LobbyMessage>();
        private readonly HashSet<string> handled = new HashSet<string>();
        private readonly HashSet<string> dismissed = new HashSet<string>();

        public IReadOnlyList<LobbyMessage> Messages => messages;
        public IReadOnlyList<OnlinePlayer> Online { get; private set; } = new List<OnlinePlayer>();
        public long Latest { get; private set; }
        public ChallengeView Incoming { get; private set; }

        // Applies a poll; returns the outcomes of own challenges that are new since the last poll.
        public IReadOnlyList<ChallengeView> Apply(LobbyView view)
        {
            AddMessages(view.Messages);
            Latest = System.Math.Max(Latest, view.Latest);
            Online = view.Online.OrderBy(player => player.Name, System.StringComparer.OrdinalIgnoreCase).ToList();
            Incoming = view.Incoming.FirstOrDefault(challenge => challenge.Status == ChallengeStatus.Pending && !dismissed.Contains(challenge.Id));
            return view.Outgoing.Where(challenge => challenge.Status != ChallengeStatus.Pending && handled.Add(challenge.Id)).ToList();
        }

        // The player answered or closed the tab; the same challenge does not come back.
        public void Dismiss(string challenge)
        {
            dismissed.Add(challenge);
            if (Incoming?.Id == challenge)
                Incoming = null;
        }

        public void AddMessages(IEnumerable<LobbyMessage> added)
        {
            foreach (var message in added.OrderBy(message => message.Sequence))
            {
                if (messages.Count > 0 && message.Sequence <= messages[messages.Count - 1].Sequence)
                    continue;
                messages.Add(message);
            }
            if (messages.Count > LobbyLimits.MessagesKept)
                messages.RemoveRange(0, messages.Count - LobbyLimits.MessagesKept);
            if (messages.Count > 0)
                Latest = System.Math.Max(Latest, messages[messages.Count - 1].Sequence);
        }

        public static string Outcome(ChallengeView challenge) => challenge.Status switch
        {
            ChallengeStatus.Accepted => challenge.To + " accepted your challenge. Opening the table…",
            ChallengeStatus.Declined => challenge.To + " declined your challenge.",
            ChallengeStatus.Expired => challenge.To + " did not answer your challenge in time.",
            _ => ""
        };

        public static string Refusal(string error, string name) => error switch
        {
            "PlayerOffline" => name + " is no longer online.",
            "AlreadyChallenged" => "There is already a challenge between you and " + name + ".",
            "TooManyChallenges" => "Wait for an answer to your other challenges first.",
            "SelfChallenge" => "You cannot challenge yourself.",
            "PlayerNotFound" => "There is no player named " + name + ".",
            "ChallengeClosed" => "That challenge is no longer open.",
            "ChallengeNotFound" => "That challenge is no longer open.",
            "RoomUnavailable" => "The table could not be made. Try the challenge again.",
            "InvalidMessage" => "Messages are 1–" + LobbyLimits.LongestMessage + " characters.",
            "TooManyRequests" => "Too many requests. Wait a moment.",
            _ => "That did not work. Try again."
        };
    }
}
