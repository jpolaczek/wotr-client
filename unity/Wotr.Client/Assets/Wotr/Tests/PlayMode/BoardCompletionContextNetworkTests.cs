using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionContextNetworkTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string White = "base.character.gandalf-white";
        private const string Gimli = "base.character.gimli";

        [UnityTest]
        public IEnumerator RecordedTransformationAndGoldKeepTheNewExactCharacterDraft() => Wait(Transformation());

        [UnityTest]
        public IEnumerator PrivateSetupAndPublicGuideKeepTheirHistoricalVersionsAndProperHost() => Wait(Setup());

        private static async Task Transformation()
        {
            using var boundary = new UI10HttpBoundary();
            using var room = await Connect("ui71-history", ScenarioKind.Interaction, fpHandler: boundary, openPanels: false);
            var board = room.Fp.Board;
            boundary.MatchPath = "/api/matches/" + board.View.MatchId;
            var revision = board.View.Revision;
            var choice = board.View.Options.Single(o => o.Kind == CommandKind.EnterCharacter).CharacterActions
                .Single(value => value.CharacterId == White);
            var die = choice.Dice.Last();
            await ComponentGestures.Cost(room, room.Fp, die);
            // UI-108/UI-142: a click on Gandalf's row brings the White in; the decision stays in its dock.
            await ComponentGestures.Enter(room, room.Fp, choice);
            Assert.AreNotSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.AreEqual(0, boundary.Commands);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var page = await History(room, client);
                var step = page.Steps.Single(value => value.Command == CommandKind.EnterCharacter);
                var group = page.Actions.Single(value => value.Id == step.ActionId);
                var parent = client.Board.Tray.parent;
                var state = JsonConvert.SerializeObject(client.Board.View);
                client.Board.ActionHistory.Open(new ActionOpenRequest(group.Id, group.Version, step.Id));
                await room.Until(() => client.Board.ActionHistory.Context != null);
                await room.Settle();
                var text = HistoryText(client.Board);
                StringAssert.Contains("Strider · Base → Aragorn · Base", text);
                StringAssert.Contains("Pelargir", text);
                StringAssert.DoesNotContain("Will Of The West", text, "UI-106: the card names no die.");
                Assert.IsFalse(text.Contains("Pelargir → Pelargir"));
                Assert.AreSame(parent, client.Board.Tray.parent);
                Assert.AreEqual(state, JsonConvert.SerializeObject(client.Board.View));
                LayoutTests.Save(client.Viewport, "ui71-transformation-" + client.Board.View.You + ".png");
                client.Board.Log.CloseCard();
            }
            board.Characters.Close();
            await KingsCardNetworkChecks.Reconnect(room, room.Fp);
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display);
            Assert.AreNotSame(board.Characters.DecisionHost, board.Tray.parent);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("fangorn"), "The chosen transformation survives the reconnect.");
            Assert.AreEqual(die, board.ActionDie.DieId);
            Assert.AreEqual(revision, board.View.Revision);
            Assert.AreEqual(0, boundary.Commands);
            board.Map.Select("fangorn");
            await room.At(revision + 1);
            Assert.AreEqual(1, boundary.Commands);
            Assert.AreEqual(die, boundary.LastCommand.Payload.DieId);
            Assert.AreEqual(White, boundary.LastCommand.Payload.CharacterId);
        }

        private static async Task Setup()
        {
            using var room = await Connect("ui71-setup", ScenarioKind.Interaction, openPanels: false);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            Assert.AreEqual(DecisionKind.CouncilHomelands, board.View.Decision.Kind);
            var shadowBefore = JsonConvert.SerializeObject(room.Sh.Board.View);
            board.Fellowship.Open();
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Fellowship.Root.Q<Button>("fellowship-portrait-" + Gimli));
            await room.Settle();
            board.Map.Select("ered-luin");
            await room.Settle();
            var selected = board.Fellowship.Root.Q<Button>("fellowship-portrait-" + Gimli).Q<Label>("homeland-location").text;
            StringAssert.Contains("Ered Luin", selected);
            var privatePage = await History(room, room.Fp);
            var privateStep = privatePage.Steps.Single(step => step.Command == CommandKind.ChooseCompanionVersions);
            var privateGroup = privatePage.Actions.Single(action => action.Id == privateStep.ActionId);
            board.ActionHistory.Open(new ActionOpenRequest(privateGroup.Id, privateGroup.Version, privateStep.Id));
            await room.Until(() => board.ActionHistory.Context != null);
            await room.Settle();
            Assert.AreEqual(InGameActionHost.Fellowship, board.ActionHistory.Context.InGame.Host);
            StringAssert.Contains("Selected version · Gandalf the Grey · Lords", HistoryText(board));
            StringAssert.Contains("Selected version · Gimli · Fate · Alternate", HistoryText(board));
            StringAssert.Contains("Selected version · Legolas · Lords", HistoryText(board));
            StringAssert.Contains("Guide · Strider · Lords", HistoryText(board));
            Assert.AreSame(board.Fellowship.Root, board.Tray.parent);
            LayoutTests.Save(room.Fp.Viewport, "ui71-setup-own-history.png");
            board.Log.CloseCard();
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.AreEqual(selected, board.Fellowship.Root.Q<Button>("fellowship-portrait-" + Gimli).Q<Label>("homeland-location").text);

            var publicPage = await History(room, room.Sh);
            var publicText = JsonConvert.SerializeObject(publicPage);
            foreach (var hidden in new[] { "fate.character.gimli-dwarven-lord", "lords.character.legolas", "lords.character.gandalf-grey", privateStep.Id })
                StringAssert.DoesNotContain(hidden, publicText);
            Assert.IsFalse(publicPage.Steps.Any(step => step.Command == CommandKind.ChooseCompanionVersions));
            var guide = publicPage.Steps.Single(step => step.Kind == "CompanionRevealed");
            var guideGroup = publicPage.Actions.Single(action => action.Id == guide.ActionId);
            room.Sh.Board.ActionHistory.Open(new ActionOpenRequest(guideGroup.Id, guideGroup.Version, guide.Id));
            await room.Until(() => room.Sh.Board.ActionHistory.Context != null);
            await room.Settle();
            Assert.AreEqual(InGameActionHost.Fellowship, room.Sh.Board.ActionHistory.Context.InGame.Host);
            Assert.IsFalse(room.Sh.Board.Fellowship.IsOpen,
                "Reading recorded setup must preserve the collapsed Fellowship panel.");
            Assert.IsTrue(room.Sh.Board.Fellowship.Status.ClassListContains("history-context-host"),
                "The visible Fellowship plaque anchors the historical public Guide.");
            Assert.IsFalse(room.Sh.Board.Characters.Tab.ClassListContains("history-context-host"));
            StringAssert.Contains("Strider · Lords", HistoryText(room.Sh.Board));
            Assert.IsFalse(room.Sh.Board.Log.Popup.Query<Label>().ToList().Any(label =>
                label.text.Contains("Gimli") || (label.tooltip ?? "").Contains("gimli")));
            Assert.AreEqual(shadowBefore, JsonConvert.SerializeObject(room.Sh.Board.View));
            LayoutTests.Save(room.Sh.Viewport, "ui71-setup-public-guide.png");
            room.Sh.Board.Log.CloseCard();
            var confirm = board.Tray.Q<Button>("confirm-decision");
            await room.Until(() => confirm.enabledInHierarchy);
            PointerGestureEvents.ClickAt(confirm);
            await room.At(revision + 1);
            Assert.AreEqual("ered-luin", room.Sh.Board.View.Characters.Single(c => c.Id == Gimli).Location);
            Assert.AreEqual(DecisionKind.CouncilToken, room.Sh.Board.View.Decision.Kind);
            // UI-270: the Shadow takes its token in its pool; no panel hosts the decision.
            Assert.AreEqual(DecisionHost.Dice, DecisionRoutes.Of(room.Sh.Board.View));
            var oldRecord = await room.Sh.Api.Get<ActionLogView>("api/matches/" + board.View.MatchId +
                "/actions/" + guideGroup.Id, room.Token);
            Assert.IsFalse(oldRecord.Steps.Any(step => step.Facts.Targets.Contains("fate.character.gimli-dwarven-lord")),
                "A later public separation must not rewrite the previously private setup step.");
        }

        private static Task<ActionLogPage> History(MatchTestRoom room, MountedMatchClient client) =>
            client.Api.Get<ActionLogPage>("api/matches/" + client.Board.View.MatchId + "/actions?limit=50", room.Token);

        private static string HistoryText(BoardScreen board) =>
            string.Join("\n", board.Log.Card.Root.Query<Label>().ToList().Select(label => label.text));
    }
}
