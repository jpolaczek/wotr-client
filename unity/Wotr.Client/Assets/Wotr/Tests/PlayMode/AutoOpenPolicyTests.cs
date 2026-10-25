using System.Collections;
using System.Collections.Generic;
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
    // UI-168 (SIGNALS_UX C, AO01–AO03) with the real server: a window opens by itself only where the table says —
    // the battle and Hunt windows for both players, the player's own decisions (Fellowship phase, Reinforcements)
    // only for them, the end of the game for both (GameEndNetworkTests) — and never for an ordinary action of the
    // opponent. A window the player closed stays closed until the next event, and the camera never moves by itself.
    public sealed class AutoOpenPolicyTests
    {
        [UnityTest]
        public IEnumerator AnOrdinaryOpponentActionOpensNothingAndTheFellowshipPhaseOnlyForItsPlayer() => Wait(Ordinary());

        [UnityTest]
        public IEnumerator ANewBattleOpensForBothAndAClosedWindowStaysClosed() => Wait(Battle());

        [UnityTest]
        public IEnumerator ANewHuntOpensForBothAndAClosedWindowStaysClosed() => Wait(Hunt());

        [UnityTest]
        public IEnumerator TheMusterDieOpensReinforcementsOnlyForThePlayerUntilClosed() => Wait(Muster());

        // AO01 and the Fellowship row: a fresh base game from the Fellowship phase to the first ordinary action.
        private static async Task Ordinary()
        {
            using var room = await CreateNormalGame();
            var fp = room.Fp.Board;
            var sh = room.Sh.Board;
            await room.Until(() => fp.View?.Decision?.Kind == DecisionKind.FellowshipPhase);
            await room.Settle();
            Assert.IsTrue(fp.Fellowship.Held, "The Free Peoples' own Fellowship phase holds the Fellowship panel.");
            Assert.IsFalse(sh.Fellowship.Held);
            AssertNothingOpen(sh, "the opponent's Fellowship phase");
            await Send(room, room.Fp, CommandKind.ContinueFellowship, new CommandPayload());
            await room.Until(() => sh.View?.Decision?.Kind == DecisionKind.AllocateHunt);
            await room.Settle();
            AssertNothingOpen(fp, "the opponent's Hunt allocation");
            // UI-183: the Shadow's allocation lights the dice pool's frame and the dice it can take; the Free Peoples'
            // pool stays plain. The frame does not move a single die.
            var pool = sh.OwnDice;
            Assert.IsTrue(pool.ClassListContains(DiceDecision.ClassName), "The allocation lights the Shadow's dice pool.");
            Assert.IsFalse(fp.OwnDice.ClassListContains(DiceDecision.ClassName), "The opponent's pool stays plain.");
            var candidates = pool.Query<Button>(className: "hunt-candidate").ToList();
            Assert.IsNotEmpty(candidates, "The dice the Hunt can take glow.");
            // UI-205: before the first die is in the allocation the candidates call loudest with a 3 pt ring; the
            // gentler 2 pt ring of UI-183 returns once a die is in.
            Assert.IsTrue(pool.ClassListContains("hunt-choose-dice"), "No die is in the allocation yet.");
            Assert.AreEqual(3f, candidates[0].resolvedStyle.borderTopWidth, .01f);
            Assert.AreEqual(2f, pool.resolvedStyle.borderTopWidth, .01f);
            Assert.AreEqual(pool.resolvedStyle.paddingLeft + 2, 8f, .01f, "The frame takes the padding's place, so no die moves.");
            var allocation = sh.View.Options.First(o => o.Kind == CommandKind.AllocateHunt);
            var dice = allocation.EligibleIds.Concat(allocation.EligibleDice).Distinct().Take(allocation.Minimum).ToList();
            await Send(room, room.Sh, CommandKind.AllocateHunt, new CommandPayload { Ids = dice });
            await room.Until(() => fp.View.Decision?.Kind == DecisionKind.Action && fp.View.DecisionOwner == Side.FreePeoples);
            await room.Settle();
            Assert.IsFalse(sh.OwnDice.ClassListContains(DiceDecision.ClassName), "The sent allocation turns the frame off.");
            var camera = Camera(sh);
            var die = fp.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available).Id;
            var revision = sh.View.Revision;
            await Send(room, room.Fp, CommandKind.DiscardDie, new CommandPayload { DieId = die });
            await room.Until(() => sh.View.Revision > revision);
            await room.Settle();
            AssertNothingOpen(sh, "an ordinary action of the opponent");
            Assert.AreEqual(camera, Camera(sh), "The camera never moves by itself.");
        }

        // AO02, battle row: both windows open; a closed one stays closed for the rest of the battle.
        private static async Task Battle()
        {
            using var room = await Connect("ui43-stale", ScenarioKind.BaseAcceptance, openPanels: false);
            var fp = room.Fp.Board;
            var sh = room.Sh.Board;
            // UI-305 (OPPONENT_ACTIONS_UX §4, §6.5): the Shadow's camera may frame the opponent's attack before its window
            // opens. A beat after the camera's records where the presentation left it; opening the window moves nothing.
            string presented = null;
            room.Sh.Playback.Register(new ScriptedCuePresenter(new Dictionary<PresentationCueKind, double> { [PresentationCueKind.Attack] = 0 },
                _ => presented = Camera(sh)));
            await room.Attack(room.Fp, "minas-tirith", "lossarnach");
            await room.Until(() => fp.Battles.IsOpen && sh.Battles.IsOpen);
            Assert.IsNotNull(presented, "The attack was presented to the Shadow.");
            Assert.AreEqual(presented, Camera(sh), "Opening the battle window never moves the camera.");
            var framed = Camera(sh);
            sh.Battles.Close();
            await room.Settle();
            await PastCombatCards(room);
            await room.Settle();
            Assert.IsFalse(sh.Battles.IsOpen, "A battle window the player closed stays closed until the next battle.");
            Assert.IsTrue(fp.Battles.IsOpen);
            Assert.IsFalse(sh.Log.IsOpen);
            Assert.AreEqual(DisplayStyle.None, sh.Log.Popup.resolvedStyle.display, "The opponent's action card never opens by itself.");
            Assert.AreEqual(framed, Camera(sh), "The battle's beats play in its window and never move the camera.");
        }

        // AO02, Hunt row: a step on the Mordor Track always draws a damaging tile (TEST-07 sh-ring-corrupted), so the
        // Hunt waits for the Free Peoples' answer; both windows open, and one the Shadow closed stays closed. A Hunt
        // that resolves without any decision does not unfold the window (UI-162).
        private static async Task Hunt()
        {
            var session = await TestScenarioRoom.Start("sh-ring-corrupted");
            try
            {
                var fp = session.Client(Side.FreePeoples).Board;
                var sh = session.Client(Side.Shadow).Board;
                var camera = Camera(sh);
                var die = fp.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Character);
                await session.Send(Side.FreePeoples, CommandKind.MoveFellowship, new CommandPayload { DieId = die.Id });
                await session.Until(() => fp.View.Decision?.Kind == DecisionKind.HuntDamage && fp.Hunt.IsOpen && sh.Hunt.IsOpen);
                Assert.AreEqual(camera, Camera(sh), "Opening the Hunt window never moves the camera.");
                sh.Hunt.Close();
                await session.Settle();
                var revision = sh.View.Revision;
                await session.Send(Side.FreePeoples, CommandKind.ResolveHuntDamage, new CommandPayload { HuntResponse = HuntResponse.Ring });
                await session.Until(() => sh.View.Revision > revision);
                await session.Settle();
                Assert.IsFalse(sh.Hunt.IsOpen, "A Hunt window the player closed stays closed until the next Hunt.");
                Assert.IsTrue(sh.GameEnd.IsOpen && fp.GameEnd.IsOpen, "Corruption 12 ends the game on both screens.");
            }
            finally
            {
                await session.End();
            }
        }

        // AO03 (UI-181): the player's own Muster die lights the Reserve tab, like the other tabs, and unrolls nothing;
        // the opponent sees neither. A tab the player opened and closed stays closed while the die is used.
        private static async Task Muster()
        {
            var session = await TestScenarioRoom.Start("fp-muster-recruit");
            try
            {
                var client = session.Client(Side.FreePeoples);
                var fp = client.Board;
                var sh = session.Client(Side.Shadow).Board;
                Assert.AreEqual(DisplayStyle.None, fp.Reinforcements.Root.resolvedStyle.display, "Nothing before the die is chosen.");
                Assert.IsFalse(fp.Reinforcements.Tab.ClassListContains("legal-target"));
                var die = fp.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Muster).Id;
                ArmyGestures.Die(client, die, DieFace.Muster);
                await session.Until(() => fp.Reinforcements.Tab.ClassListContains("legal-target"));
                await session.Settle();
                Assert.AreEqual(DisplayStyle.None, fp.Reinforcements.Root.resolvedStyle.display, "The Muster die only lights the tab.");
                Assert.IsTrue(fp.Reinforcements.Tab.ClassListContains(DieOptionGlow.LitClass), "The die's glow marks the tab like the others.");
                AssertNothingOpen(sh, "the opponent's Muster die");
                Assert.IsFalse(sh.Reinforcements.Tab.ClassListContains("legal-target"), "The opponent's tab stays plain.");
                fp.Reinforcements.Open();
                await session.Settle();
                Assert.AreEqual(DisplayStyle.Flex, fp.Reinforcements.Root.resolvedStyle.display);
                fp.Reinforcements.Close();
                await session.Settle();
                fp.Map.Refresh();
                await session.Settle();
                Assert.AreEqual(DisplayStyle.None, fp.Reinforcements.Root.resolvedStyle.display,
                    "The Reserve tab closed by the player stays closed while the die is used.");
            }
            finally
            {
                await session.End();
            }
        }

        // Both sides answer No card through the server, also a side without a legal card (UI-279: nothing is sent by
        // itself), until the battle moves on to its next decision.
        private static async Task PastCombatCards(MatchTestRoom room)
        {
            var sh = room.Sh.Board;
            await room.Until(() => sh.View.Decision?.Kind != DecisionKind.CombatCard || Chooser(room, sh.View) != null);
            while (sh.View.Decision?.Kind == DecisionKind.CombatCard)
            {
                var revision = sh.View.Revision;
                await room.Until(() => Chooser(room, sh.View) != null);
                await Send(room, Chooser(room, sh.View), CommandKind.CommitCombatCard, new CommandPayload());
                await room.Until(() => sh.View.Revision > revision);
            }
        }

        // The side choosing its Combat card, once its client shows the same revision.
        private static MountedMatchClient Chooser(MatchTestRoom room, PlayerView view)
        {
            var client = view.DecisionOwner == Side.FreePeoples ? room.Fp : room.Sh;
            return client.Board.View.Revision == view.Revision ? client : null;
        }

        // The windows the table never opens for an observer: battle, Hunt, the decision window, the Fellowship panel,
        // Reinforcements, the Log and its action card, the card preview, the region panel and the end of the game.
        private static void AssertNothingOpen(BoardScreen board, string label)
        {
            Assert.IsFalse(board.Battles.IsOpen, "Battle · " + label);
            Assert.IsFalse(board.Hunt.IsOpen, "Hunt · " + label);
            Assert.IsFalse(board.DecisionWindow.IsOpen, "Decision window · " + label);
            Assert.IsFalse(board.Fellowship.Held || board.Fellowship.Pinned, "Fellowship · " + label);
            Assert.AreEqual(DisplayStyle.None, board.Reinforcements.Root.resolvedStyle.display, "Reserve · " + label);
            Assert.IsFalse(board.Log.IsOpen, "Log · " + label);
            Assert.AreEqual(DisplayStyle.None, board.Log.Popup.resolvedStyle.display, "Action card · " + label);
            Assert.AreEqual("", board.Cards.CardId, "Card preview · " + label);
            Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display, "Region panel · " + label);
            Assert.IsFalse(board.GameEnd.IsOpen, "End of the game · " + label);
        }

        private static string Camera(BoardScreen board) => board.Map.Camera.Pan + "/" + board.Map.Camera.Zoom;

        private static async Task Send(MatchTestRoom room, MountedMatchClient client, CommandKind kind, CommandPayload payload)
        {
            var view = client.Board.View;
            var receipt = await client.Api.Post<CommandReceipt>($"api/matches/{view.MatchId}/commands", new GameCommand
            {
                MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
                CommandId = System.Guid.NewGuid().ToString("N"), Kind = kind, Payload = payload
            }, room.Token);
            Assert.IsTrue(receipt.Accepted, kind + ": " + receipt.Error);
        }
    }
}
