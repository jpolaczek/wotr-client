using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class CardNetworkTests
    {
        [UnityTest]
        public IEnumerator CardChoicesDrawDiscardAndTableEffectsUseTheProductionServer()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            var task = Run(fixture);
            var deadline = Time.realtimeSinceStartup + 90;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Card integration timed out.");
            task.GetAwaiter().GetResult();
        }

        private static async Task Run(Fixture fixture)
        {
            using (var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(80)))
            using (var fp = new MountedMatchClient(fixture.Address, fixture.CardsMatch))
            using (var sh = new MountedMatchClient(fixture.Address, fixture.CardsMatch))
            {
                var token = lifetime.Token;
                await fp.Api.Login(fixture.FreePeoplesUser, fixture.Password, token);
                await sh.Api.Login(fixture.ShadowUser, fixture.Password, token);
                fp.Connection.Start();
                sh.Connection.Start();
                await Until(() => fp.Connected > 0 && sh.Connected > 0, token);
                await RecruitAndRestore(fp, sh, token);
                await Play(sh, "base.sh.strategy.05", DieFace.Muster, token);
                await BothAt(fp, sh, 3, token);
                Assert.AreEqual("base.sh.strategy.05", fp.Board.View.Effects.Single().SourceId);
                Assert.AreEqual(1, fp.Board.TableEffects.Root.Query<Button>().ToList().Count(b => b.name.StartsWith("effect-source-")));
                await DrawAndDiscard(fp, sh, token);
                await DiscardDie(sh, token);
                await BothAt(fp, sh, 6, token);
                var before = fp.Board.View.Nations.Single(n => n.Id == "north").StepsToWar;
                Click(fp.Board.OwnDice.Q<Button>(fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Muster).Id));
                await Task.Delay(60, token);
                await CardGestures.Open(fp);
                CardGestures.PlayClick(fp, "base.fp.strategy.08");
                await Task.Delay(60, token);
                Assert.IsNull(fp.Board.Tray.Q<Button>("confirm-decision"));
                var die = fp.Board.ActionDie.DieId;
                Click(fp.Board.Console.Actions.Back);
                await Task.Delay(60, token);
                Assert.AreEqual(die, fp.Board.ActionDie.DieId);
                Assert.AreEqual(6, fp.Board.View.Revision);
                await CardGestures.Open(fp);
                CardGestures.PlayClick(fp, "base.fp.strategy.08");
                await Task.Delay(140, token);
                // UI-148: Wisdom of Elrond waits for a nation, so Politics opens without a click.
                Assert.AreEqual(DisplayStyle.Flex, fp.Board.Nations.Drawer.resolvedStyle.display);
                Click(fp.Board.Nations.Drawer.Q<Button>("nation-status-north"));
                await BothAt(fp, sh, 7, token);
                Assert.AreEqual(before - 1, fp.Board.View.Nations.Single(n => n.Id == "north").StepsToWar);
                Assert.AreEqual(1, fp.Board.View.Effects.Count, "Event advancement does not expire Threats and Promises.");
                await DiscardDie(sh, token);
                await BothAt(fp, sh, 8, token);
                await Play(fp, "base.fp.character.12", DieFace.Character, token);
                await BothAt(fp, sh, 9, token);
                Assert.AreEqual(1, fp.Board.View.Fellowship.Corruption);
                Assert.AreEqual(4, fp.Board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            }
        }

        private static async Task RecruitAndRestore(MountedMatchClient fp, MountedMatchClient sh, CancellationToken token)
        {
            await Play(fp, "base.fp.strategy.15", DieFace.Event, token);
            await BothAt(fp, sh, 1, token);
            Assert.AreEqual(DecisionKind.EffectChoice, fp.Board.View.Decision.Kind);
            Assert.AreEqual(1, fp.Board.Console.Status.Trail.Query<Button>(className: "committed-choice").ToList().Count);
            Assert.IsNull(sh.Board.View.Decision);
            Assert.IsEmpty(sh.Board.View.EffectTrail);
            var connections = fp.Connected;
            fp.Connection.Reconnect();
            await Until(() => fp.Connected > connections, token);
            // UI-147: a played card opens no window with its text, before or after a reconnect.
            Assert.IsFalse(fp.Board.Cards.Pinned);
            fp.Board.Map.Focus("lorien");
            Assert.IsEmpty(fp.Board.Cards.CardId);
            var option = fp.Board.View.Options.Single(o => o.Kind == CommandKind.ChooseEffect);
            var elite = option.EligibleIds.First(id => fp.Board.View.Units.Single(u => u.Id == id).Kind == UnitKind.Elite);
            // UI-131: the card's reserve figures are Reinforcements tokens; one choice still commits on the click.
            Click(MatchTestRoom.Reinforcement(fp, "effect-choice-" + elite));
            await BothAt(fp, sh, 2, token);
            Assert.AreEqual("lorien", fp.Board.View.Units.Single(u => u.Id == elite).Location);
            Assert.AreEqual(6, fp.Board.View.Hand.Count);
        }

        private static async Task DrawAndDiscard(MountedMatchClient fp, MountedMatchClient sh, CancellationToken token)
        {
            var die = fp.Board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Event);
            Click(fp.Board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(60, token);
            await CardGestures.Open(fp);
            PointerGestureEvents.ClickAt(fp.Board.Decks.Root.Q<Button>("deck-character"));
            await BothAt(fp, sh, 4, token);
            Assert.AreEqual(DecisionKind.DiscardCards, fp.Board.View.Decision.Kind);
            var secret = "base.fp.character.19";
            await CardGestures.Open(fp);
            PointerGestureEvents.ClickAt(fp.Board.Hand.Q<Button>(secret));
            PointerGestureEvents.ClickAt(fp.Board.Hand.Q<Button>(secret), 2);
            await BothAt(fp, sh, 5, token);
            Assert.AreEqual(6, fp.Board.View.Hand.Count);
            Assert.IsFalse(fp.Board.View.Hand.Contains(secret));
            StringAssert.DoesNotContain(secret, JsonConvert.SerializeObject(sh.Board.View));
        }

        private static async Task Play(MountedMatchClient client, string card, DieFace cost, CancellationToken token)
        {
            await Task.Delay(60, token);
            var die = client.Board.View.Dice.First(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available && d.Face == cost);
            Click(client.Board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(60, token);
            // UI-194: hover unrolls the card for reading; its one click plays it, with no preview to confirm.
            await CardGestures.Preview(client, card);
            Assert.AreNotEqual(card, client.Board.Cards.CardId, "One way to pay opens no preview.");
        }

        private static async Task DiscardDie(MountedMatchClient client, CancellationToken token)
        {
            var die = client.Board.View.Dice.First(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available);
            Click(client.Board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(60, token);
            await ComponentGestures.Select(client, "DiscardDie");
        }

        private static async Task Confirm(MountedMatchClient client, CancellationToken token)
        {
            await Until(() => client.Board.Tray.Q<Button>("confirm-decision")?.enabledSelf == true, token);
            Click(client.Board.Tray.Q<Button>("confirm-decision"));
        }
        private static async Task BothAt(MountedMatchClient first, MountedMatchClient second, long revision, CancellationToken token)
        {
            await Until(() => first.Board.View?.Revision == revision && second.Board.View?.Revision == revision, token);
            await Task.Delay(80, token); // Let newly mounted controls receive their first layout before pointer input.
        }
        private static async Task Until(Func<bool> condition, CancellationToken token)
        {
            while (!condition())
                await Task.Delay(30, token);
        }
        internal static void Click(VisualElement element)
        {
            Assert.NotNull(element);
            // RenderTexture panels share a pointer cache in one Unity process. Move into
            // this panel before pressing, as a real pointer entering its window would.
            var root = element.panel.visualTree;
            foreach (var point in new[] { root.worldBound.min + Vector2.one, element.worldBound.center })
                using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                    root.SendEvent(move);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = element.worldBound.center, clickCount = 1 }))
                element.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = element.worldBound.center, clickCount = 1 }))
                element.SendEvent(up);
        }
        private sealed class Fixture
        {
            public string Address;
            public string CardsMatch;
            public string FreePeoplesUser;
            public string ShadowUser;
            public string Password;
        }
    }
}
