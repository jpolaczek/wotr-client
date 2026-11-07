using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-516 (owner, 2026-11-07: "show better when a card leaves the table … it should disappear somehow, or be clearly
    // struck through, so it is seen to belong to the Effects tab"). In the open Effects panel the leaving card's tile is
    // struck through and dims, then fades where it stood; its neighbours close up after it. With the panel closed its
    // struck-through name stands beside the Effects tab while the tab's count steps down (ValuePulse), unless the
    // presentation of the opponent's action already named it there (UI-493). Under Skip animations the tile goes at once
    // and the name stands without a fade. Nothing of a leaving card takes the pointer or answers a query by its old name.
    public sealed class TableCardLeaving
    {
        public const string LeavingClass = "effect-leaving";
        public const string StrikeName = "effect-leaving-strike";
        public const string NoticeName = "effect-leaving-notice";
        public const long StrikeMilliseconds = 900;
        public const long NoticeMilliseconds = 2000;
        private const string Prefix = "leaving-";
        private readonly Button tab;

        public TableCardLeaving(Button tab) => this.tab = tab;

        public static string Struck(string title) => "<s>" + title + "</s>";

        // The tile stays where it stood, struck through, then fades and goes.
        public void Tile(VisualElement tile)
        {
            Retire(tile);
            var strike = new VisualElement { name = StrikeName, pickingMode = PickingMode.Ignore };
            strike.AddToClassList(StrikeName);
            tile.Add(strike);
            tile.AddToClassList(LeavingClass);
            if (!Appearance.Moving(tile))
            {
                tile.RemoveFromHierarchy();
                return;
            }
            tile.schedule.Execute(() => FadeAndGo(tile)).ExecuteLater(StrikeMilliseconds);
        }

        // The card's struck-through name beside the Effects tab, level with it.
        public void Notice(string title)
        {
            var host = tab.parent?.parent ?? tab.parent;
            if (host == null || float.IsNaN(tab.worldBound.width) || tab.worldBound.width <= 0)
                return;
            var notice = new Label(Struck(title)) { name = NoticeName, pickingMode = PickingMode.Ignore };
            notice.AddToClassList(NoticeName);
            var place = host.WorldToLocal(new Vector2(tab.worldBound.xMax + 6, tab.worldBound.yMin));
            notice.style.left = place.x;
            notice.style.top = place.y;
            host.Add(notice);
            var moving = Appearance.Moving(notice);
            if (moving)
                Appearance.Arrive(notice);
            notice.schedule.Execute(() =>
            {
                if (moving)
                    FadeAndGo(notice);
                else
                    notice.RemoveFromHierarchy();
            }).ExecuteLater(NoticeMilliseconds);
        }

        // Out of every query by its old name, and out of the pointer's way.
        private static void Retire(VisualElement tile)
        {
            foreach (var element in tile.Query<VisualElement>().ToList())
            {
                element.pickingMode = PickingMode.Ignore;
                if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith(Prefix))
                    element.name = Prefix + element.name;
            }
        }

        private static void FadeAndGo(VisualElement element)
        {
            Appearance.Display(element, false);
            element.schedule.Execute(element.RemoveFromHierarchy).ExecuteLater(Appearance.CloseMilliseconds + 100);
        }
    }
}
