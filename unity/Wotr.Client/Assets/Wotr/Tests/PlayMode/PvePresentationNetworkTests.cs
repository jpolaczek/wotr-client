using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class PvePresentationNetworkTests
    {
        [UnityTest] public IEnumerator ShadowMovementUsesTheCameraDieAndFigures() => Wait(Play("shadow-move", Side.FreePeoples));
        [UnityTest] public IEnumerator FreePeoplesMovementUsesTheSamePresenters() => Wait(Play("free-move", Side.Shadow));
        [UnityTest] public IEnumerator CpuRecruitmentCardRevealsBeforeItsEffects() => Wait(Play("recruit-card", Side.FreePeoples));
        [UnityTest] public IEnumerator CpuAttackKeepsTheHumansBattleChoiceExplicit() => Wait(Play("attack", Side.FreePeoples));
        [UnityTest] public IEnumerator CpuCombatResponseUsesTheBattleWindow() => Wait(Play("combat", Side.FreePeoples));
        [UnityTest] public IEnumerator CpuFellowshipMovementUsesTheHuntWindow() => Wait(Play("hunt", Side.Shadow));
        [UnityTest] public IEnumerator CpuHuntDamageResponseShowsTheAcceptedCorruption() => Wait(Play("hunt-response", Side.Shadow));
        [UnityTest] public IEnumerator CpuHuntRerollResponseLeavesDamageForTheHuman() => Wait(Play("hunt-reroll", Side.FreePeoples));
        [UnityTest] public IEnumerator ReducedMotionPreservesTheHumansNextDecision() => Wait(Play("reduced", Side.Shadow, reduced: true));
        [UnityTest] public IEnumerator SkipDoesNotSubmitTheHumansNextDecision() => Wait(Play("skip", Side.FreePeoples, skip: true));
        [UnityTest] public IEnumerator CpuContinuesWithoutAClientAndReconnectDoesNotReplayItsMove() => Wait(Recovery());

        private static async Task Play(string key, Side human, bool reduced = false, bool skip = false)
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(65));
            var token = timeout.Token;
            var requests = new HumanRequests();
            using var api = await Login(fixture, human, requests, token);
            using var client = new MountedMatchClient(fixture.Address, fixture.PveMatches[key], api: api);
            client.Interface.Preferences.SetReducedMotion(reduced);
            client.Connection.Start();
            await Until(() => client.Board.View != null && client.Connected > 0, token);
            var before = client.Board.View.Revision;
            var corruption = client.Board.View.Fellowship.Corruption;
            client.Board.Map.Focus("grey-havens");
            await Task.Delay(150, token);
            var observations = new Observations();
            await PveTestFixture.Release(fixture.PveMatches[key], token);
            var frames = 0;
            var quiet = DateTime.UtcNow;
            var nextFrame = DateTime.MinValue;
            while (client.Board.View.Revision <= before || client.Playback.Holding || DateTime.UtcNow - quiet < TimeSpan.FromMilliseconds(400))
            {
                token.ThrowIfCancellationRequested();
                observations.Read(client);
                if (client.Playback.Holding)
                {
                    quiet = DateTime.UtcNow;
                    Assert.IsFalse(client.Board.Tray.enabledInHierarchy, "Human commands stay held during playback.");
                    if (skip) client.Playback.Skip();
                }
                if (DateTime.UtcNow >= nextFrame)
                {
                    LayoutTests.Save(client.Viewport, $"bot13-clip-{key}-{frames++:0000}.png");
                    nextFrame = DateTime.UtcNow.AddMilliseconds(100);
                }
                await Task.Delay(15, token);
            }
            Assert.Greater(client.Board.View.Revision, before);
            Assert.AreEqual(0, requests.Commands, "CPU actions came from the runtime; the human client submitted nothing.");
            if (!skip)
            {
                if (key.Contains("move") || key == "reduced")
                {
                    Assert.IsTrue(observations.Source, "The used die is visibly marked.");
                    if (!reduced)
                    {
                        Assert.IsTrue(observations.Figures, "The ordinary figure flight was observed.");
                        Assert.IsTrue(observations.Framed, "The camera framed the source before its figures flew.");
                        Assert.IsTrue(observations.SourceBeforeFigures, "The used die was marked before movement.");
                    }
                }
                if (key == "recruit-card")
                {
                    Assert.IsTrue(observations.Card, "The actual played card was revealed.");
                    Assert.IsTrue(observations.Recruitment, "The recruitment effect followed at its regions.");
                    Assert.IsTrue(observations.CardBeforeRecruitment);
                }
                if (key == "attack" || key == "combat") Assert.IsTrue(observations.Battle);
                if (key.StartsWith("hunt")) Assert.IsTrue(observations.Hunt);
                if (key == "hunt-response") Assert.Greater(client.Board.View.Fellowship.Corruption, corruption);
            }
            if (key is "attack" or "combat" or "reduced" or "skip" or "hunt-reroll")
            {
                await Until(() => client.Board.View.DecisionOwner == human && !client.Playback.Holding, token);
                var revision = client.Board.View.Revision;
                Assert.IsNotNull(client.Board.View.Decision);
                await Task.Delay(350, token);
                Assert.AreEqual(revision, client.Board.View.Revision, "The human choice remains pending.");
                Assert.AreEqual(0, requests.Commands);
            }
            Assert.Greater(frames, 1, "The real network playback was captured.");
        }

        private static async Task Recovery()
        {
            var fixture = PveTestFixture.Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var token = timeout.Token;
            var requests = new HumanRequests();
            using var api = await Login(fixture, Side.FreePeoples, requests, token);
            var match = fixture.PveMatches["recovery"];
            long before;
            using (var closed = new MountedMatchClient(fixture.Address, match, api: api))
            {
                closed.Connection.Start();
                await Until(() => closed.Board.View != null, token);
                before = closed.Board.View.Revision;
            }
            await PveTestFixture.Release(match, token);
            PlayerView current;
            do
            {
                await Task.Delay(100, token);
                current = await api.Get<PlayerView>("api/matches/" + match, token);
            } while (current.Revision <= before || current.DecisionOwner != Side.FreePeoples);
            using var resumed = new MountedMatchClient(fixture.Address, match, api: api);
            resumed.Connection.Start();
            await Until(() => resumed.Board.View?.Revision == current.Revision && resumed.Connected > 0, token);
            Assert.IsFalse(resumed.Playback.Holding, "Reconnect shows the latest state without replaying missed effects.");
            Assert.IsNotNull(resumed.Board.View.Decision);
            var connected = resumed.Connected;
            resumed.Connection.Reconnect();
            await Until(() => resumed.Connected > connected, token);
            Assert.IsFalse(resumed.Playback.Holding);
            Assert.AreEqual(current.Revision, resumed.Board.View.Revision);
            Assert.AreEqual(0, requests.Commands);
        }

        private static async Task<GameApi> Login(PveTestFixture fixture, Side human, HttpMessageHandler handler, CancellationToken token)
        {
            var api = new GameApi(fixture.Address, handler);
            await api.Login(human == Side.FreePeoples ? fixture.FreePeoplesUser : fixture.ShadowUser, fixture.Password, token);
            return api;
        }
        private sealed class HumanRequests : DelegatingHandler
        {
            public int Commands;
            public HumanRequests() : base(new HttpClientHandler()) { }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath.EndsWith("/commands")) Commands++;
                return base.SendAsync(request, token);
            }
        }
        private sealed class Observations
        {
            public bool Source, Figures, Framed, SourceBeforeFigures, Card, Recruitment, CardBeforeRecruitment, Battle, Hunt;
            public void Read(MountedMatchClient client)
            {
                Source |= client.Board.Root.Q(CostEmphasis.MarkName)?.ClassListContains("shown") == true;
                if (!Figures && client.Board.Map.Figures.ActiveCount > 0)
                {
                    var movement = client.Board.Map.Figures.Moves.First();
                    var anchor = client.Board.Map.Data.Shapes[movement.From.Region].Anchor;
                    Framed = client.Presenters.Camera.SafeArea().Contains(client.Board.Map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
                    SourceBeforeFigures = Source;
                    Figures = true;
                }
                Card |= client.Playback.Holding && client.Board.Cards.CardId == "base.sh.strategy.20";
                if (client.Board.Map.Q("observed-delta") != null)
                {
                    Recruitment = true;
                    CardBeforeRecruitment = Card;
                }
                Battle |= client.Playback.Holding && client.Board.Battles.Presenting;
                Hunt |= client.Playback.Holding && client.Board.Hunt.Root.style.display.value == DisplayStyle.Flex;
            }
        }
    }
}
