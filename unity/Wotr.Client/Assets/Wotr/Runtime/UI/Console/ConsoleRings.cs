using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-160: three sockets, one for each Elven Ring of the game: with the Free Peoples, captured by the Shadow,
    // or removed from the game. The number of sockets never changes; a ring the player can use now is enabled for
    // the step's ring choice (ExpansionActions.RingKind): changing a die, Vilya or the cost of the selected card.
    // UI-268: a Lords game's named rings keep fixed sockets (Vilya, Nenya, Narya) in their own art; a socket glows
    // (keeper-ready) while its Keeper ability is offered now — Nenya in the Hunt's tile step, Vilya after a paid action.
    public sealed class ConsoleRings
    {
        public const int Count = 3;
        public const string KeeperReadyClass = "keeper-ready";
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

        // option: the server's option for the ring choice of this step, of the given kind; chosen: Rings a card's step
        // has picked so far (UI-272: Three Rings for the Elven-Kings).
        public void Apply(PlayerView view, string armed, bool enabled, ActionOption option, CommandKind kind,
            IReadOnlyCollection<string> chosen = null)
        {
            var ordered = view.Rings.OrderBy(r => NamedRings.Place(r.Id)).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
            for (var index = 0; index < Count; index++)
            {
                var ring = index < ordered.Count ? ordered[index] : null;
                shown[index] = ring?.Id ?? "";
                var keeper = enabled && ring != null && ring.Owner == view.You && NamedRings.KeeperReady(view, ring.Id);
                var usable = keeper || enabled && ring != null && option is { Enabled: true } && option.EligibleIds.Contains(ring.Id);
                // A card may recover a Ring the Shadow holds or one out of play, so its socket answers too.
                ids[index] = ring != null && (ring.Owner == view.You || usable) ? ring.Id : "";
                var socket = sockets[index];
                Name(socket, ring == null ? "" : NamedRings.Key(ring.Id), keeper);
                if (applied && ring != null && owners[index] != ring.Owner)
                    Pulse(socket);
                owners[index] = ring?.Owner;
                socket.EnableInClassList("ring-free", ring?.Owner == Side.FreePeoples);
                socket.EnableInClassList("ring-captured", ring?.Owner == Side.Shadow);
                socket.EnableInClassList("ring-removed", ring?.Owner == null);
                socket.EnableInClassList("selected", ring != null && (ring.Id == armed || chosen?.Contains(ring.Id) == true));
                socket.EnableInClassList("legal-target", usable);
                socket.SetEnabled(usable);
                socket.tooltip = keeper ? Name(ring) + "\n" + NamedRings.UseNow(ring.Id) : Tooltip(view, ring, usable, option, kind);
            }
            applied = true;
        }

        // UI-268: the socket's art follows its named ring; keeper-ready lights it.
        private static void Name(Button socket, string key, bool keeper)
        {
            socket.EnableInClassList("ring-named", key.Length > 0);
            foreach (var name in NamedRings.Keys)
                socket.EnableInClassList("ring-" + name, key == name);
            socket.EnableInClassList(KeeperReadyClass, keeper);
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
            if (usable)
                return Name(ring) + "\n" + Use(kind);
            if (ring.Owner == null)
                return ExpansionLabels.Ring(ring.Id) + " · Removed from the game";
            var reason = ring.Owner != view.You ? "" : option == null ? "Not usable now" : option.Reason;
            var keeper = ring.Owner == Side.FreePeoples ? NamedRings.Keeper(ring.Id) : "";
            return Name(ring) + (reason.Length > 0 ? "\n" + reason : "") + (keeper.Length > 0 ? "\n" + keeper : "");
        }

        private static string Use(CommandKind kind) => kind switch
        {
            CommandKind.UseRing => "Click, then a die to change its result",
            CommandKind.UseVilya => "Click to use it on the kept die",
            CommandKind.ChooseEffect => "Click to recover this Ring for the Free Peoples",
            _ => "Click to pay the card's cost with this ring"
        };

        private static string Name(RingView ring) => ExpansionLabels.Ring(ring.Id) + " · " +
            (ring.Owner == Side.FreePeoples ? "Free Peoples" : ring.Owner == Side.Shadow ? "Shadow (captured)" : "Removed from the game");
    }
}
