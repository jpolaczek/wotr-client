using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Owns board furniture only. Decisions continue through their existing panels.
    public sealed class BoardEdgeLayout
    {
        public VisualElement Left { get; } = new VisualElement { name = "board-left-tabs", pickingMode = PickingMode.Ignore };
        public VisualElement Right { get; } = new VisualElement { name = "board-right-panels", pickingMode = PickingMode.Ignore };
        // UI-98: the top right corner of the map, under the bar and over the hand: a small tab with the
        // Fellowship (UI-160: the Hunt Box moved to the turn console), and over it the full Fellowship and Hunt panels.
        public VisualElement Dropdowns { get; } = new VisualElement { name = "board-dropdowns", pickingMode = PickingMode.Ignore };
        public Button Corner { get; } = new Button { name = "edge-corner-tab" };
        public VisualElement CornerPanels { get; } = new VisualElement { name = "edge-corner-panels", pickingMode = PickingMode.Ignore };
        public LeftRailPreview Preview { get; } = new LeftRailPreview();
        private const string RightGroup = "right";
        // UI-190: the space kept between a left panel and the bottom of the map when it moves up to stay whole.
        private const float BottomMargin = 8;
        private const string CornerKey = "corner";
        private readonly BoardScreen board;
        private readonly VisualElement stage;
        private string match;
        private Side? side;

        public BoardEdgeLayout(BoardScreen board, VisualElement stage)
        {
            this.board = board;
            this.stage = stage;
            board.Root.AddToClassList("edge-board");
            Left.AddToClassList("board-left-tabs");
            // UI-223: a usable tab pulses gently; the class only matters under .motion-enabled (LeftRail.uss).
            Left.schedule.Execute(() => Left.ToggleInClassList(TabPulseClass)).Every(MapTargetGlow.PulseMilliseconds);
            Right.AddToClassList("board-right-panels");
            Left.AddToClassList("map-edge-control");
            Right.AddToClassList("map-edge-control");
            Dropdowns.AddToClassList("board-dropdowns");
            Dropdowns.AddToClassList("map-edge-control");
            Left.style.display = Right.style.display = Dropdowns.style.display = DisplayStyle.None;
            stage.Add(Left);
            board.Root.Add(Right);
            Right.PlaceBehind(stage);
            board.Root.Add(Dropdowns);
            Dropdowns.PlaceBehind(stage);
            // UI-107: Politics, Characters, then Effects from the top of the rail; UI-179/UI-181: Reserve under Politics.
            AddTab(board.Nations.Root, "Politics", "politics");
            AddTab(board.Reinforcements.Tab, "Reserve", "reinforcements");
            AddTab(board.Characters.Tab, "Characters", "characters");
            AddTab(board.TableEffects.Tab, "Effects", "effects");
            DockLeft(board.Nations.Drawer, "politics");
            DockLeft(board.Reinforcements.Root, "reinforcements");
            DockLeft(board.TableEffects.Root, "effects");
            DockLeft(board.Characters.Root, "characters");
            AlignWithTab(board.Nations.Root, board.Nations.Drawer);
            AlignWithTab(board.Reinforcements.Tab, board.Reinforcements.Root);
            AlignWithTab(board.TableEffects.Tab, board.TableEffects.Root);
            AlignWithTab(board.Characters.Tab, board.Characters.Root);
            board.TableEffects.Docked = true;
            board.Nations.Opening += () => SelectLeft(board.Nations.Drawer);
            board.Reinforcements.Opening += () => SelectLeft(board.Reinforcements.Root);
            board.TableEffects.Opening += () => SelectLeft(board.TableEffects.Root);
            board.Characters.Opening += () => SelectLeft(board.Characters.Root);
            // UI-92/UI-98: hovering a tab unrolls its panel, interactive at once; a click never pins it.
            Preview.Blocked = () => board.Map.Panning;
            Preview.Add("politics", board.Nations.Root, board.Nations.Drawer, board.Nations.Open, board.Nations.Close);
            // UI-181: a reserve the player chose in the Reserve tab holds it while the figures are placed.
            Preview.Add("reinforcements", board.Reinforcements.Tab, board.Reinforcements.Root, board.Reinforcements.Open,
                board.Reinforcements.Collapse, LeftRailPreview.LeftGroup, () => board.Reinforcements.Held, null);
            // A left panel that comes to host the decision tray stays open, as if its context had opened it.
            Preview.Add("effects", board.TableEffects.Tab, board.TableEffects.Root, board.TableEffects.Open, board.TableEffects.Close,
                LeftRailPreview.LeftGroup, () => board.Tray.parent == board.TableEffects.DecisionHost, null);
            Preview.Add("characters", board.Characters.Tab, board.Characters.Root, board.Characters.Open, board.Characters.Close,
                LeftRailPreview.LeftGroup, () => board.Tray.parent == board.Characters.DecisionHost, null);

            board.Fellowship.Docked = true;
            BuildCorner();
            DockDropdown(board.Fellowship.Root, "fellowship");
            board.Fellowship.VisibilityChanged += RefreshCorner;
            // UI-98: hovering the corner tab unrolls the Fellowship like a left tab; a decision it hosts holds it.
            // UI-162: the Hunt is a window of its own, opened from the console.
            Preview.Add(CornerKey, Corner, CornerPanels, OpenCorner, CloseCorner,
                RightGroup, () => board.Fellowship.Held, CloseCorner);
            // UI-220: while hiding is legal, the plaque's click hides the Fellowship instead of unrolling the panel.
            board.Fellowship.Status.clickable = new Clickable(() =>
            {
                if (!board.Fellowship.RequestHide())
                    Preview.Show(CornerKey);
            });
            // UI-91: no "Cards N" header or help link; the hand itself is the permanent bottom of the column.
            var cards = new VisualElement { name = "edge-cards" };
            cards.AddToClassList("edge-cards");
            board.HandDrawer.Docked = true;
            board.HandDrawer.Tab.style.display = DisplayStyle.None;
            cards.Add(board.Hand);
            Right.Add(cards);
            board.Hand.AddToClassList("edge-hand");
            board.HandDrawer.InspectionBoundary = Right;
            board.HandDrawer.Header.style.display = DisplayStyle.None;
            UnmarkHandles(board.Hand);
            stage.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var top = board.Root.WorldToLocal(stage.worldBound.position).y;
                board.Root.EnableInClassList("compact-edge-board", board.Root.contentRect.height - top < 700);
                Right.style.top = top;
                FitHand();
            });
            // The panels hang from the bar itself, beside the notification bar, which ends at the column.
            board.TopBar.Root.RegisterCallback<GeometryChangedEvent>(e => Dropdowns.style.top = e.newRect.yMax);
            Right.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                board.Cards.SetDefaultAnchor(new Vector2(Right.layout.width + 8, 8));
                FitHand();
            });
            board.Hand.RegisterCallback<GeometryChangedEvent>(_ => FitHand());
            Corner.RegisterCallback<GeometryChangedEvent>(_ => FitHand());
            CornerPanels.RegisterCallback<GeometryChangedEvent>(_ => FitHand());

            board.Victory.MenuOnly = true;
            board.Menu.VictoryRequested += board.Victory.Open;
            board.Menu.VictoryRequested += () => board.Victory.Root.Focus();
        }

        public const string TabPulseClass = "edge-tabs-pulse";

        private void AddTab(Button tab, string title, string icon)
        {
            tab.AddToClassList("edge-tab");
            // UI-92: hovering previews the panel itself; a tooltip would only cover it.
            tab.tooltip = "";
            tab.Add(Icon("edge-tab-icon", icon));
            Left.Add(tab);
        }

        // The Fellowship plaque sits in the corner tab; its full panel covers the tab while unrolled. UI-160/UI-162:
        // the Hunt plaque lives in the turn console and opens the Hunt window.
        private void BuildCorner()
        {
            Corner.AddToClassList("edge-corner-tab");
            Corner.Add(board.Fellowship.Status);
            CornerPanels.AddToClassList("edge-corner-panels");
            CornerPanels.style.display = DisplayStyle.None;
            Dropdowns.Add(Corner);
            Dropdowns.Add(CornerPanels);
        }

        private void DockDropdown(VisualElement panel, string icon)
        {
            panel.AddToClassList("edge-dropdown-panel");
            panel.style.left = panel.style.top = panel.style.right = panel.style.bottom = StyleKeyword.Null;
            CornerPanels.Add(panel);
            UnmarkHandles(panel);
            panel.Q(className: "panel-header")?.Insert(0, Icon("panel-header-icon", icon));
        }

        private void OpenCorner() => board.Fellowship.Open();

        private void CloseCorner() => board.Fellowship.Close();

        // The full panel unrolls from the corner tab whenever it opens.
        private void RefreshCorner()
        {
            var open = board.Fellowship.IsOpen;
            var shown = CornerPanels.style.display.value == DisplayStyle.Flex;
            CornerPanels.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            Corner.EnableInClassList(EdgePlaque.OpenClass, open);
            if (open && !shown)
                LeftRailPreview.Unroll(CornerPanels);
            FitHand();
        }

        // UI-190 (user decision): a left panel opens level with its own tab, not at the top of the rail; it moves up
        // only as far as it must to stay whole above the bottom of the map.
        private void AlignWithTab(VisualElement tab, VisualElement panel)
        {
            panel.RegisterCallback<GeometryChangedEvent>(_ => PlaceBesideTab(tab, panel));
            tab.RegisterCallback<GeometryChangedEvent>(_ => PlaceBesideTab(tab, panel));
        }

        private void PlaceBesideTab(VisualElement tab, VisualElement panel)
        {
            var parent = panel.parent;
            if (parent == null || panel.resolvedStyle.display == DisplayStyle.None || panel.layout.height <= 0)
                return;
            var level = parent.WorldToLocal(tab.worldBound.position).y;
            var lowest = parent.contentRect.height - panel.layout.height - BottomMargin;
            var highest = parent.WorldToLocal(Left.worldBound.position).y;
            var top = Mathf.Max(highest, Mathf.Min(level, lowest));
            if (Mathf.Abs(panel.layout.y - top) > 0.5f)
                panel.style.top = top;
        }

        private static void DockLeft(VisualElement panel, string icon)
        {
            panel.AddToClassList("edge-left-panel");
            panel.style.left = panel.style.top = panel.style.right = panel.style.bottom = StyleKeyword.Null;
            UnmarkHandles(panel);
            panel.Q(className: "panel-header")?.Insert(0, Icon("panel-header-icon", icon));
        }

        // Artwork comes from Styles/LeftRail.uss and Styles/RightColumn.uss; icons never take the pointer.
        private static VisualElement Icon(string role, string key)
        {
            var icon = new VisualElement { name = role + "-" + key, pickingMode = PickingMode.Ignore };
            icon.AddToClassList(role);
            icon.AddToClassList(role + "-" + key);
            return icon;
        }

        private static void UnmarkHandles(VisualElement panel)
        {
            foreach (var handle in panel.Query(className: FloatingWindow.HandleClass).ToList())
            {
                handle.RemoveFromClassList(FloatingWindow.HandleClass);
                handle.AddToClassList("panel-header");
                handle.tooltip = "";
            }
        }

        private void SelectLeft(VisualElement selected)
        {
            LeftRailPreview.Unroll(selected);
            if (selected != board.Nations.Drawer)
                board.Nations.Close();
            if (selected != board.Reinforcements.Root)
                board.Reinforcements.Collapse();
            if (selected != board.TableEffects.Root)
                board.TableEffects.Close();
            if (selected != board.Characters.Root)
                board.Characters.Close();
        }

        // The column holds the hand under the corner tab. Panels held by a decision or opened by code take the
        // column as the permanent panels did, so a full hand moves aside; a hovered preview only lies over it.
        private void FitHand()
        {
            if (Right.contentRect.height <= 0 || board.Hand.layout.height <= 0)
                return;
            var occupied = Corner.worldBound.yMax;
            if (CornerPanels.style.display.value == DisplayStyle.Flex && !CornerPanels.ClassListContains(LeftRailPreview.PreviewClass))
                occupied = Mathf.Max(occupied, CornerPanels.worldBound.yMax);
            var cards = board.Hand.parent;
            var required = OuterHeight(board.Hand) + cards.resolvedStyle.paddingTop + cards.resolvedStyle.paddingBottom +
                Mathf.Max(0, occupied - Right.worldBound.yMin) + 12;
            var overflow = board.HandDrawer.IsOpen && required > Right.contentRect.height;
            board.Hand.EnableInClassList("edge-hand-overflow", overflow);
            board.HandDrawer.InspectionBoundary = overflow ? board.Hand : Right;
            // The hand extension stays above the dice and remains attached to Cards.
            if (overflow)
                board.Hand.style.bottom = Mathf.Max(0, Right.worldBound.yMax - stage.worldBound.yMax + 8);
            else
                board.Hand.style.bottom = StyleKeyword.Null;
        }

        private static float OuterHeight(VisualElement element) =>
            element.layout.height + element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom;

        public void Apply(PlayerView view)
        {
            Left.style.display = Right.style.display = Dropdowns.style.display = DisplayStyle.Flex;
            var changed = match != view.MatchId || side != view.You;
            match = view.MatchId;
            side = view.You;
            if (changed)
                board.HandDrawer.Open();
            FitHand();
        }
    }
}
