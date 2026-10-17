using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // AUDIO-04 (AU05, AU06): one sound per completed gesture, the same for the mouse and Enter/Space; a release outside,
    // a drag, a disabled control and a plain hover stay silent; a gesture that changes the card look rustles instead of
    // clicking; one that sends a command, or that the board ignores, leaves the sound to the result.
    public sealed class GestureSoundsTests
    {
        private static List<SoundCue> Record(MountedMatchClient client)
        {
            var played = new List<SoundCue>();
            client.Interface.Sounds.Policy.Played += played.Add;
            return played;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 4; i++)
                yield return null;
        }

        private static void Key(VisualElement target, KeyCode key, bool down)
        {
            var value = new Event { type = down ? EventType.KeyDown : EventType.KeyUp, keyCode = key };
            if (down)
            {
                using var e = KeyDownEvent.GetPooled(value);
                target.SendEvent(e);
            }
            else
            {
                using var e = KeyUpEvent.GetPooled(value);
                target.SendEvent(e);
            }
        }

        private static Button Probe(MountedMatchClient client, string name, System.Action action)
        {
            var button = BoardScreen.Button(name, action);
            button.name = name;
            button.style.position = Position.Absolute;
            button.style.left = 400;
            button.style.top = 300;
            button.style.width = 120;
            button.style.height = 40;
            client.Board.Root.Add(button);
            return button;
        }

        [UnityTest]
        public IEnumerator CompletedGesturesSoundOnceAndEverythingElseStaysSilent()
        {
            using var client = new MountedMatchClient("http://localhost", "gesture-sounds");
            client.Apply(new BoardFixture().View);
            yield return Settle();
            var played = Record(client);
            var clicks = 0;
            var probe = Probe(client, "sound-probe", () => clicks++);
            yield return Settle();

            Move(probe, probe.worldBound.center);
            yield return Settle();
            CollectionAssert.IsEmpty(played, "Hover is silent.");

            PointerGestureEvents.ClickAt(probe);
            yield return Settle();
            CollectionAssert.AreEqual(new[] { SoundCue.UiClick }, played, "A click sounds once.");
            Assert.AreEqual(1, clicks);

            yield return new WaitForSecondsRealtime(0.15f);
            probe.Focus();
            Key(probe, KeyCode.Return, true);
            Key(probe, KeyCode.Return, false);
            yield return Settle();
            CollectionAssert.AreEqual(new[] { SoundCue.UiClick, SoundCue.UiClick }, played, "Enter is the same gesture.");

            yield return new WaitForSecondsRealtime(0.15f);
            PointerGestureEvents.Press(probe);
            var outside = probe.worldBound.center + new Vector2(0, 200);
            Move(probe, outside);
            Up(probe, outside);
            yield return Settle();
            Assert.AreEqual(2, played.Count, "A release outside the control is silent.");

            probe.SetEnabled(false);
            PointerGestureEvents.Press(probe);
            PointerGestureEvents.Release(probe);
            yield return Settle();
            Assert.AreEqual(2, played.Count, "A disabled control is silent.");
            probe.SetEnabled(true);

            var card = System.Linq.Enumerable.First(CardLibrary.Cards.Keys);
            var look = Probe(client, "look-probe", () => client.Board.Cards.Show(card, true));
            look.style.top = 360;
            yield return Settle();
            PointerGestureEvents.ClickAt(look);
            yield return Settle();
            Assert.AreEqual(SoundCue.CardInspect, played[played.Count - 1], "Opening a card look rustles instead of clicking.");
            Assert.AreEqual(3, played.Count);

            var send = Probe(client, "send-probe", client.Interface.Sounds.CommandSent);
            send.style.top = 420;
            yield return Settle();
            yield return new WaitForSecondsRealtime(0.3f);
            PointerGestureEvents.ClickAt(send);
            yield return Settle();
            Assert.AreEqual(3, played.Count, "A gesture that sends a command leaves the sound to its result.");

            client.Interface.Sounds.GestureIgnored = () => true;
            PointerGestureEvents.ClickAt(probe);
            yield return Settle();
            Assert.AreEqual(3, played.Count, "A click the board ignores is silent.");
        }

        private static void Move(VisualElement target, Vector2 point)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            target.panel.visualTree.SendEvent(move);
        }

        private static void Up(VisualElement target, Vector2 point)
        {
            using var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
            target.panel.visualTree.SendEvent(up);
        }
    }
}
