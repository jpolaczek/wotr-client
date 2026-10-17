using System;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // MENU-11: one open game in the menu window — the free side's emblem, the game's name and who hosts it.
    // OBS-04: the row also says whether the game waits or is in progress, how many seats and observers it has, and
    // keeps Play and Observe in fixed places; the one not available is greyed out with the reason.
    public sealed class PublicRoomRow
    {
        public VisualElement Root { get; }
        private readonly VisualElement emblem = new VisualElement { pickingMode = PickingMode.Ignore };
        private readonly Label name = new Label { pickingMode = PickingMode.Ignore };
        private readonly Label detail = new Label { pickingMode = PickingMode.Ignore };
        private readonly Button play;
        private readonly Button observe;

        public PublicRoomRow(string id, Action<string, RoomRole> join)
        {
            Root = new VisualElement { name = "public-room-" + id };
            Root.AddToClassList("menu-row");
            Root.AddToClassList("public-room-row");
            emblem.AddToClassList("menu-row-emblem");
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList("menu-row-text");
            name.AddToClassList("menu-row-title");
            detail.AddToClassList("menu-row-detail");
            text.Add(name);
            text.Add(detail);
            play = RowButton("join-public-" + id, "Play", () => join(id, RoomRole.Player));
            observe = RowButton("observe-public-" + id, "Observe", () => join(id, RoomRole.Observer));
            Root.Add(emblem);
            Root.Add(text);
            Root.Add(play);
            Root.Add(observe);
        }

        private static Button RowButton(string buttonName, string text, Action click)
        {
            var button = BoardScreen.Button(text, click);
            button.name = buttonName;
            button.AddToClassList("public-room-action");
            return button;
        }

        public void Apply(PublicRoomSummary room)
        {
            var free = room.OpenSide == Side.FreePeoples;
            emblem.EnableInClassList("side-free", free);
            emblem.EnableInClassList("side-shadow", !free);
            name.text = room.Name;
            detail.text = Detail(room);
            Root.tooltip = room.Name + " · " + detail.text;
            play.SetEnabled(room.CanPlay);
            play.tooltip = room.CanPlay ? "Join as " + BoardScreen.SideName(room.OpenSide) + "."
                : room.Status == RoomStatus.Started ? "The game has started." : "Both seats are taken.";
            observe.SetEnabled(room.CanObserve);
            observe.tooltip = room.CanObserve ? "Watch this game without playing." : "All observer places are taken.";
        }

        public static string Detail(PublicRoomSummary room)
        {
            var observers = "Observers " + room.Observers + "/" + RoomLimits.Observers;
            return room.Status == RoomStatus.Started
                ? "Hosted by " + room.Host + " · In progress · " + observers
                : "Hosted by " + room.Host + " · Waiting · Seats " + room.Players + "/2 · " + observers;
        }
    }

    // OBS-01: a room takes two players and up to sixteen observers.
    public static class RoomLimits
    {
        public const int Observers = 16;
    }
}
