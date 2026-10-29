using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-178: one nation of the Politics panel: its name, its track and the small arrow that moves it a step.
    // The track is a gilt rail with four stations (three steps and War), drawn by the style sheet; stations are
    // drop and click targets, not framed buttons. The nation's medallion stands on the station of its step. UI-197:
    // the medallion carries the nation's crest over a soft gilt glow, full and bright for an active nation, dimmed for
    // a passive one; the name column has no crest of its own.
    internal sealed class NationTrackRow
    {
        public const int SlotWidth = 36;
        public VisualElement Root { get; } = new VisualElement();
        public Button Token { get; }
        public Button Advance { get; private set; }
        // Expansions only (Narya): offered activation takes the arrow's place in the same column.
        public Button Activation { get; private set; }
        public NationView Nation { get; private set; }
        public bool Own { get; private set; }
        private const float GlowScale = 1.45f;
        private readonly Label label = new Label();
        private readonly VisualElement glow;
        private readonly Button[] slots = new Button[4];
        private readonly Action<string> advance;
        private bool highlighted;
        private string advanceHint = "";
        private bool advancing;
        private bool activating;

        public NationTrackRow(string id, Action<string> select, Action<string> advance, Action<string> activate)
        {
            this.advance = advance;
            Root.name = "nation-track-" + id;
            Root.AddToClassList("nation-track");
            label.AddToClassList("nation-name");
            label.text = id == "southrons" ? "S & E" : FigureGrid.Name(id);
            Root.Add(label);
            var track = BoardScreen.Row("nation-slots");
            track.Add(Rail());
            Root.Add(track);
            for (var step = 3; step >= 0; step--)
            {
                var position = step;
                slots[step] = Station(id, step, () => Choose(position));
                track.Add(slots[step]);
            }
            glow = IconGlow.Create("nation-glow-" + id);
            track.Add(glow);
            Token = BoardScreen.Button("", () => select(id));
            Token.name = "nation-status-" + id;
            Token.AddToClassList("nation-token");
            Token.userData = new DraftToken(DraftObject.Nation, id);
            Token.Add(Emblem(id));
            track.Add(Token);
            Root.Add(ActionColumn(id, activate));
            Root.RegisterCallback<GeometryChangedEvent>(_ => PositionToken());
        }

        private VisualElement ActionColumn(string id, Action<string> activate)
        {
            var column = new VisualElement { name = "nation-actions-" + id };
            column.AddToClassList("nation-actions");
            Advance = IconButton.Create("next", "Advance", () => { if (advancing) advance(id); });
            Advance.name = "nation-advance-" + id;
            Advance.AddToClassList("nation-advance");
            column.Add(Advance);
            Activation = IconButton.Create("confirm", "Activate", () => { if (activating) activate(id); });
            Activation.name = "nation-activate-" + id;
            Activation.AddToClassList("nation-activate");
            Activation.userData = id;
            Activation.style.visibility = Visibility.Hidden;
            column.Add(Activation);
            return column;
        }

        // The rail joins the first and the last station; the stations stand on it.
        private static VisualElement Rail()
        {
            var rail = new VisualElement { name = "nation-rail", pickingMode = PickingMode.Ignore };
            rail.AddToClassList("nation-rail");
            return rail;
        }

        private static Button Station(string id, int step, Action choose)
        {
            var slot = new Button(choose) { name = "political-slot-" + id + "-" + step };
            slot.AddToClassList("nation-slot");
            slot.EnableInClassList("nation-slot-war", step == 0);
            var mark = new VisualElement { pickingMode = PickingMode.Ignore };
            mark.AddToClassList("nation-station");
            slot.Add(mark);
            return slot;
        }

        // UI-197: the crest is the face of the nation's medallion on the track.
        private static VisualElement Emblem(string id)
        {
            var emblem = new VisualElement { name = "nation-emblem-" + id, pickingMode = PickingMode.Ignore };
            emblem.AddToClassList("nation-emblem");
            emblem.AddToClassList("nation-emblem-" + id);
            return emblem;
        }

        private void ShowSide(bool active)
        {
            Token.EnableInClassList("active-nation", active);
            Token.EnableInClassList("passive-nation", !active);
            glow.EnableInClassList("active-nation-glow", active);
        }

        public void Apply(NationView nation, Side side, Side you)
        {
            Nation = nation;
            Own = side == you;
            Root.EnableInClassList("opponent-nation", !Own);
            Root.EnableInClassList("shadow-nation", side == Side.Shadow);
            Root.EnableInClassList("nation-at-war", nation.StepsToWar == 0);
            // UI-327: an activation turns the token over to its active side; the first side is simply shown.
            var shown = Token.ClassListContains("active-nation") || Token.ClassListContains("passive-nation");
            if (shown && Token.ClassListContains("active-nation") != nation.Active)
                TokenFlip.Turn(Token, () => ShowSide(nation.Active));
            else
                ShowSide(nation.Active);
            PositionToken();
            label.tooltip = PoliticalActions.Describe(nation);
        }

        private void PositionToken()
        {
            if (Nation == null || Root.contentRect.height <= 0)
                return;
            var size = Mathf.Min(26, Root.contentRect.height - 4);
            Token.style.width = Token.style.minWidth = size;
            Token.style.height = size;
            var top = (Root.contentRect.height - size) / 2;
            var left = (3 - Nation.StepsToWar) * SlotWidth + (SlotWidth - size) / 2;
            Token.style.top = top;
            Token.style.left = left;
            PlaceGlow(left, top, size);
        }

        // The glow is a little larger than the medallion and centred on it.
        private void PlaceGlow(float left, float top, float size)
        {
            var halo = size * GlowScale;
            var offset = (halo - size) / 2;
            glow.style.width = halo;
            glow.style.height = halo;
            glow.style.left = left - offset;
            glow.style.top = top - offset;
        }

        // UI-144: every legal step and activation shows at once; the token needs no selecting click first.
        public void Refresh(NationRowActions actions)
        {
            highlighted = actions.Highlighted;
            advancing = actions.CanAdvance;
            advanceHint = actions.AdvanceHint;
            activating = actions.CanActivate;
            Token.EnableInClassList("selected", highlighted);
            Token.EnableInClassList("legal-target", advancing || actions.CardTarget);
            Root.EnableInClassList("selected-nation", highlighted);
            Token.tooltip = PoliticalActions.Describe(Nation) + "\n" + TokenHint(actions);
            if (Own && !Nation.Active)
                Token.tooltip += "\n" + PoliticalActions.ActivationSources;
            RefreshArrow(actions);
            for (var step = 0; step < slots.Length; step++)
                RefreshSlot(step);
        }

        // UI-178: the arrow keeps its place in every row, lit when the chosen die can move this nation and greyed
        // otherwise; an offered activation (Narya) stands in its place.
        private void RefreshArrow(NationRowActions actions)
        {
            Advance.style.visibility = actions.ActivationOffered ? Visibility.Hidden : Visibility.Visible;
            Advance.SetEnabled(advancing);
            Advance.EnableInClassList("legal-target", advancing);
            Advance.tooltip = !advancing ? ArrowHint() : advanceHint.Length > 0 ? advanceHint
                : "Move " + FigureGrid.Name(Nation.Id) + " one step toward war";
            Activation.style.visibility = actions.ActivationOffered ? Visibility.Visible : Visibility.Hidden;
            Activation.SetEnabled(activating);
            Activation.EnableInClassList("legal-target", activating);
            Activation.tooltip = actions.ActivationHint;
        }

        private string ArrowHint() =>
            !Own ? "Opponent nation · Read-only" :
            Nation.StepsToWar == 0 ? "This nation is already At War." :
            !Nation.Active && Nation.StepsToWar == 1 ? "Activate this nation before it can enter war." :
            "Select a legal Muster result to advance this nation.";

        private string TokenHint(NationRowActions actions) =>
            actions.CardTarget ? (actions.CardHint.Length > 0 ? actions.CardHint : "Choose this nation for the card.") :
            advancing && advanceHint.Length > 0 ? advanceHint :
            advancing ? "Click this token or its arrow to move it one step toward war. Dragging it one station on also moves it." :
            ArrowHint();

        // UI-410 (owner): no station glows — a lit circle on the track misled; the nation's token (and its arrow) shows the
        // step. The next station stays the drop target of a dragged token, marked only while the drag points at it.
        private void RefreshSlot(int step)
        {
            var next = advancing && step == Nation.StepsToWar - 1;
            var current = step == Nation.StepsToWar;
            var slot = slots[step];
            slot.EnableInClassList("destination-selected", next && highlighted);
            slot.EnableInClassList("passed-station", step > Nation.StepsToWar);
            slot.SetEnabled(next);
            slot.userData = next ? new DraftToken(DraftObject.PoliticalSlot, Nation.Id) : null;
            slot.tooltip = (step == 0 ? "At War" : step + " steps to war") + (current ? " · Current position" :
                next ? " · Advance this nation" :
                step == 0 && !Nation.Active ? " · Activation required" : "");
        }

        private void Choose(int step)
        {
            if (advancing && step == Nation.StepsToWar - 1)
                advance(Nation.Id);
        }
    }
}
