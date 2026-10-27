using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-344 (EXP-15 D1-D4) with two real clients on the test menu's Kings scenarios: the watcher's board plays the
    // opponent's corruption attempt (the tile at the king's force and his meter), awakening (the king's moment where his
    // Leader stood) and Dismay (the watcher's own Ruler die rerolled, the Dismay chip counting down) before the release.
    public sealed class OpponentExpansionPresentationNetworkTests
    {
        [UnityTest] public IEnumerator TheShadowsCorruptionAttemptPlaysAtTheKingsForce() => Wait(Corruption());
        [UnityTest] public IEnumerator TheFreePeoplesAwakeningPlaysWhereTheLeaderStood() => Wait(Awakening());
        [UnityTest] public IEnumerator TheShadowsDismayRerollsMyRulerDieInItsPresentation() => Wait(Dismay());

        private static async Task Corruption()
        {
            var session = await TestScenarioRoom.Start("kings-sh-corruption");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await Watching(session, fp);
                var attempt = sh.Board.View.Options.First(o => o.Enabled && o.Kind == CommandKind.AttemptCorruption);
                var king = attempt.CharacterActions.Count > 0 ? attempt.CharacterActions[0].CharacterId : attempt.EligibleIds[0];
                var eye = attempt.EligibleDice.Count > 0 ? attempt.EligibleDice[0]
                    : sh.Board.View.Dice.First(d => d.Location == DieLocation.Hunt && d.Face == DieFace.Eye).Id;
                await Step(session, sh, CommandKind.AttemptCorruption, new CommandPayload { CharacterId = king, DieId = eye });
                while (!sh.Board.View.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile))
                    await Step(session, sh, CommandKind.DrawCorruptionTile, new CommandPayload());
                var tile = sh.Board.View.Options.First(o => o.Enabled && o.Kind == CommandKind.ChooseCorruptionTile).EligibleIds[0];
                await session.Send(Side.Shadow, CommandKind.ChooseCorruptionTile, new CommandPayload { Ids = { tile } });
                await Seen(() => fp.Playback.Holding && fp.Board.Root.Q(SovereignPresenter.PlateName) != null,
                    "The tile's meter stands at the king's force before the release.");
                Assert.AreEqual(DisplayStyle.None, fp.Board.Characters.Root.resolvedStyle.display, "The Characters panel never opens by itself.");
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual(sh.Board.View.Revision, fp.Board.View.Revision);
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Awakening()
        {
            var session = await TestScenarioRoom.Start("kings-fp-awaken");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await Watching(session, sh);
                var awaken = fp.Board.View.Options.First(o => o.Enabled && o.Kind == CommandKind.AwakenSovereign);
                var king = awaken.CharacterActions[0];
                var origin = awaken.Travel.First(t => t.Id == king.CharacterId).Origin;
                await session.Send(Side.FreePeoples, CommandKind.AwakenSovereign,
                    new CommandPayload { CharacterId = king.CharacterId, DieId = king.Dice[0], Route = { origin } });
                await Seen(() => sh.Playback.Holding && sh.Board.Map.Q("observed-portraits") != null,
                    "The awakened king's moment stands where his Leader stood.");
                await session.Until(() => !sh.Playback.Holding);
                Assert.IsTrue(sh.Board.View.Characters.Any(c => c.Id == king.CharacterId && c.Location == origin));
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task Dismay()
        {
            var session = await TestScenarioRoom.Start("kings-dismay");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var sh = session.Client(Side.Shadow);
                await Watching(session, fp);
                var dismay = sh.Board.View.Options.First(o => o.Enabled && o.Kind == CommandKind.UseDismay);
                var die = dismay.EligibleDice.First(id => sh.Board.View.Dice.Single(d => d.Id == id).Owner == Side.FreePeoples);
                var chip = fp.Board.TopBar.Opponent.Root.Q("opponent-dismay");
                Assert.AreEqual(DisplayStyle.Flex, chip.style.display.value, "The Shadow's Dismay tokens have their chip.");
                var baseline = fp.Board.View.Revision;
                var face = fp.Board.View.Dice.Single(d => d.Id == die).Face;
                var cues = new System.Collections.Generic.List<PresentationCue>();
                fp.Activity.CuesReady += cues.AddRange;
                await session.Send(Side.Shadow, CommandKind.UseDismay, new CommandPayload { Ids = { dismay.EligibleIds[0] }, DieId = die });
                await Seen(() => fp.Playback.Holding && fp.Board.Root.Q(className: PresentationGlow.ClassName) != null &&
                        PresentationGlow.IsLit(fp.Board.OwnDice.Q<Button>(die)),
                    "My Ruler die glows in the Shadow's presentation; cues: " + string.Join(", ", cues.Select(cue => cue.Id)) +
                    " · revisions " + fp.Board.View.Revision + "/" + sh.Board.View.Revision + " · presented " + fp.Playback.PresentedRevision +
                    " · baseline " + baseline + " · decision " + fp.Board.View.Decision?.Kind + "/" + fp.Board.View.DecisionOwner +
                    " · holding " + fp.Playback.Holding + " · live " + fp.Playback.Live + " · connected " + fp.Connected +
                    " · face " + face + ">" + fp.Board.View.Dice.Single(d => d.Id == die).Face +
                    " · tokens " + string.Join(",", sh.Board.View.RulerTokens.Select(token => token.Id + "@" + token.Location)));
                await session.Until(() => !fp.Playback.Holding);
                Assert.AreEqual("0", chip.Q<Label>(className: "summary-value").text);
            }
            finally
            {
                await session.End();
            }
        }

        // The watcher has read its history's baseline, so the opponent's next step is presented rather than remembered.
        private static Task Watching(TestScenarioRoom session, MountedMatchClient watcher) => session.Until(() => watcher.Activity.Loaded);

        // One Shadow command, then its own view's next revision, so the next command is sent against it.
        private static async Task Step(TestScenarioRoom session, MountedMatchClient client, CommandKind kind, CommandPayload payload)
        {
            var revision = client.Board.View.Revision;
            await session.Send(Side.Shadow, kind, payload);
            await session.Until(() => client.Board.View.Revision > revision);
        }

        private static async Task Seen(Func<bool> condition, string what)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition() && DateTime.UtcNow < deadline)
                await Task.Delay(20);
            Assert.IsTrue(condition(), what);
        }
    }
}
