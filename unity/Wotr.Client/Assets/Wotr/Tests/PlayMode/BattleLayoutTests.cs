using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class BattleLayoutTests
    {
        [UnityTest]
        public IEnumerator IncomingBattleDoesNotOpenOrPanAndExplicitPanelKeepsLosses()
        {
            using (var client = new MountedMatchClient("http://localhost", "regional-battle"))
            {
                var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
                client.Apply(view);
                yield return Settle();
                var pan = client.Board.Map.Camera.Pan;
                // UI-149: the battle opens its window; a closed window comes back through its button.
                Assert.IsTrue(client.Board.Battles.IsOpen);
                CardNetworkTests.Click(client.Board.Battles.Root.Q<Button>("close-battle"));
                yield return Settle();
                Assert.AreEqual(DisplayStyle.None, client.Board.Battles.Root.resolvedStyle.display);
                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                yield return Settle();
                Assert.IsTrue(client.Board.Battles.IsOpen);
                var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite);
                var figure = client.Board.Tray.Q<Button>("loss-" + elite.Id);
                CardNetworkTests.Click(figure);
                yield return Settle();
                StringAssert.StartsWith("1.", figure.text);
                StringAssert.Contains("↓", figure.text);
                CardNetworkTests.Click(client.Board.Battles.Root.Q<Button>("close-battle"));
                yield return Settle();
                Assert.IsFalse(client.Board.Battles.IsOpen);
                client.Board.Map.Select(view.Battle.RegionId);
                yield return Settle();
                Assert.IsTrue(client.Board.Battles.IsOpen);
                Assert.AreSame(figure, client.Board.Tray.Q<Button>("loss-" + elite.Id));
                StringAssert.StartsWith("1.", figure.text);
                StringAssert.Contains("↓", figure.text);
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                CardNetworkTests.Click(client.Board.Battles.Root.Q<Button>("close-battle"));
                yield return Settle();
                view.Revision++;
                client.Apply(view);
                yield return Settle();
                Assert.IsFalse(client.Board.Battles.IsOpen, "A window closed during the battle stays closed.");
                view.Battle.Id = "next-battle";
                view.Revision++;
                client.Apply(view);
                yield return Settle();
                Assert.IsTrue(client.Board.Battles.IsOpen, "The next battle opens its window again.");
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
            }
        }

        [UnityTest]
        public IEnumerator AllBattleWindowsKeepTheMapVisibleAndFitWithoutScrollOrNumericFields()
        {
            using (var client = new MountedMatchClient("http://localhost", "battle-layout"))
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        foreach (var kind in new[] { CommandKind.AttackArmy, CommandKind.CommitCombatCard, CommandKind.RerollBattle,
                            CommandKind.TakeBattleLosses, CommandKind.AdvanceBattle, CommandKind.RetreatBattle,
                            CommandKind.ExtendSiege, CommandKind.FightFieldBattle, CommandKind.ContinueBattle })
                        {
                            var mode = kind == CommandKind.ExtendSiege ? BattleMode.Siege : kind == CommandKind.AdvanceBattle ? BattleMode.Relief :
                                kind == CommandKind.ContinueBattle ? BattleMode.Sortie : BattleMode.Field;
                            var view = BattleScreenFixture.Create(kind, true, mode);
                            if (view.Battle != null)
                            {
                                view.AutoDecision.Policy = new AutoDecisionPolicy { Owner = view.You, BattleId = view.Battle.Id };
                                view.AutoDecision.LastSkippedBattleId = view.Battle.Id;
                                view.AutoDecision.LastSkippedRound = 12;
                                // Stress the full own pool as well as the opponent's public pool.
                                view.Dice.RemoveAll(d => d.Owner == view.You);
                                for (var die = 0; die < 10; die++)
                                    view.Dice.Add(new DieView { Id = "layout-own-" + die, Owner = view.You, Face = DieFace.Army, Location = DieLocation.Available });
                                view.Rings.Clear();
                                for (var ring = 0; ring < 3; ring++)
                                    view.Rings.Add(new RingView { Id = "ring-" + ring, Owner = view.You });
                            }
                            client.Apply(view);
                            yield return Settle();
                            client.Board.Map.Fit(); // Explicit player action after a viewport resize, never an automatic camera move.
                            yield return Settle();
                            if (view.Battle != null && !client.Board.Battles.IsOpen)
                            {
                                CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                                yield return Settle();
                            }
                            if (kind == CommandKind.AttackArmy)
                            {
                                ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
                                yield return Settle();
                            }
                            if (kind == CommandKind.AttackArmy)
                            {
                                yield return MatchTestRoom.Wait(ArmyGestures.All(client, "minas-tirith"));
                                yield return Settle();
                                client.Board.Map.Select("lossarnach");
                                yield return Settle();
                                Assert.IsNull(client.Board.Map.Q<Label>("ghost-lossarnach"), "Planning an attack never moves figures.");
                            }
                            if (kind == CommandKind.TakeBattleLosses)
                            {
                                Assert.IsNull(client.Board.Tray.Q<Button>("battle-keep-all"), "Mandatory losses cannot offer an empty sacrifice.");
                                var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite);
                                CardNetworkTests.Click(client.Board.Tray.Q<Button>("loss-" + elite.Id));
                                yield return Settle();
                                StringAssert.StartsWith("1.", client.Board.Tray.Q<Button>("loss-" + elite.Id).text);
                                StringAssert.Contains("↓", client.Board.Tray.Q<Button>("loss-" + elite.Id).text);
                                var losses = new TurnDraft();
                                losses.Choose(CommandKind.TakeBattleLosses);
                                BattleDraft.Loss(losses, elite.Id, BattleLossKind.Downgrade);
                                var preview = new ActionPreview { RemovedIds = view.Battle.Attacker.ParticipantIds.ToList() };
                                client.Board.Tray.Q<Label>(className: "draft-status").text = BattleSummary.Describe(losses, view, preview);
                                CardNetworkTests.Click(client.Board.Tray.Q<Button>("battle-roll-page-FreePeoples"));
                                Assert.IsNotNull(client.Board.Tray.Q<Button>("battle-die-special-FreePeoples-0"));
                                StringAssert.Contains("Hits 6", client.Board.Tray.Q<Label>("battle-roll-source").text);
                                CardNetworkTests.Click(client.Board.Tray.Q<Button>("battle-roll-page-FreePeoples"));
                                Assert.IsNotNull(client.Board.Tray.Q<Button>("battle-die-special-FreePeoples-5"));
                                StringAssert.Contains("Hits 6", client.Board.Tray.Q<Label>("battle-roll-source").text, "Paging dice does not change the source's total hits.");
                                CardNetworkTests.Click(client.Board.Tray.Q<Button>("battle-roll-page-FreePeoples"));
                                yield return Settle();
                            }
                            if (kind == CommandKind.CommitCombatCard)
                            {
                                var back = client.Board.Tray.Q<Button>("battle-card-attacker");
                                StringAssert.Contains("Face down", back.text);
                                Assert.IsFalse(back.enabledSelf);
                                Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"));
                                // UI-137: resting the pointer on a Combat card shows the whole card above it.
                                var tile = client.Board.Tray.Query<Button>(className: "combat-choice").ToList().FirstOrDefault(b => b.enabledSelf);
                                if (tile != null)
                                {
                                    var id = tile.name.Substring("combat-choice-".Length);
                                    Point(tile, tile.worldBound.center);
                                    yield return Settle();
                                    var preview = client.Board.Tray.Q(CombatCardPreview.PreviewName);
                                    Assert.AreEqual(DisplayStyle.Flex, preview.resolvedStyle.display, "Hover shows the card.");
                                    Assert.AreEqual(CardLibrary.Cards[id].Combat, preview.Q<Label>("combat-card-preview-text").text);
                                    Assert.IsTrue(client.Board.Root.worldBound.Contains(preview.worldBound.min) &&
                                        client.Board.Root.worldBound.Contains(preview.worldBound.max), "The preview stays on screen.");
                                    LayoutTests.Save(target, $"battle-combat-preview-{width}-{scale:0.00}.png");
                                    Point(tile, client.Board.Root.worldBound.min + Vector2.one);
                                    yield return Settle();
                                    Assert.AreEqual(DisplayStyle.None, preview.resolvedStyle.display, "Leaving hides it.");
                                }
                            }
                            if (kind == CommandKind.RerollBattle)
                            {
                                var failed = view.Options[0].EligibleIds[0];
                                CardNetworkTests.Click(client.Board.Tray.Q<Button>("battle-die-" + failed));
                                yield return Settle();
                                Assert.IsTrue(client.Board.Tray.Q<Button>("battle-die-" + failed).ClassListContains("selected"));
                            }
                            LayoutTests.Save(target, $"battle-{kind}-{width}-{scale:0.00}.png");
                            Check(client.Board, kind);
                        }
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
        }

        // UI-208: losses are chosen in the column of the side that takes them, in place of its composition, and a battle's
        // decision buttons stand on the player's side: left for the attacker, right for the defender.
        [UnityTest]
        public IEnumerator LossesAndRerollsStandOnTheDecidingPlayersSide()
        {
            using (var client = new MountedMatchClient("http://localhost", "battle-sides"))
                foreach (var kind in new[] { CommandKind.TakeBattleLosses, CommandKind.RerollBattle })
                    foreach (var defender in new[] { false, true })
                    {
                        var view = BattleScreenFixture.Create(kind, true);
                        if (defender)
                            PlayTheDefender(view, kind);
                        client.Apply(view);
                        yield return Settle();
                        if (!client.Board.Battles.IsOpen)
                            CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                        yield return Settle();
                        var tray = client.Board.Tray;
                        var own = tray.Q(defender ? "battle-defender" : "battle-attacker");
                        var other = tray.Q(defender ? "battle-attacker" : "battle-defender");
                        var label = kind + (defender ? " defender" : " attacker");
                        // UI-394 (owner): the choosing side's column is as wide as the other one.
                        Assert.That(own.worldBound.width, Is.EqualTo(other.worldBound.width).Within(1.5f), label + ": columns of one width");
                        var confirm = tray.Q<Button>("confirm-decision");
                        Assert.IsNotNull(confirm, label);
                        if (defender)
                            Assert.Greater(confirm.worldBound.center.x, other.worldBound.xMax, label + ": buttons on the right");
                        else
                            Assert.Less(confirm.worldBound.center.x, other.worldBound.xMin, label + ": buttons on the left");
                        if (kind == CommandKind.TakeBattleLosses)
                        {
                            var figures = tray.Query<Button>(className: "loss-figure").ToList();
                            Assert.IsNotEmpty(figures, label);
                            Assert.IsTrue(figures.All(own.Contains), label + ": loss figures in the losing side's column");
                            Assert.IsTrue(own.Contains(tray.Q("battle-hit-seals")), label + ": hit seals beside them");
                            Assert.AreEqual(DisplayStyle.None, own.Q("battle-composition").resolvedStyle.display, label + ": composition replaced");
                            Assert.AreEqual(DisplayStyle.Flex, other.Q("battle-composition").resolvedStyle.display, label + ": the other side keeps its composition");
                        }
                        else
                        {
                            var eligible = view.Options.First(o => o.Kind == kind).EligibleIds[0];
                            Assert.IsTrue(own.Contains(tray.Q<Button>("battle-die-" + eligible)), label + ": dice to reroll in the player's column");
                        }
                        Assert.IsEmpty(tray.Query<ScrollView>().ToList(), label);
                        Assert.LessOrEqual(confirm.worldBound.yMax, client.Board.Map.worldBound.yMax + 1, label + ": above the console");
                    }
        }

        private static void PlayTheDefender(PlayerView view, CommandKind kind)
        {
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            var option = view.Options.First(o => o.Kind == kind);
            if (kind == CommandKind.TakeBattleLosses)
            {
                view.Battle.LossOwner = Side.Shadow;
                option.EligibleIds = view.Battle.Defender.Units.Where(ArmyDraft.IsCombat).Select(u => u.Id).ToList();
            }
            else
                option.EligibleIds = view.Battle.Defender.Dice.Where(d => !d.Hit && !d.Reroll.HasValue).Select(d => d.Id).ToList();
        }

        private static void Point(VisualElement element, Vector2 position)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = position });
            element.panel.visualTree.SendEvent(move);
        }

        [UnityTest]
        public IEnumerator RevealChangesBackToInspectedCombatTextAndReportKeepsActualRerolls()
        {
            using (var client = new MountedMatchClient("http://localhost", "reveal"))
            {
                var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
                client.Apply(view);
                yield return Settle();
                if (!client.Board.Battles.IsOpen)
                    CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                yield return Settle();
                Assert.IsFalse(client.Board.Tray.Q<Button>("battle-card-attacker").enabledSelf);
                var pan = client.Board.Map.Camera.Pan;
                view.Revision++;
                view.Battle.CardsRevealed = true;
                view.Battle.Attacker.CardId = "base.fp.strategy.01";
                client.Apply(view);
                yield return Settle();
                var openings = 0;
                System.Action opened = () => openings++;
                client.Board.Cards.Opening += opened;
                PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>("battle-card-attacker"));
                client.Board.Cards.Opening -= opened;
                yield return Settle();
                Assert.AreEqual(1, openings, "A revealed Combat card has one inspection callback.");
                Assert.AreEqual(CardLibrary.Cards[view.Battle.Attacker.CardId].Combat, client.Board.Cards.Root.Q<Label>("card-combat-text").text);
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan);
                view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
                view.LastBattleRound = view.Battle;
                view.Battle = null;
                client.Apply(view);
                yield return Settle();
                CardNetworkTests.Click(client.Board.BattleReport.Open);
                yield return Settle();
                var rerolled = client.Board.BattleReport.Root.Q<Button>("battle-die-FreePeoples-roll-2");
                StringAssert.Contains("First roll 2", rerolled.tooltip);
                StringAssert.Contains("Leader reroll 6", rerolled.tooltip);
                Assert.AreEqual(2, rerolled.Query<DiePips>().ToList().Count);
                Assert.AreEqual("✓", rerolled.Q<Label>("battle-die-outcome").text);
                // UI-114: the press state adds a sink and a tint to the cosmetic set; still no roll animation or delay.
                var cosmetic = new[] { "background-color", "translate", "-unity-background-image-tint-color" };
                Assert.IsTrue(client.Board.BattleReport.Root.Query<Button>().ToList().All(b =>
                    b.resolvedStyle.transitionProperty.All(p => cosmetic.Contains(p.ToString())) &&
                    b.resolvedStyle.transitionDelay.All(t => t.value == 0)), "Only cosmetic transitions; no fabricated roll animation or delay.");
                var another = BattleScreenFixture.Create(CommandKind.AttackArmy);
                client.Apply(another);
                yield return Settle();
                Assert.AreEqual(DisplayStyle.None, client.Board.BattleReport.Root.resolvedStyle.display, "A different match must not inherit the previous report.");
            }
        }

        private static void Check(BoardScreen board, CommandKind kind)
        {
            var top = board.Root.Q<VisualElement>(className: "topbar");
            foreach (var die in board.OpponentDice.Query<Button>().ToList())
            {
                Assert.GreaterOrEqual(die.worldBound.yMin, top.worldBound.yMin, "Opponent die clipped above the screen");
                Assert.LessOrEqual(die.worldBound.yMax, top.worldBound.yMax, "Opponent die clipped below the top bar");
            }
            foreach (var button in board.Tray.Query<Button>().ToList().Where(b => b.worldBound.width > 0 && b.worldBound.height > 0))
            {
                Assert.GreaterOrEqual(button.worldBound.xMin + 1, board.Tray.worldBound.xMin, kind + " " + button.name);
                Assert.LessOrEqual(button.worldBound.xMax, board.Tray.worldBound.xMax + 1, kind + " " + button.name);
                Assert.LessOrEqual(button.worldBound.yMax, (board.View.Battle != null ? board.Map : board.Root).worldBound.yMax + 1, kind + " " + button.name);
                // Panel scaling can round a 32 logical pixel control by a fraction of a physical pixel.
                Assert.GreaterOrEqual(button.resolvedStyle.height + .25f, button.ClassListContains("group-heading") ? 24 : 32, kind + " " + button.name);
                if (button.ClassListContains("battle-card"))
                {
                    var text = button.MeasureTextSize(button.text, button.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                    Assert.LessOrEqual(text.y, button.contentRect.height + 1, "Card title vertically clipped: " + button.text);
                }
            }
            if (board.View.Battle != null)
            {
                // UI-208: the whole battle window, its footer included, stays above the turn console.
                Assert.LessOrEqual(board.Battles.Root.worldBound.yMax, board.Console.Root.worldBound.yMin + 1, kind + ": battle window above the console");
                var policy = board.BattlePolicy;
                Assert.LessOrEqual(policy.Root.worldBound.yMax, board.Root.worldBound.yMax + 1, "Private card setting below viewport");
                // UI-160: the private card setting sits in the battle window's header, not in the dice panel.
                Assert.IsTrue(board.Battles.Root.Contains(policy.Root), "Private card setting outside the battle window");
                Assert.LessOrEqual(policy.Toggle.worldBound.xMax, board.Battles.Root.worldBound.xMax + 1, "Private card setting outside the battle window");
                Assert.AreEqual(board.Battles.Root.Q<Button>(className: "close-battle").resolvedStyle.height, policy.Toggle.resolvedStyle.height, .5f,
                    "The setting is sized like the header's other controls.");
                Assert.AreEqual("Ask for cards", policy.Toggle.text);
                Assert.LessOrEqual(policy.Toggle.MeasureTextSize(policy.Toggle.text, 0, VisualElement.MeasureMode.Undefined, 0,
                    VisualElement.MeasureMode.Undefined).x, policy.Toggle.contentRect.width + 1);
            }
            // UI-207: figures read at a glance; the short viewport keeps them one size smaller. Panel scaling rounds a size
            // to whole physical pixels, up to half a pixel (0.4 point at 125%).
            var compact = board.Root.ClassListContains("short-viewport");
            foreach (var figure in board.Tray.Query(className: "battle-figure").ToList().Where(Shown))
            {
                Assert.GreaterOrEqual(figure.resolvedStyle.width + .5f, compact ? 26 : 36, kind + " " + figure.name);
                Assert.GreaterOrEqual(figure.resolvedStyle.height + .5f, compact ? 34 : 48, kind + " " + figure.name);
            }
            foreach (var figure in board.Tray.Query<Button>(className: "loss-figure").ToList())
            {
                Assert.GreaterOrEqual(figure.resolvedStyle.width + .5f, compact ? 42 : 52, kind + " " + figure.name);
                Assert.GreaterOrEqual(figure.resolvedStyle.height + .5f, compact ? 46 : 58, kind + " " + figure.name);
            }
            Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2, kind.ToString());
            Assert.IsEmpty(board.Tray.Query<ScrollView>().ToList(), "The current decision never requires scrolling; history may scroll.");
            Assert.IsEmpty(board.Tray.Query<TextField>().ToList());
        }

        // UI-208: the losing side's composition is hidden while its loss figures stand in the column.
        private static bool Shown(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 3; i++)
                yield return null;
        }
    }
}
