using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class InGameDenseLayoutTests
    {
        [UnityTest]
        public IEnumerator BothRolesInspectDenseEffectsFullPoolsAndFiveSovereignsInFourLayouts() => Wait(CheckLayouts());

        private static async Task CheckLayouts()
        {
            using var room = await Connect("ui63-dense", ScenarioKind.Interaction, openPanels: false);
            foreach (var client in new[] { room.Fp, room.Sh })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        try
                        {
                            client.Panel.targetTexture = target;
                            client.Interface.Preferences.SetScale(scale);
                            client.Interface.Preferences.SetReducedMotion(scale > 1);
                            client.Interface.Windows.Clear();
                            await room.Settle();
                            await Inspect(room, client, target, width, scale);
                        }
                        finally
                        {
                            client.Panel.targetTexture = client.Viewport;
                            target.Release();
                            Object.Destroy(target);
                        }
                    }
        }

        private static async Task Inspect(MatchTestRoom room, MountedMatchClient client, RenderTexture target, int width, float scale)
        {
            var board = client.Board;
            board.Characters.Close();
            board.TableEffects.Close();
            await room.Settle();
            var revision = board.View.Revision;
            var camera = board.Map.Camera.Pan;
            var decision = board.View.Decision?.Id;
            var phase = board.View.Phase;
            var ownHand = board.View.Hand.ToArray();
            if (board.Fellowship.IsOpen)
            {
                board.Fellowship.Close();
                await room.Settle();
                Assert.IsFalse(board.Fellowship.Pinned);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(decision, board.View.Decision?.Id);
                Assert.AreEqual(phase, board.View.Phase);
            }
            var portraits = board.Sovereigns.Root.Query<Button>(className: "sovereign-portrait").ToList();
            var pool = board.OwnDice.Q<Button>("pool-Spent");
            Assert.AreEqual(5, portraits.Count);
            Assert.AreEqual(5, board.View.RulerTokens.Count);
            Assert.AreEqual(2, board.View.CouncilTokens.Count);
            Assert.AreEqual(6, board.View.Effects.Select(effect => effect.SourceId).Distinct().Count());
            Assert.IsTrue(board.View.Dice.Any(die => die.Source == "lords.die.narya" && die.Location == DieLocation.Unrolled));
            ExpansionMatrixLayoutTests.ResourcesFit(board);
            await Effects(room, board, target, width, scale);
            foreach (var portrait in portraits)
            {
                Press(await BoardNavigation.Sovereign(board, portrait.name.Substring("sovereign-".Length)));
                await room.Settle();
                Assert.AreEqual(portrait.name.Substring("sovereign-".Length), board.Characters.Inspector.FigureId);
                Inside(board.Characters.Inspector.Root.worldBound, board.Map.worldBound);
                Press(board.Characters.Inspector.Root.Q<Button>("close-character"));
                await room.Settle();
            }
            await DenseEffects(room, board, target, width, scale);
            board.Apply(board.View);
            await room.Settle();
            Assert.AreSame(pool, board.OwnDice.Q<Button>("pool-Spent"));
            foreach (var portrait in portraits)
                Assert.AreSame(portrait, board.Sovereigns.Root.Q<Button>(portrait.name));
            Assert.AreEqual(camera, board.Map.Camera.Pan);
            Assert.AreEqual(revision, room.Fp.Board.View.Revision);
            Assert.AreEqual(revision, room.Sh.Board.View.Revision);
            Assert.AreEqual(decision, board.View.Decision?.Id);
            Assert.AreEqual(phase, board.View.Phase);
            CollectionAssert.AreEqual(ownHand, board.View.Hand);
            if (board.View.You == Side.Shadow)
            {
                Assert.IsEmpty(board.View.OwnCompanionVersions);
                Assert.IsEmpty(board.View.OwnCompanions);
                Assert.IsFalse(board.View.OwnCharacterRoster.Any(row => row.Side == Side.FreePeoples));
            }
            LayoutTests.AssertNoDecisionScroll(board);
            ExpansionMatrixLayoutTests.ResourcesFit(board);
        }

        private static async Task Effects(MatchTestRoom room, BoardScreen board, RenderTexture target, int width, float scale)
        {
            Press(board.TableEffects.Tab);
            await room.Settle();
            Assert.AreEqual("Effects · 6", board.TableEffects.Tab.text);
            Inside(board.TableEffects.Root.worldBound, board.Map.worldBound);
            // UI-221: every source is in view at once; the header has only its close button beside the global scope badges.
            Assert.AreEqual(6, Sources(board));
            Assert.AreEqual(1, board.TableEffects.Root.Q(className: "window-header").Query<Button>().ToList()
                .Count(b => !b.ClassListContains("effect-scope-badge")));
            SovereignTargets(board);
            LayoutTests.Save(target, $"ui63-dense-effects-{board.View.You}-{width}-{scale:0.00}.png");
            Press(board.TableEffects.Root.Q<Button>("close-effects"));
            await room.Settle();
            foreach (var targetId in board.View.Effects.SelectMany(effect => effect.Targets)
                .Where(effect => effect.Kind == "Region").Select(effect => effect.Id).Distinct())
                Assert.IsNotNull(board.Map.Q<Label>("map-effects-" + targetId));
        }

        // The dense board with Effects unfolded: every source in view (UI-221), every control reachable (UI-173: no Help link).
        private static async Task DenseEffects(MatchTestRoom room, BoardScreen board, RenderTexture target, int width, float scale)
        {
            Press(board.TableEffects.Tab);
            await room.Settle();
            Assert.IsNull(board.TableEffects.Root.Q<Button>("help-for-effects"), "UI-173: panels have no Help link.");
            LayoutTests.Save(target, $"ui63-dense-effects-{board.View.You}-{width}-{scale:0.00}.png");
            Assert.IsTrue(board.TableEffects.IsOpen);
            Assert.AreEqual(6, Sources(board));
            foreach (var source in VisibleSources(board))
                Pickable(source);
            SovereignTargets(board);
            Pickable(board.Menu.Toggle);
            Pickable(board.Characters.Tab);
            Pickable(board.Hand.Query<Button>(className: "hand-card").First());
            // UI-98: full-panel controls are reachable while unfolded or held by a decision.
            if (board.Fellowship.IsOpen)
                Pickable(board.Fellowship.Root.Q<Button>("focus-fellowship"));
            Pickable(board.TableEffects.Root.Q<Button>("close-effects"));
            ExpansionMatrixLayoutTests.ResourcesFit(board);
            Press(board.TableEffects.Root.Q<Button>("close-effects"));
            await room.Settle();
        }

        private static int Sources(BoardScreen board)
        {
            var sources = VisibleSources(board);
            foreach (var source in sources)
            {
                Inside(source.worldBound, board.TableEffects.Root.worldBound);
                var measured = source.MeasureTextSize(source.text, source.contentRect.width, VisualElement.MeasureMode.Exactly,
                    0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.y, source.contentRect.height + 1, source.text);
            }
            return sources.Count;
        }

        private static List<Button> VisibleSources(BoardScreen board) =>
            board.TableEffects.Root.Query<Button>().ToList().Where(button => button.name.StartsWith("effect-source-") &&
                button.parent.resolvedStyle.display != DisplayStyle.None).ToList();

        private static void Press(Button button)
        {
            Assert.IsNotNull(button);
            Assert.IsTrue(button.enabledInHierarchy, button.name);
            PointerGestureEvents.ClickAt(button);
        }

        private static void Pickable(Button button)
        {
            var bounds = button.worldBound;
            foreach (var point in new[] { bounds.min + Vector2.one, bounds.max - Vector2.one, bounds.center,
                new Vector2(bounds.xMin + 1, bounds.yMax - 1), new Vector2(bounds.xMax - 1, bounds.yMin + 1) })
            {
                var picked = button.panel.Pick(point);
                Assert.IsTrue(picked == button || button.Contains(picked), button.name + " covered by " + picked?.name);
            }
        }

        // UI-101: the portraits stand in the Characters panel; its tab stays reachable beside dense panels.
        private static void SovereignTargets(BoardScreen board)
        {
            var portraits = board.Sovereigns.Root.Query<Button>(className: "sovereign-portrait").ToList();
            Assert.AreEqual(5, portraits.Count);
            foreach (var portrait in portraits)
            {
                Assert.IsTrue(portrait.enabledInHierarchy, portrait.name);
                Assert.IsTrue(board.Characters.Root.Contains(portrait), portrait.name);
            }
            Pickable(board.Characters.Tab);
        }

        private static void Inside(Rect child, Rect parent)
        {
            Assert.GreaterOrEqual(child.xMin, parent.xMin - 1);
            Assert.GreaterOrEqual(child.yMin, parent.yMin - 1);
            Assert.LessOrEqual(child.xMax, parent.xMax + 1);
            Assert.LessOrEqual(child.yMax, parent.yMax + 1);
        }
    }
}
