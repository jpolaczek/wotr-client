using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    internal sealed class EffectSourceDetails
    {
        public VisualElement Root { get; } = new VisualElement { name = "effects-source-details" };
        public bool ShowsCard { get; private set; }
        public event Action Rebuilt;
        private readonly Action<string> locate;
        private readonly EffectCardText card = new EffectCardText();
        private string signature = "";

        public EffectSourceDetails(Action<string> locate)
        {
            this.locate = locate;
            Root.style.display = DisplayStyle.None;
        }

        public void Show(EffectSource source, bool ended, MatchConfiguration configuration)
        {
            Root.style.display = source == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (source == null)
            {
                Card(false);
                return;
            }
            CardLibrary.Cards.TryGetValue(source.SourceId, out var text);
            var cardText = text == null ? "" : CardLibrary.EventText(text, configuration);
            var next = source.Key + ended + cardText + string.Join("|", source.Effects.Select(e => e.Id + ":" + e.Description + ":" +
                EffectsPresentation.Expiry(e) + ":" + string.Join(",", e.Targets.Select(t => t.Kind + ":" + t.Id + ":" + t.Label))));
            if (signature == next)
                return;
            signature = next;
            var focused = Root.panel?.focusController.focusedElement as VisualElement;
            var focusName = focused != null && Root.Contains(focused) ? focused.name : "";
            Root.Clear();
            Root.EnableInClassList("effect-ended", ended);
            Root.Add(Heading(source, ended, text != null));
            var scopes = new VisualElement();
            scopes.AddToClassList("effect-scopes");
            Root.Add(scopes);
            var located = new HashSet<string>();
            foreach (var group in source.Effects.GroupBy(e => (e.Description, e.Modifier, Expiry: EffectsPresentation.Expiry(e))))
                scopes.Add(Scope(source, group.ToList(), group.Key.Expiry, text != null, located));
            // UI-94: a card source shows its complete Event and Combat text here instead of in a card window.
            if (text != null)
            {
                card.Show(text, cardText, ended);
                Root.Add(card.Root);
            }
            ShowsCard = text != null;
            Rebuilt?.Invoke();
            Root.tooltip = string.Join("\n", source.Effects.Select(e => e.SourceReference).Where(s => s.Length > 0).Distinct());
            if (focusName.Length > 0)
                Root.Q(focusName)?.Focus();
        }

        private void Card(bool shown)
        {
            if (ShowsCard == shown)
                return;
            ShowsCard = shown;
            Rebuilt?.Invoke();
        }

        private static VisualElement Heading(EffectSource source, bool ended, bool card)
        {
            var heading = BoardScreen.Row("effect-detail-heading");
            heading.Add(new Label(source.Title) { name = "effect-detail-title" });
            heading.Add(new Label(State(source, ended, card)) { name = "effect-detail-state" });
            return heading;
        }

        // UI-94: above a card's complete text a scope drops its summary, which the card states, and keeps
        // its targets and expiry on one wrapping row.
        private VisualElement Scope(EffectSource source, IReadOnlyList<ActiveEffectView> effects, string expiry, bool card,
            HashSet<string> located)
        {
            var block = new VisualElement();
            block.AddToClassList("effect-scope");
            block.EnableInClassList("effect-scope-compact", card);
            var first = effects[0];
            var description = card ? "" : source.Kind == "Ability" ? first.Description : Modifier(first.Modifier);
            if (description.Length > 0)
                block.Add(new Label(description) { name = "effect-description-" + first.Id });
            var targets = Targets(effects, description, located);
            block.Add(targets);
            var ending = new Label(expiry) { name = "effect-expiry-" + first.Id };
            ending.AddToClassList("effect-expiry");
            (card ? targets : block).Add(ending);
            return block;
        }

        private VisualElement Targets(IEnumerable<ActiveEffectView> effects, string description, HashSet<string> located)
        {
            var targets = BoardScreen.Row("effect-targets");
            foreach (var target in effects.SelectMany(e => e.Targets).GroupBy(t => (t.Kind, t.Id, t.Label)).Select(g => g.First()))
            {
                if (target.Kind == "Region")
                {
                    // UI-414 (owner): a place has its button once in a source, and none where the text already names it.
                    if (Names(description, target.Label) || !located.Add(target.Id))
                        continue;
                    var button = BoardScreen.Button("⌖ " + target.Label, () => locate(target.Id));
                    button.name = "effect-locate-" + target.Id;
                    button.tooltip = "Locate " + target.Label + " on the map";
                    targets.Add(button);
                }
                else
                    targets.Add(new Label(target.Label.Length > 0 ? target.Label : FigureGrid.Name(target.Kind)));
            }
            return targets;
        }

        private static bool Names(string description, string place) =>
            place.Length > 0 && description.IndexOf(place, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string State(EffectSource source, bool ended, bool card)
        {
            if (ended)
                return "Effect ended";
            return BoardScreen.SideName(source.Owner) + " · " + (card ? "In play" : source.Kind);
        }

        private static string Modifier(string value) => value switch
        {
            "PreventShadowArmyEntry" => "Shadow armies cannot enter or attack.",
            "RestrictRohanActivation" => "Rohan activation is restricted.",
            "PreventPassiveMusterAdvance" => "Inactive nations cannot advance with Muster.",
            "PreventFellowshipHuntDie" => "Fellowship movement does not add a Hunt die.",
            "PreventMinasTirithCombatCards" => "Free Peoples cannot play Combat cards in this siege.",
            _ => ""
        };
    }
}
