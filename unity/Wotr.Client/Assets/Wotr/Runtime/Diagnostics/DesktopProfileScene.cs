#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // Explicit presentation workload. It never connects, previews or sends a game command.
    internal sealed class DesktopProfileScene : IDisposable
    {
        public BoardScreen Board { get; } = new BoardScreen();
        private readonly string kind;
        private readonly GameApi api = new GameApi("http://localhost");
        private readonly MatchConnection connection;
        private readonly TurnInteraction interaction;
        private readonly Event pointer = new Event();
        private readonly string selectionButton;
        private bool armyPrepared;
        private string tooltipText;
        private readonly Button tooltipFellowship;
        private readonly Button tooltipSettings;
        private readonly Label tooltip;
        private readonly HuntProfileScene hunt;
        private readonly ActivityProfileScene activity;
        private readonly InGameProfileScene inGame;
        private readonly BoardProfileScene boardProfile;
        private readonly FigureMotionProfileScene motionProfile;
        private readonly string[] cards;
        private string expectedCard = "";
        private int cardIndex;

        public string DiagnosticState => motionProfile?.DiagnosticState ?? boardProfile?.DiagnosticState ?? inGame?.DiagnosticState ?? "";
        public object MotionEvidence => motionProfile?.Evidence;
        public void BeginMotionCapture() => motionProfile.BeginCapture();
        public void CaptureMotionFrame(string phase) => motionProfile.Capture(phase);

        public DesktopProfileScene(VisualElement parent, PlayerView view, string kind, ActionLogPage history = null,
            System.Collections.Generic.Dictionary<string, PlayerView> recordedViews = null)
        {
            this.kind = kind;
            parent.Add(Board.Root);
            connection = new MatchConnection(api, view.MatchId);
            interaction = new TurnInteraction(api, connection, Board);
            Apply(view);
            if (Array.IndexOf(FigureMotionProfileScene.Kinds, kind) >= 0)
                motionProfile = new FigureMotionProfileScene(Board, recordedViews, kind, Apply, Key);
            if (InGameProfileKinds.Contains(kind))
                inGame = new InGameProfileScene(Board, view, kind, Click, Apply, Wheel);
            if (Array.IndexOf(BoardProfileScene.Kinds, kind) >= 0)
                boardProfile = new BoardProfileScene(Board, kind, Click, Key, Hover);
            if (kind.StartsWith("activity-", StringComparison.Ordinal))
                activity = new ActivityProfileScene(Board, history, kind, Click);
            if (kind.StartsWith("hunt-", StringComparison.Ordinal) || kind == "mordor-refresh")
                hunt = new HuntProfileScene(Board, view, kind, Click, Apply);
            if (kind == "tooltip")
            {
                Board.Fellowship.Open();
                tooltipFellowship = Board.Fellowship.Root.Q<Button>("focus-fellowship");
                tooltipSettings = Board.Menu.Toggle;
                tooltip = parent.parent.Q<Label>("runtime-tooltip");
            }
            if (kind == "battle")
            {
                Board.Battles.Open();
                var regular = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular);
                selectionButton = "loss-" + regular.Id;
            }
            if (kind == "army")
                selectionButton = "hover-figure-" + view.Units.First(u => u.Side == view.You &&
                    u.Location == "minas-tirith" && u.Kind == UnitKind.Regular).Id;
            if (kind == "card")
            {
                cards = view.Hand.OrderByDescending(id => CardLibrary.EventText(CardLibrary.Cards[id], view.Configuration).Length).Take(2).ToArray();
                Board.HandDrawer.Open();
            }
        }

        public bool Tick(int frame)
        {
            if (motionProfile != null)
                return motionProfile.Tick(frame);
            if (boardProfile != null)
                return boardProfile.Tick(frame);
            if (inGame != null)
                return inGame.Tick(frame);
            if (activity != null)
                return activity.Tick(frame);
            if (kind == "map-idle")
                return false;
            if (kind == "map-zoom")
            {
                pointer.type = EventType.ScrollWheel;
                pointer.mousePosition = Board.Map.worldBound.center;
                pointer.delta = new Vector2(0, frame % 120 < 60 ? .025f : -.025f);
                using (var wheel = WheelEvent.GetPooled(pointer))
                    Board.Map.SendEvent(wheel);
                return true;
            }
            if (frame % 15 != 0)
                return false;
            if (hunt != null)
                return hunt.Tick();
            if (kind == "card")
            {
                if (frame % 30 == 0)
                {
                    MoveOutsideHand();
                    expectedCard = "";
                }
                else
                {
                    expectedCard = cards[cardIndex++ % cards.Length];
                    Click(Board.Hand.Q<Button>(expectedCard));
                    if (Board.ExpandedCard != expectedCard)
                        throw new InvalidOperationException("The hand gesture did not unfold the requested card.");
                }
                return true;
            }
            if (kind == "hover")
            {
                if (frame % 30 == 0)
                    BoardTerrainInput.Click(Board.Map, "minas-tirith");
                else
                    Board.Hover.Close();
                return true;
            }
            if (kind == "tooltip")
            {
                var target = frame % 30 == 0 ? tooltipFellowship : tooltipSettings;
                FocusTooltip(target);
                return true;
            }
            if (kind == "army" && !armyPrepared)
            {
                Click(Board.OwnDice.Q<Button>("army"));
                BoardTerrainInput.Click(Board.Map, "minas-tirith");
                armyPrepared = true;
                return true;
            }
            var name = selectionButton;
            var button = Board.Root.Q<Button>(name);
            if (button == null || !button.enabledInHierarchy || button.worldBound.width <= 0)
                throw new InvalidOperationException("Unavailable benchmark control: " + name);
            var selected = button.ClassListContains("selected");
            Click(button);
            var updated = Board.Root.Q<Button>(name);
            if (updated.ClassListContains("selected") == selected)
                throw new InvalidOperationException("Benchmark click did not update selection: " + name);
            return true;
        }

        public void VerifyRendered()
        {
            motionProfile?.Verify();
            activity?.VerifyRendered();
            hunt?.VerifyRendered();
            inGame?.VerifyRendered();
            boardProfile?.VerifyRendered();
            if (kind == "card" && (Board.ExpandedCard != expectedCard ||
                expectedCard.Length > 0 && !BoardProfileCheck.Visible(Board.Hand.Q<Button>(expectedCard)?.Q("hand-card-face"))))
                throw new InvalidOperationException("The Player did not render the requested card.");
            if (kind != "tooltip")
                return;
            if (tooltip == null || tooltip.resolvedStyle.display == DisplayStyle.None ||
                tooltip.resolvedStyle.visibility != Visibility.Visible || tooltip.text != tooltipText || tooltip.worldBound.width <= 0)
                throw new InvalidOperationException("The Player did not render the focused control's tooltip: " +
                    $"expected={tooltipText}, actual={tooltip?.text}, display={tooltip?.resolvedStyle.display}, " +
                    $"visibility={tooltip?.resolvedStyle.visibility}, focused={Application.isFocused}");
        }

        private void FocusTooltip(Button target)
        {
            if (target?.panel == null || !target.enabledInHierarchy || target.worldBound.width <= 0 ||
                string.IsNullOrEmpty(target.tooltip))
                throw new InvalidOperationException("Unavailable benchmark tooltip source: " + target?.name);
            var picked = target.panel.Pick(target.worldBound.center);
            if (picked != target && !target.Contains(picked))
                throw new InvalidOperationException($"Occluded benchmark tooltip source: {target.name}, picked {picked?.name}.");
            tooltipText = target.tooltip;
            // Focus has no hover dwell. The existing end-of-frame measurement still
            // includes dispatch, tooltip layout and rendering, without activating the control.
            target.Focus();
            if (target.panel.focusController.focusedElement != target)
                throw new InvalidOperationException("Benchmark focus did not reach tooltip source: " + target.name);
            if (Board.Menu.IsOpen)
                throw new InvalidOperationException("Reading benchmark metadata unexpectedly opened the Menu.");
        }

        private void MoveOutsideHand() => Hover(Board.Menu.Toggle);

        private void Hover(VisualElement target)
        {
            if (!BoardProfileCheck.Visible(target))
                throw new InvalidOperationException("Unavailable benchmark hover target: " + target?.name);
            var picked = target.panel.Pick(target.worldBound.center);
            if (picked != target && !target.Contains(picked))
                throw new InvalidOperationException("Occluded benchmark hover target: " + target.name);
            pointer.type = EventType.MouseMove;
            pointer.mousePosition = target.worldBound.center;
            using var move = PointerMoveEvent.GetPooled(pointer);
            target.panel.visualTree.SendEvent(move);
        }

        private void Click(Button button)
        {
            if (button == null || !button.enabledInHierarchy || button.worldBound.width <= 0)
                throw new InvalidOperationException($"Unavailable benchmark control: {button?.name}, " +
                    $"enabled={button?.enabledInHierarchy}, bounds={button?.worldBound}, handOpen={Board.HandDrawer.IsOpen}");
            var picked = button.panel.Pick(button.worldBound.center);
            if (picked != button && !button.Contains(picked))
                throw new InvalidOperationException($"Occluded benchmark control: {button.name} {button.worldBound}; " +
                    $"picked {picked?.name} ({picked?.GetType().Name}) {picked?.worldBound}, " +
                    $"parent {picked?.parent?.name}; map {Board.Map.worldBound}");
            pointer.mousePosition = button.worldBound.center;
            pointer.type = EventType.MouseMove;
            using (var move = PointerMoveEvent.GetPooled(pointer))
                button.panel.visualTree.SendEvent(move);
            pointer.button = 0;
            pointer.clickCount = 1;
            pointer.type = EventType.MouseDown;
            using (var down = PointerDownEvent.GetPooled(pointer))
                button.SendEvent(down);
            pointer.type = EventType.MouseUp;
            using (var up = PointerUpEvent.GetPooled(pointer))
                button.SendEvent(up);
        }

        private void Wheel(ScrollView scroll, float delta)
        {
            var point = scroll.contentViewport.worldBound.center;
            var picked = scroll.panel.Pick(point);
            if (picked != scroll && !scroll.Contains(picked))
                throw new InvalidOperationException($"Occluded benchmark navigation: {scroll.name}, picked {picked?.name}.");
            pointer.mousePosition = point;
            pointer.type = EventType.ScrollWheel;
            pointer.delta = new Vector2(0, delta);
            using (var wheel = WheelEvent.GetPooled(pointer))
                picked.SendEvent(wheel);
        }

        private void Apply(PlayerView view)
        {
            Board.Apply(view);
            interaction.Update(view);
        }

        private static void Key(VisualElement target, KeyCode code)
        {
            if (target?.panel == null || !BoardProfileCheck.Visible(target))
                throw new InvalidOperationException("Unavailable benchmark keyboard target: " + target?.name);
            target.Focus();
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = code }))
                target.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = code }))
                target.SendEvent(up);
        }

        public void Dispose()
        {
            interaction.Dispose();
            connection.Dispose();
            api.Dispose();
            Board.Root.RemoveFromHierarchy();
        }
    }
}
#endif
