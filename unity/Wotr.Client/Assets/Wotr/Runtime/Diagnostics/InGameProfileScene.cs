#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // The fixture is a side-filtered server projection. Dense cases exercise one public-value refresh.
    internal sealed class InGameProfileScene
    {
        private readonly BoardScreen board;
        private readonly string kind;
        private readonly Action<Button> click;
        private readonly Action<PlayerView> apply;
        private readonly PlayerView refresh;
        private readonly InGameProfilePanels panels;
        private readonly Button[] sovereigns;
        private readonly Button[] effectSources;
        private readonly string[] sovereignIds;
        private readonly Button resourceAnchor, effectAnchor;
        private int step;
        private bool prepared;
        private bool closedFellowship;
        private bool openedCharacters;
        private bool unfoldedEffects;
        private bool effectsPrepared;
        private string expectedSovereign = "";
        private Vector2 camera;

        public string DiagnosticState => $"kind={kind}, step={step}";

        public InGameProfileScene(BoardScreen board, PlayerView view, string kind, Action<Button> click,
            Action<PlayerView> apply, Action<ScrollView, float> wheel)
        {
            this.board = board;
            this.kind = kind;
            this.click = click;
            this.apply = apply;
            refresh = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            panels = new InGameProfilePanels(board, click);
            sovereignIds = view.Sovereigns.Select(sovereign => sovereign.Id).ToArray();
            sovereigns = sovereignIds.Select(id => board.Sovereigns.Root.Q<Button>("sovereign-" + id)).ToArray();
            resourceAnchor = board.OwnDice.Q<Button>("pool-Spent");
            effectSources = view.Effects.Select(effect => effect.SourceId).Distinct()
                .Select(source => board.TableEffects.Root.Q<Button>("effect-source-" + source)).ToArray();
            effectAnchor = effectSources.FirstOrDefault();
            if (sovereigns.Length != 5 || sovereigns.Any(button => button == null) || resourceAnchor == null ||
                effectSources.Length != 6 || effectSources.Any(button => button == null))
                throw new InvalidOperationException("Incomplete in-game stress projection or controls.");
        }

        public bool Tick(int frame)
        {
            if (frame % 15 != 0)
                return false;
            camera = board.Map.Camera.Pan;
            if (PrepareInspection())
                return true;
            if (kind == "effects")
                panels.Effects(step);
            else if (kind.StartsWith("resources-", StringComparison.Ordinal))
                panels.Resources(step);
            else if (kind == "victory")
                panels.Victory(step);
            else if (kind == "sovereigns")
                Sovereign(step);
            if (kind.StartsWith("dense-", StringComparison.Ordinal))
            {
                // Alternate one public value to measure a real incremental update of the dense board.
                refresh.Fellowship.Corruption = step % 2 == 0 ? 2 : 3;
                refresh.Revision++;
                apply(refresh);
            }
            step++;
            VerifyStableControls();
            return true;
        }

        private bool PrepareInspection()
        {
            closedFellowship = false;
            openedCharacters = false;
            unfoldedEffects = false;
            if (prepared)
                return false;
            if (kind.StartsWith("dense-", StringComparison.Ordinal) && !effectsPrepared)
            {
                // The dense board is measured with Effects unfolded by the player's gesture.
                click(board.TableEffects.Tab);
                effectsPrepared = true;
                unfoldedEffects = true;
                return true;
            }
            if (kind == "sovereigns" && board.Characters.Root.style.display.value == DisplayStyle.None)
            {
                // UI-101: the portraits are a section of the Characters panel; unroll it with the player's gesture.
                click(board.Characters.Tab);
                openedCharacters = true;
                return true;
            }
            prepared = true;
            if (!board.Fellowship.IsOpen || board.Fellowship.Docked)
                return false;
            // The recovered resource fixture retains its pending Fellowship decision.
            // Close its initial auto-open panel with the player's gesture during warmup.
            click(board.Fellowship.Root.Q<Button>("close-fellowship"));
            closedFellowship = true;
            VerifyPreparation();
            return true;
        }

        private void VerifyPreparation()
        {
            if (board.Fellowship.IsOpen || board.View.Revision != refresh.Revision ||
                board.View.Decision?.Id != refresh.Decision?.Id || board.View.Phase != refresh.Phase)
                throw new InvalidOperationException("Closing the initial inspection changed the pending decision.");
            if (board.Map.Camera.Pan != camera)
                throw new InvalidOperationException("Closing the initial inspection moved the map camera.");
            VerifyStableControls();
        }

        private void Sovereign(int index)
        {
            var selected = index % sovereigns.Length;
            expectedSovereign = sovereignIds[selected];
            // UI-101: an awakened sovereign the roster lists as a character is opened from that roster entry.
            var listed = sovereigns[selected].style.display.value == DisplayStyle.None;
            click(listed ? board.Characters.Root.Q<Button>("character-roster-" + expectedSovereign) : sovereigns[selected]);
            if (board.Characters.Inspector.FigureId != expectedSovereign)
                throw new InvalidOperationException("The sovereign portrait did not open its public character.");
        }

        public void VerifyRendered()
        {
            if (openedCharacters)
            {
                Visible(board.Characters.Root);
                return;
            }
            if (unfoldedEffects)
            {
                Visible(board.TableEffects.Root);
                return;
            }
            if (closedFellowship)
            {
                VerifyPreparation();
                return;
            }
            if (step == 0)
                return;
            if (kind == "effects" || kind.StartsWith("resources-", StringComparison.Ordinal) || kind == "victory")
                panels.VerifyRendered(kind);
            else if (kind == "sovereigns")
            {
                Visible(board.Characters.Inspector.Root);
                if (board.Characters.Inspector.FigureId != expectedSovereign)
                    throw new InvalidOperationException("The rendered sovereign changed unexpectedly.");
            }
            if (kind.StartsWith("dense-", StringComparison.Ordinal))
            {
                var corruption = board.Fellowship.IsOpen
                    ? board.Fellowship.Root.Q<Label>("corruption-label")
                    : board.Fellowship.Status.Q<Label>("fellowship-status-corruption");
                Visible(corruption);
                if (corruption.text != refresh.Fellowship.Corruption.ToString())
                    throw new InvalidOperationException("The changed public Corruption value did not render.");
                VerifyDenseTargets();
            }
            if (board.Map.Camera.Pan != camera)
                throw new InvalidOperationException("Inspection unexpectedly moved the map camera.");
            VerifyStableControls();
        }

        private void VerifyDenseTargets()
        {
            var visibleSources = 0;
            foreach (var source in effectSources)
            {
                if (source.parent.resolvedStyle.display == DisplayStyle.None)
                    continue;
                visibleSources++;
                VerifyTarget(source);
            }
            if (visibleSources != 4)
                throw new InvalidOperationException("Dense inspection must show the full page of four Effects sources.");
            VerifyTarget(board.Menu.Toggle);
            VerifyTarget(board.Characters.Tab);
            VerifyTarget(board.Hand.Query<Button>(className: "hand-card").First());
            if (board.Fellowship.IsOpen)
                VerifyTarget(board.Fellowship.Root.Q<Button>("focus-fellowship"));
        }

        private static void VerifyTarget(Button button)
        {
            var bounds = button.worldBound;
            VerifyTarget(button, bounds.min + Vector2.one);
            VerifyTarget(button, bounds.max - Vector2.one);
            VerifyTarget(button, bounds.center);
            VerifyTarget(button, new Vector2(bounds.xMin + 1, bounds.yMax - 1));
            VerifyTarget(button, new Vector2(bounds.xMax - 1, bounds.yMin + 1));
        }

        private static void VerifyTarget(Button button, Vector2 point)
        {
            var picked = button.panel.Pick(point);
            if (picked != button && !button.Contains(picked))
                throw new InvalidOperationException($"Dense target {button.name} is covered by {picked?.name} at {point}.");
        }

        private void VerifyStableControls()
        {
            if (board.OwnDice.Q<Button>(resourceAnchor.name) != resourceAnchor ||
                board.TableEffects.Root.Q<Button>(effectAnchor.name) != effectAnchor)
                throw new InvalidOperationException("Unrelated refresh replaced a stable source/resource control.");
            for (var index = 0; index < sovereigns.Length; index++)
                if (board.Sovereigns.Root.Q<Button>(sovereigns[index].name) != sovereigns[index])
                    throw new InvalidOperationException("Unrelated refresh replaced a sovereign portrait.");
        }

        internal static void Visible(VisualElement element)
        {
            if (!BoardProfileCheck.Visible(element))
                throw new InvalidOperationException("Profile inspection did not render: " + element?.name);
        }
    }
}
#endif
