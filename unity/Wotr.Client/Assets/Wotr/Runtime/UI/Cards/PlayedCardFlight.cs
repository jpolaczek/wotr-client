using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-463 (owner, 2026-11-03: "let's add an animation when playing a card makes it go to Effects"): the player's own
    // card that stays on the table goes visibly to the Effects panel, as the opponent's does (UI-316). On UI-326's pattern
    // the play is marked where the card stands when it is sent (TurnInteraction, BoardScreen.MarkPlayedCard), and settles
    // with the view in which the card has left the hand and a new source of the player's own with its id stands among the
    // effects (EffectsPresentation.Sources): the card's stand-in — the parchment card with its title of UI-324 — appears at
    // that place and shrinks, eased as a presented card (CardFlight), into the Effects tab (its tile while the panel is
    // open); the tab then glows. Only its translate, scale and opacity move, above the HUD, never taking the pointer. A card
    // still in the hand at a newer revision is forgotten (the play did not happen), so is a card that left for anything
    // else once its resolution is over (an Event that does not stay, a discard); a quiet view (a reconnect, the first view)
    // forgets them all. Reduced motion (Skip animations): no flight, the tab's glow only.
    public sealed class PlayedCardFlight
    {
        public const string StandInName = "played-card-stand-in";
        public const double GlowSeconds = PresentationPace.HoldSeconds;
        private const long TickMilliseconds = 16;
        private readonly Func<bool> reduced;
        private readonly TableEffectsView effects;
        private readonly Dictionary<string, Marked> marked = new Dictionary<string, Marked>();
        private readonly ObjectGlow glow = new ObjectGlow();
        private readonly IVisualElementScheduledItem ticks;
        private Flight flight;
        private int lit;
        public VisualElement Root { get; } = new VisualElement { name = "played-card-flight", pickingMode = PickingMode.Ignore };
        public bool Flying => flight != null;
        public int Waiting => marked.Count;

        private sealed class Marked
        {
            public Rect? Place;
            public long Revision;
            public bool HadSource;
        }

        private sealed class Flight
        {
            public string Id;
            public VisualElement Card;
            public CardFlight Motion;
            public double Created;
            public double Started = -1;
        }

        public PlayedCardFlight(Func<bool> reduced, TableEffectsView effects)
        {
            this.reduced = reduced;
            this.effects = effects;
            Root.AddToClassList("played-card-flight");
            ticks = Root.schedule.Execute(Tick).Every(TickMilliseconds);
            ticks.Pause();
        }

        // A hand card the player is playing, where it stands now (no place when it is not shown).
        public void Mark(string id, VisualElement card, PlayerView view)
        {
            var place = card != null && CardFlight.Laid(card) ? card.worldBound : (Rect?)null;
            var known = Own(EffectsPresentation.Sources(view), view.You, id);
            marked[id] = new Marked { Place = place, Revision = view.Revision, HadSource = known };
        }

        // A new view: a marked card that left the hand for the player's own table flies to the Effects tab, once.
        public void Settle(PlayerView view, bool quiet)
        {
            if (marked.Count == 0)
                return;
            if (quiet)
            {
                marked.Clear();
                return;
            }
            var sources = EffectsPresentation.Sources(view);
            foreach (var pair in marked.ToList())
            {
                if (view.Hand.Contains(pair.Key))
                {
                    if (view.Revision > pair.Value.Revision)
                        marked.Remove(pair.Key);
                    continue;
                }
                if (Own(sources, view.You, pair.Key) && !pair.Value.HadSource)
                {
                    marked.Remove(pair.Key);
                    Play(pair.Key, pair.Value.Place);
                }
                else if (!Resolving(view, pair.Key))
                    marked.Remove(pair.Key);
            }
        }

        private static bool Own(IEnumerable<EffectSource> sources, Side you, string id) =>
            sources.Any(source => source.Owner == you && source.SourceId == id);

        // The card's resolution is still going on: it leads the effect trail.
        private static bool Resolving(PlayerView view, string id) => view.EffectTrail.Any(step => step.SourceId == id);

        private void Play(string id, Rect? place)
        {
            Land();
            if (reduced() || Root.panel == null || !place.HasValue)
            {
                Light();
                return;
            }
            Root.BringToFront();
            var card = StandIn(id, place.Value);
            flight = new Flight
            {
                Id = id, Card = card, Motion = new CardFlight(card, false), Created = Time.realtimeSinceStartupAsDouble
            };
            ticks.Resume();
        }

        private VisualElement StandIn(string id, Rect place)
        {
            var card = new VisualElement { name = StandInName, pickingMode = PickingMode.Ignore };
            card.AddToClassList(CardRevealPresenter.StandInName);
            card.AddToClassList(StandInName);
            card.Add(new Label(CardLibrary.Title(id)) { pickingMode = PickingMode.Ignore });
            var corner = Root.WorldToLocal(place.position);
            card.style.left = corner.x;
            card.style.top = corner.y;
            card.style.width = place.width;
            card.style.height = place.height;
            Root.Add(card);
            return card;
        }

        // Once it and its destination are laid out (a tile the view has just added waits a frame) the stand-in leaves.
        private void Tick()
        {
            if (flight == null)
            {
                ticks.Pause();
                return;
            }
            var now = Time.realtimeSinceStartupAsDouble;
            if (flight.Started < 0 && !Aim(now))
                return;
            var t = (now - flight.Started) / CardFlight.Seconds;
            flight.Motion.Leaving(t);
            if (t >= 1)
                Land();
        }

        private bool Aim(double now)
        {
            var destination = Destination(flight.Id);
            var laid = CardFlight.Laid(flight.Card) && CardFlight.Laid(destination);
            if (!laid && now - flight.Created < CardFlight.Seconds)
                return false;
            flight.Motion.To(destination);
            flight.Started = now;
            return true;
        }

        // The card's tile while the Effects panel is open, else the tab.
        private VisualElement Destination(string id)
        {
            var tile = effects.Root.Q("effect-source-" + id);
            return effects.IsOpen && tile != null && CardFlight.Laid(tile) ? tile : effects.Tab;
        }

        // The stand-in has landed (or gives way to the next one): it leaves and the tab glows.
        private void Land()
        {
            if (flight == null)
                return;
            flight.Card.RemoveFromHierarchy();
            flight = null;
            ticks.Pause();
            Light();
        }

        private void Light()
        {
            var current = ++lit;
            glow.Follow(effects.Tab);
            effects.Tab.schedule.Execute(() =>
            {
                if (current == lit)
                    glow.End();
            }).StartingIn((long)(GlowSeconds * 1000));
        }
    }
}
