using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Rehosts the same decision controls. Opening never moves the camera; a new battle opens it (UI-149), otherwise
    // the player does.
    public sealed class RegionalBattlePanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "regional-battle" };
        private readonly VisualElement header;
        public bool IsOpen => active && open;
        public bool Pinned { get; private set; }
        public string Region => active ? region : "";
        public string BattleId { get; private set; } = "";
        public event System.Action Opening;
        public event System.Action Closed;
        public FloatingWindow Window { get; set; }
        private readonly VisualElement tray;
        private readonly VisualElement dock;
        private readonly MapCanvas map;
        private readonly Button close;
        private readonly Label heading;
        private readonly Label decision = new Label { name = "battle-decision-owner", enableRichText = false };
        // UI-115: the scene strip under the header names the battle mode in pictures and flanks the decision with the sides' emblems.
        private readonly VisualElement scene = new VisualElement { name = "battle-scene" };
        private readonly VisualElement attackerEmblem = new VisualElement { name = "battle-scene-attacker", pickingMode = PickingMode.Ignore };
        private readonly VisualElement defenderEmblem = new VisualElement { name = "battle-scene-defender", pickingMode = PickingMode.Ignore };
        private string sceneMode = "";
        private string scope = "";
        // UI-225: the battle (scope) the window's current place was found for.
        private string placedScope = "";
        private string region = "";
        private bool active;
        private bool open;
        private bool historyContext;
        private float cardPreviewWidth;
        private bool cardDrag;
        private bool combatChoice;

        public RegionalBattlePanel(VisualElement tray, VisualElement dock, MapCanvas map)
        {
            this.tray = tray;
            this.dock = dock;
            this.map = map;
            Root.AddToClassList("regional-battle");
            Root.style.display = DisplayStyle.None;
            close = IconButton.Create("close", "Close", Close);
            close.name = "close-battle";
            close.tooltip = "Close battle panel · Keep your selection";
            close.AddToClassList("close-battle");
            header = FloatingWindow.Header("Battle");
            heading = header.Q<Label>(className: "title");
            header.Add(close);
            Root.Add(header);
            scene.AddToClassList("battle-scene");
            attackerEmblem.AddToClassList("battle-scene-emblem");
            defenderEmblem.AddToClassList("battle-scene-emblem");
            scene.Add(attackerEmblem);
            scene.Add(decision);
            scene.Add(defenderEmblem);
            Root.Add(scene);
            Root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect))
                    Resized();
                map.Refresh();
            });
            map.RegisterCallback<GeometryChangedEvent>(_ => Position());
            map.CameraChanged += Position;
        }

        // UI-160: the private "skip cards this battle" setting belongs to the battle, in the window's header.
        public void AttachPolicy(VisualElement policy)
        {
            policy.AddToClassList("battle-window-policy");
            header.Insert(header.IndexOf(close), policy);
        }

        public void Apply(PlayerView view)
        {
            combatChoice = view.Decision?.Kind == DecisionKind.CombatCard && view.DecisionOwner == view.You;
            var next = view.EventLoss != null ? view.MatchId + "/loss/" + view.EventLoss.SourceId + "/" + view.EventLoss.RegionId
                : view.Battle != null ? view.MatchId + "/" + view.Battle.Id : "";
            var started = scope != next && view.Battle != null;
            if (scope != next)
            {
                Close();
                HistoryContext(false);
            }
            scope = next;
            active = next.Length > 0;
            BattleId = view.Battle?.Id ?? "";
            region = view.EventLoss?.RegionId ?? view.Battle?.RegionId ?? "";
            Root.EnableInClassList("has-battle", view.Battle != null);
            var destination = active ? Root : dock;
            if (tray.parent != destination)
                destination.Add(tray);
            if (active)
            {
                heading.text = map.Data.Regions[region].Name + (view.Battle == null ? " · Event losses" :
                    " · " + view.Battle.Mode + " · Round " + view.Battle.Round);
                decision.text = view.Decision != null ? "You · " + DecisionNoticeText.Describe(view) :
                    view.DecisionOwner.HasValue ? BoardScreen.SideName(view.DecisionOwner.Value) + " is choosing" : "Resolving battle";
                Scene(view);
            }
            // UI-149: a new battle opens its window for both players; a window closed during the battle stays closed.
            if (started)
                Open();
            Root.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // UI-206: the place decides the illustration; without one the mode decides the strip (UI-115, UI-120), and an event
        // loss without a battle keeps a plain strip.
        private void Scene(PlayerView view)
        {
            var mode = view.Battle == null ? "" : BattleSceneArtwork.Key(view.Battle.Mode);
            var illustration = view.Battle == null ? "" : BattleSceneArt.Key(map.Data.Regions[region], map.Data.Regions.Values);
            if (sceneMode != mode + "/" + illustration)
            {
                foreach (var name in scene.GetClasses().Where(name => name.StartsWith("battle-scene-") && name != "battle-scene-emblem").ToList())
                    scene.RemoveFromClassList(name);
                if (mode.Length > 0) scene.AddToClassList("battle-scene-" + mode);
                sceneMode = mode + "/" + illustration;
                var painting = illustration.Length > 0 ? BattleSceneArtwork.Illustration(illustration) : null;
                // UI-120: a painted strip, when the atlas provides one, replaces the vector strip of the kit.
                var painted = painting == null && view.Battle != null ? BattleSceneArtwork.Texture(view.Battle.Mode) : null;
                var background = painting ?? painted;
                scene.style.backgroundImage = background == null ? StyleKeyword.Null : new StyleBackground(background);
                scene.EnableInClassList("battle-scene-illustrated", painting != null);
                scene.EnableInClassList("battle-scene-painted", painted != null);
                scene.EnableInClassList("battle-scene-illustration-" + illustration, painting != null);
            }
            var attacker = view.Battle?.Attacker.Side ?? view.You;
            Emblem(attackerEmblem, attacker, view.Battle == null ? "" : "Attacker · " + BoardScreen.SideName(attacker));
            var defender = view.Battle?.Defender.Side ?? view.You;
            Emblem(defenderEmblem, defender, view.Battle == null ? "" : "Defender · " + BoardScreen.SideName(defender));
            scene.tooltip = view.Battle == null ? "Event losses" : view.Battle.Mode + " battle · Round " + view.Battle.Round;
        }

        private static void Emblem(VisualElement emblem, Side side, string description)
        {
            emblem.EnableInClassList("side-free", side == Side.FreePeoples);
            emblem.EnableInClassList("side-shadow", side == Side.Shadow);
            emblem.style.display = description.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emblem.tooltip = description;
        }

        public bool SelectRegion(string id)
        {
            if (!IsRegion(id))
                return false;
            Open();
            return true;
        }

        public void Open()
        {
            Show(true, true);
        }

        public bool IsRegion(string id) => active && id == region;
        public void Preview() => Show(false);
        public void Pin() => Show(true);
        public void CloseTransient() { if (open && !Pinned) Close(); }

        private void Show(bool pinned, bool raise = false)
        {
            if (!active)
                return;
            var wasOpen = open;
            Pinned |= pinned;
            if (!open)
            {
                Opening?.Invoke();
                Root.BringToFront();
            }
            open = true;
            Root.style.display = DisplayStyle.Flex;
            if (raise) Root.BringToFront();
            if (!wasOpen)
                Position();
        }

        public void Close()
        {
            open = false;
            Pinned = false;
            Closed?.Invoke();
            Root.style.display = DisplayStyle.None;
            map.Refresh();
        }

        public void HistoryContext(bool shown)
        {
            historyContext = shown;
            if (!shown) Root.style.width = StyleKeyword.Null;
            Position();
        }

        public void CardPreview(float width)
        {
            if (Mathf.Abs(cardPreviewWidth - width) < .5f)
                return;
            cardPreviewWidth = width;
            // A Combat preview must not move the miniature between its two clicks.
            // Its space is already reserved when the decision opens.
            if (!combatChoice)
                Position();
        }

        public void PreviewDrag(DraftToken source)
        {
            var next = IsOpen && source?.Object == DraftObject.Card;
            if (cardDrag == next)
                return;
            cardDrag = next;
            if (!cardDrag)
                Position();
        }

        // UI-225: a window placed for this battle keeps its place when its content changes size (No card, a played card,
        // the opponent's choice); it is placed again only when it no longer fits there.
        private void Resized()
        {
            if (placedScope == scope && !historyContext && !cardDrag && Window?.Restore() != true &&
                MapPanelPlacement.Fits(map, Root, Root.layout))
                return;
            Position();
        }

        public void Position()
        {
            if (cardDrag)
                return;
            var previewWidth = combatChoice ? CardViewportLayout.MaximumInitialWidth : cardPreviewWidth;
            var reservedWidth = Mathf.Max(historyContext ? 414 : 0, previewWidth > 0 ? previewWidth + 24 : 0);
            Root.style.width = reservedWidth > 0
                ? new StyleLength(Mathf.Clamp(Root.parent.contentRect.width - reservedWidth, 520, 760))
                : StyleKeyword.Null;
            if (Window?.Restore() == true)
                return;
            if (!IsOpen || !map.Data.Shapes.TryGetValue(region, out var shape))
                return;
            var size = Root.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0 || float.IsNaN(size.y) || size.y <= 0)
                return;
            if (historyContext)
            {
                Root.style.left = Mathf.Max(8, Root.parent.contentRect.width - Root.style.width.value.value - 8);
                Root.style.top = 8;
                return;
            }
            var anchor = map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            var bounds = MapPanelPlacement.Near(map, Root, anchor, combatChoice ? reservedWidth : 0);
            Root.style.left = bounds.x;
            Root.style.top = bounds.y;
            placedScope = scope;
        }
    }
}
