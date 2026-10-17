using System;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-160: three sockets, one for each Elven Ring of the game: with the Free Peoples, captured by the Shadow,
    // or removed from the game. The number of sockets never changes; a ring the player can use now is enabled for
    // the step's ring choice (ExpansionActions.RingKind): changing a die, Vilya or the cost of the selected card.
    public sealed class ConsoleRings
    {
        public const int Count = 3;
        public VisualElement Root { get; } = new VisualElement { name = "console-rings" };
        public event Action<string> Chosen;
        private readonly Button[] sockets = new Button[Count];
        private readonly VisualElement[] rings = new VisualElement[Count];
        private readonly string[] ids = new string[Count];
        private readonly string[] shown = new string[Count];
        private readonly Side?[] owners = new Side?[Count];
        private bool applied;

        public ConsoleRings()
        {
            Root.AddToClassList("console-rings");
            for (var index = 0; index < Count; index++)
            {
                var position = index;
                var socket = new Button(() => { if (ids[position].Length > 0) Chosen?.Invoke(ids[position]); })
                    { name = "console-ring-" + index };
                socket.AddToClassList("console-ring-socket");
                var ring = new VisualElement { name = "console-ring-image", pickingMode = PickingMode.Ignore };
                ring.AddToClassList("console-ring-image");
                socket.Add(ring);
                Root.Add(socket);
                sockets[index] = socket;
                rings[index] = ring;
                ids[index] = shown[index] = "";
            }
        }

        public Button Socket(int index) => sockets[index];

        // The socket that shows the ring, or null when the game has no such ring.
        public Button For(string ringId)
        {
            var index = Array.IndexOf(shown, ringId);
            return index < 0 ? null : sockets[index];
        }

        // option: the server's option for the ring choice of this step, of the given kind.
        public void Apply(PlayerView view, string armed, bool enabled, ActionOption option, CommandKind kind)
        {
            var ordered = view.Rings.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();
            for (var index = 0; index < Count; index++)
            {
                var ring = index < ordered.Count ? ordered[index] : null;
                shown[index] = ring?.Id ?? "";
                ids[index] = ring?.Owner == view.You ? ring.Id : "";
                var usable = enabled && ring != null && option is { Enabled: true } && option.EligibleIds.Contains(ring.Id);
                var socket = sockets[index];
                if (applied && ring != null && owners[index] != ring.Owner)
                    Pulse(socket);
                owners[index] = ring?.Owner;
                socket.EnableInClassList("ring-free", ring?.Owner == Side.FreePeoples);
                socket.EnableInClassList("ring-captured", ring?.Owner == Side.Shadow);
                socket.EnableInClassList("ring-removed", ring?.Owner == null);
                socket.EnableInClassList("selected", ring != null && ring.Id == armed);
                socket.EnableInClassList("legal-target", usable);
                socket.SetEnabled(usable);
                socket.tooltip = Tooltip(view, ring, usable, option, kind);
            }
            applied = true;
        }

        // UI-161: a ring that changes hands (Free Peoples → Shadow, or removed) pulses once in its socket.
        private static void Pulse(Button socket)
        {
            socket.AddToClassList("ring-transferred");
            socket.schedule.Execute(() => socket.RemoveFromClassList("ring-transferred")).StartingIn(360);
        }

        private static string Tooltip(PlayerView view, RingView ring, bool usable, ActionOption option, CommandKind kind)
        {
            if (ring == null)
                return "Elven Ring · Not in this game";
            if (ring.Owner == null)
                return ExpansionLabels.Ring(ring.Id) + " · Removed from the game";
            if (usable)
                return Name(ring) + "\n" + Use(kind);
            var reason = ring.Owner != view.You ? "" : option == null ? "Not usable now" : option.Reason;
            return Name(ring) + (reason.Length > 0 ? "\n" + reason : "");
        }

        private static string Use(CommandKind kind) => kind switch
        {
            CommandKind.UseRing => "Click, then a die to change its result",
            CommandKind.UseVilya => "Click to use it on the kept die",
            _ => "Click to pay the card's cost with this ring"
        };

        private static string Name(RingView ring) =>
            ExpansionLabels.Ring(ring.Id) + " · " + (ring.Owner == Side.FreePeoples ? "Free Peoples" : "Shadow (captured)");
    }
}
