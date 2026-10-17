using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionContextRouter
    {
        private readonly BoardScreen board;
        private readonly ActionLogPanel panel;
        private VisualElement host;
        private ActionOpenRequest opened;
        public ActionContextModel Context { get; private set; }

        public ActionContextRouter(BoardScreen board)
        {
            this.board = board;
            panel = board.Log;
            panel.Presented += Present;
            panel.CardClosed += Clear;
            panel.CardOpening += Clear;
        }

        public void Open(ActionOpenRequest request) => panel.Request(request, true);

        private void Present(ActionLogView view, bool complete)
        {
            if (!panel.Pinned || !complete || opened == panel.Selection) return;
            opened = panel.Selection;
            Context = ActionContextModel.For(view, opened.StepId);
            if (Context.InGame.Host == InGameActionHost.Default && Context.Kind == ActionContextKind.Battle &&
                Context.BattleId == board.View?.Battle?.Id)
            {
                board.Battles.Open();
                board.Battles.HistoryContext(true);
                panel.Popup.BringToFront();
            }
            host = Context.InGame.Host switch
            {
                InGameActionHost.Effects => Visible(board.TableEffects.Root) ? board.TableEffects.Root : board.TableEffects.Tab,
                InGameActionHost.Sovereign => Visible(board.Characters.Root) ? board.Characters.Root : board.Characters.Tab,
                InGameActionHost.Resources => ResourceHost(),
                InGameActionHost.Victory => board.Victory.Tab,
                InGameActionHost.Fellowship => FellowshipHost(),
                _ => Host(Context.Kind)
            };
            host?.AddToClassList("history-context-host");
            panel.SetContextTitle(Title(Context) + " · Recorded action");
            board.Map.HistoryOverlay.Apply(Context);
            panel.PositionCard();
            var selection = opened;
            panel.Popup.schedule.Execute(() =>
            {
                if (opened != selection || panel.Selection != selection || Context == null) return;
                panel.PositionCard();
                Frame();
            });
        }

        private VisualElement Host(ActionContextKind kind) => kind switch
        {
            ActionContextKind.Battle when Context.BattleId == board.View?.Battle?.Id => board.Battles.Root,
            ActionContextKind.Fellowship => FellowshipHost(),
            ActionContextKind.Hunt => Visible(board.Hunt.Root) ? board.Hunt.Root : board.Hunt.Tab,
            ActionContextKind.Politics => Visible(board.Nations.Drawer) ? board.Nations.Drawer : board.Nations.Root,
            ActionContextKind.Cards => board.HandDrawer.Tab,
            ActionContextKind.Resources or ActionContextKind.Dice => ResourceHost(),
            ActionContextKind.Characters when Context.Regions.All(id => !board.Presentation.Shapes.ContainsKey(id)) => board.Characters.Tab,
            _ => board.Map
        };

        private string Title(ActionContextModel context)
        {
            if (context.InGame.Host != InGameActionHost.Default)
                return context.InGame.Title;
            if (context.Kind == ActionContextKind.Politics && context.NationId.Length > 0)
                return "Politics · " + FigureGrid.Name(context.NationId);
            if (context.Kind == ActionContextKind.Characters && context.DefinitionId.Length > 0)
                return "Characters · " + CharacterLibrary.Name(context.DefinitionId);
            if (context.Kind == ActionContextKind.Army)
            {
                var region = context.Regions.LastOrDefault(board.Presentation.Regions.ContainsKey);
                return region == null ? "Armies" : ActionCardText.Region(region, board.Presentation);
            }
            return context.Kind == ActionContextKind.General ? "Action" : context.Kind.ToString();
        }

        private VisualElement ResourceHost() => Context.InGame.Owner.HasValue && Context.InGame.Owner != board.View?.You
            ? board.OpponentDice : board.OwnDice;

        private VisualElement FellowshipHost() => Visible(board.Fellowship.Root) ? board.Fellowship.Root : board.Fellowship.Status;

        // UI-100: the centred card leaves a band on each side; the regions go to the largest band that shows them all.
        private void Frame()
        {
            foreach (var band in FreeAreas().OrderByDescending(band => band.width * band.height))
                if (board.Map.HistoryOverlay.Frame(band))
                    return;
        }

        private Rect[] FreeAreas()
        {
            var origin = board.Map.WorldToLocal(panel.Popup.parent.worldBound.position);
            var area = new Rect(origin, panel.Popup.parent.worldBound.size);
            // PositionCard writes style coordinates before UI Toolkit's next layout pass.
            // Use that requested location in the same frame as the area, not the previous layout.
            var next = new Vector2(panel.Popup.style.left.value.value, panel.Popup.style.top.value.value);
            var card = new Rect(origin + next, panel.CardSize);
            return new[]
            {
                ClearOfLog(Rect.MinMaxRect(area.xMin + 8, area.yMin + 8, card.xMin - 12, area.yMax - 8)),
                ClearOfLog(Rect.MinMaxRect(card.xMax + 12, area.yMin + 8, area.xMax - 8, area.yMax - 8)),
                ClearOfLog(Rect.MinMaxRect(area.xMin + 8, area.yMin + 8, area.xMax - 8, card.yMin - 12)),
                ClearOfLog(Rect.MinMaxRect(area.xMin + 8, card.yMax + 12, area.xMax - 8, area.yMax - 8))
            };
        }

        // The open log list trims the side of a band it covers; a band it fills is left empty.
        private Rect ClearOfLog(Rect space)
        {
            space.width = Mathf.Max(0, space.width);
            space.height = Mathf.Max(0, space.height);
            if (!panel.IsOpen)
                return space;
            var log = new Rect(board.Map.WorldToLocal(panel.Root.worldBound.position), panel.Root.worldBound.size);
            if (!log.Overlaps(space))
                return space;
            if (log.xMin <= space.xMin)
                space.xMin = Mathf.Min(space.xMax, log.xMax + 8);
            else if (log.xMax >= space.xMax)
                space.xMax = Mathf.Max(space.xMin, log.xMin - 8);
            return space;
        }

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;

        public void Clear()
        {
            if (host == board.Battles.Root) board.Battles.HistoryContext(false);
            host?.RemoveFromClassList("history-context-host");
            host = null;
            opened = null;
            Context = null;
            board.Map.HistoryOverlay.Apply(null);
            panel.SetContextTitle("Recorded action");
        }
    }
}
