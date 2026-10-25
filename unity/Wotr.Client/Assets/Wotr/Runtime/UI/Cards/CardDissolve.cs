using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-326 (owner 2026-10-25: "a small extra animation of disappearing cards at the moment a player discards them: just two
    // cards turned with their backs to the player that dissolve in the air"): two card backs, slightly fanned, rise a little
    // from where the cards leave and fade and soften away over 0.6 s (Cards.uss), above the HUD and never taking the pointer
    // — one pair per discard, whatever the number of cards; nothing under Skip animations.
    // The player's own discard is marked when its cards are chosen (CardContextInteraction) and plays when a view without
    // them arrives; a card still in the hand at a newer revision is forgotten (the discard did not happen), and a quiet view
    // (a reconnect, the first view) forgets them all.
    public sealed class CardDissolve
    {
        public const string BackClass = "card-dissolve-back";
        public const string DissolvingClass = "dissolving";
        public const long Milliseconds = 600;
        private static readonly Vector2 BackSize = new Vector2(46, 64);
        private const float Rise = 26;
        private const float Fan = 8;
        private readonly Func<bool> reduced;
        private readonly Dictionary<string, (Rect Place, long Revision)> marked = new Dictionary<string, (Rect, long)>();
        public VisualElement Root { get; } = new VisualElement { name = "card-dissolve", pickingMode = PickingMode.Ignore };
        public int Shown => Root.Query(className: BackClass).ToList().Count;

        public CardDissolve(Func<bool> reduced)
        {
            this.reduced = reduced;
            Root.AddToClassList("card-dissolve");
        }

        // A hand card the player is discarding, where it stands now.
        public void Mark(string id, VisualElement card, long revision)
        {
            if (card != null && card.worldBound.width > 0 && card.worldBound.height > 0)
                marked[id] = (card.worldBound, revision);
        }

        // A new view: the marked cards that left the hand dissolve, once, where they stood.
        public void Settle(PlayerView view, bool quiet)
        {
            if (marked.Count == 0)
                return;
            var gone = marked.Where(pair => !view.Hand.Contains(pair.Key)).ToList();
            var stale = marked.Where(pair => quiet || view.Hand.Contains(pair.Key) && view.Revision > pair.Value.Revision).ToList();
            foreach (var pair in gone.Concat(stale))
                marked.Remove(pair.Key);
            if (!quiet && gone.Count > 0)
                Play(Union(gone.Select(pair => pair.Value.Place)));
        }

        // Two backs at the place, in world points; `scale` sizes them for a small source (the opponent's hand count).
        public void Play(Rect world, float scale = 1)
        {
            if (reduced() || Root.panel == null)
                return;
            Root.BringToFront();
            var centre = Root.WorldToLocal(world.center);
            foreach (var side in new[] { -1, 1 })
            {
                var back = Back(centre, BackSize * scale, side);
                Root.Add(back);
                Dissolve(back, side, scale);
            }
        }

        // Skip shows the latest state: nothing dissolves on after it.
        public void Clear() => Root.Clear();

        private static VisualElement Back(Vector2 centre, Vector2 size, int side)
        {
            var back = new VisualElement { pickingMode = PickingMode.Ignore };
            back.AddToClassList(BackClass);
            back.style.left = centre.x - size.x / 2 + side * size.x * .18f;
            back.style.top = centre.y - size.y / 2;
            back.style.width = size.x;
            back.style.height = size.y;
            back.style.rotate = new Rotate(side * Fan);
            return back;
        }

        // From its first frame the back eases up, fades and softens (the USS transition), then leaves.
        private static void Dissolve(VisualElement back, int side, float scale)
        {
            back.schedule.Execute(() =>
            {
                back.AddToClassList(DissolvingClass);
                back.style.translate = new Translate(side * 6 * scale, -Rise * scale);
            });
            back.schedule.Execute(back.RemoveFromHierarchy).ExecuteLater(Milliseconds + 100);
        }

        private static Rect Union(IEnumerable<Rect> places)
        {
            var all = places.ToList();
            return Rect.MinMaxRect(all.Min(r => r.xMin), all.Min(r => r.yMin), all.Max(r => r.xMax), all.Max(r => r.yMax));
        }
    }
}
