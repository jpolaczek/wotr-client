#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // Repeated recorded DTOs measure presentation volume, not simulated game progress.
    internal sealed class ActivityProfileScene
    {
        private readonly ActionLogPanel log;
        private readonly string kind;
        private readonly Action<Button> click;
        private readonly Event pointer = new Event();
        private string expected;
        private float previousOffset;
        private bool scrolled;

        public ActivityProfileScene(BoardScreen board, ActionLogPage history, string kind, Action<Button> click)
        {
            log = board.Log;
            this.kind = kind;
            this.click = click;
            var model = new ActionLogModel { Loaded = true };
            model.Merge(history);
            if (model.Rows.Count < 5000)
                throw new InvalidOperationException("The activity benchmark requires 5000 entries.");
            log.Bind(model);
            log.Inspect += (request, _) => log.ShowAction(model.Find(request.ActionId), true);
            log.SetStatus("5000 recorded workload entries", false, false);
            log.Open();
        }

        public bool Tick(int frame)
        {
            if (kind == "activity-scroll")
            {
                previousOffset = log.List.Root.scrollOffset.y;
                pointer.type = EventType.ScrollWheel;
                pointer.mousePosition = log.List.Root.contentViewport.worldBound.center;
                pointer.delta = new Vector2(0, 1);
                using var wheel = WheelEvent.GetPooled(pointer);
                log.List.Root.SendEvent(wheel);
                scrolled = true;
                return true;
            }
            if (frame % 15 != 0)
                return false;
            expected = log.Model.Rows[frame % 30 == 0 ? 0 : 1].Id;
            click(log.List.Root.Q<Button>("log-open-" + expected));
            return true;
        }

        public void VerifyRendered()
        {
            if (log.List.RenderedCount > 12 || log.List.RenderedCount == 0)
                throw new InvalidOperationException("History virtualization did not bound the rendered entries.");
            if (kind == "activity-scroll" && scrolled && log.List.Root.scrollOffset.y <= previousOffset)
                throw new InvalidOperationException("The wheel gesture did not visibly advance the history.");
            if (kind == "activity-card" && expected != null && (log.Card.Displayed?.Action.Id != expected ||
                log.Popup.resolvedStyle.display != DisplayStyle.Flex || log.Card.Root.worldBound.height <= 0))
                throw new InvalidOperationException("The history click did not render the requested action.");
        }
    }
}
#endif
