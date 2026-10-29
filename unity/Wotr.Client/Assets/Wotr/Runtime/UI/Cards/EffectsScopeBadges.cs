using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A parameter-wide influence gets one badge at that object, not at every region. UI-413 (owner): a nation-wide one gets
    // none on the Politics row, where the badge covered the nation's name; the Effects tab still lists it.
    internal sealed class EffectsScopeBadges
    {
        private readonly BoardScreen board;
        private readonly Dictionary<string, Button> badges = new Dictionary<string, Button>();

        public EffectsScopeBadges(BoardScreen board) => this.board = board;

        public void Apply(PlayerView view)
        {
            var scopes = new Dictionary<string, HashSet<string>>();
            foreach (var effect in view.Effects)
                foreach (var target in effect.Targets)
                {
                    if (target.Kind == "Fellowship")
                        Add(scopes, "fellowship", effect.SourceId);
                    else if (target.Kind == "Global")
                        Add(scopes, "global", effect.SourceId);
                }
            foreach (var key in badges.Keys.Where(k => !scopes.ContainsKey(k)).ToList())
            {
                badges[key].RemoveFromHierarchy();
                badges.Remove(key);
            }
            foreach (var scope in scopes)
            {
                var host = Host(scope.Key);
                if (host == null)
                    continue;
                if (!badges.TryGetValue(scope.Key, out var badge))
                {
                    badge = BoardScreen.Button("", board.TableEffects.Open);
                    badge.name = "effect-scope-" + scope.Key;
                    badge.AddToClassList("effect-scope-badge");
                    badge.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
                    badge.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
                    badges.Add(scope.Key, badge);
                    host.Add(badge);
                }
                badge.text = "◇" + (scope.Value.Count > 1 ? " " + scope.Value.Count : "");
                badge.tooltip = string.Join("\n", scope.Value.Select(id => view.Effects.First(e => e.SourceId == id).Title)) + "\nInspect active effects";
            }
        }

        private VisualElement Host(string key) => key == "fellowship"
            ? board.Fellowship.Status
            : board.TableEffects.Root.Q(className: "window-header");

        private static void Add(Dictionary<string, HashSet<string>> scopes, string key, string id)
        {
            if (!scopes.TryGetValue(key, out var ids))
            {
                ids = new HashSet<string>();
                scopes.Add(key, ids);
            }
            ids.Add(id);
        }
    }
}
