using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-492 (owner, 2026-11-04; audit gap 8): the dice a card rolls outside a battle (Athelas, Dreadful Spells, The Ents
    // Awake, Dead Men of Dunharrow, Rangers of the North…) were a line in the log; they became a plate of die faces.
    // UI-541 (owner, 2026-11-17: "a special window should open, the dice shown as usual, and then the animations run"):
    // they stand in a window of their own: the card's name and what its dice count, then the dice (the battle's faces, a
    // hit or success on its hit face) with the dice sound, then the result. The window is read-only (Skip and Esc are the
    // presentation's only controls); it stays open while the roll's effects play (the Ring on the corruption track, the
    // figures it takes) and closes when the next action begins, at the release or on Skip. It stands above the roll's
    // region (below it when there is no room) or in the middle of the safe area for a roll without one (a healing roll,
    // the Three Rings).
    public sealed class CardRollPresenter : ICuePresenter
    {
        public const double OpenSeconds = .4;
        public const double DiceSeconds = 1;
        public static double Seconds => OpenSeconds + DiceSeconds + PresentationPace.ResultSeconds;
        public const string WindowName = "presentation-card-dice";
        public const string TitleName = "presentation-card-dice-title";
        public const string RuleName = "presentation-card-dice-rule";
        public const string DiceName = "presentation-card-dice-row";
        public const string ResultName = "presentation-card-dice-result";
        private readonly BoardScreen board;
        private readonly MapObservedTrace trace;
        private readonly PresentationSoundSink sounds;
        private readonly Func<Rect> safe;
        private MapObservedTrace.TraceSet set;
        private string action = "";

        public CardRollPresenter(BoardScreen board, MapObservedTrace trace, PresentationSoundSink sounds, Func<Rect> safe)
        {
            this.board = board;
            this.trace = trace;
            this.sounds = sounds;
            this.safe = safe;
        }

        // The window on the board now, or null.
        public VisualElement Window { get; private set; }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (cue.Kind != PresentationCueKind.Roll || cue.Rolls.Count == 0)
                return null;
            Close();
            action = cue.ActionId;
            Window = Build(cue);
            var region = cue.Regions.FirstOrDefault(board.Map.Data.Shapes.ContainsKey);
            set = trace.Begin();
            if (region != null)
                set.Region(region);
            set.Window(region, Window, safe());
            Appearance.Arrive(Window);
            var window = Window;
            return new StepsBeat(now, () => { }, new (Action Start, double Seconds)[]
            {
                (() => { }, OpenSeconds),
                (() => ShowDice(window, cue), DiceSeconds),
                (() => Show(window, ResultName), PresentationPace.ResultSeconds)
            });
        }

        // UI-541: the window stays through its own action's effects; the next action's first cue closes it.
        public void Retire(PresentationCue cue)
        {
            if (Window != null && cue.ActionId != action)
                Close();
        }

        public void Released() => Close();

        private void Close()
        {
            set?.End();
            set = null;
            Window = null;
            action = "";
        }

        private void ShowDice(VisualElement window, PresentationCue cue)
        {
            sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id);
            Show(window, DiceName);
        }

        private static void Show(VisualElement window, string part) =>
            window.Q(part)?.RemoveFromClassList(WindowName + "-waiting");

        private static VisualElement Build(PresentationCue cue)
        {
            var window = new VisualElement { name = WindowName, pickingMode = PickingMode.Ignore };
            window.AddToClassList(WindowName);
            window.Add(Text(TitleName, cue.Source.Length > 0 ? CardLibrary.Title(cue.Source) : "Card roll"));
            window.Add(Text(RuleName, Rule(cue)));
            var dice = new VisualElement { name = DiceName, pickingMode = PickingMode.Ignore };
            dice.AddToClassList(DiceName);
            dice.AddToClassList(WindowName + "-waiting");
            foreach (var roll in cue.Rolls)
                dice.Add(Face(roll, cue.HitTarget));
            window.Add(dice);
            var result = Text(ResultName, Result(cue));
            result.AddToClassList(PresentationPace.ResultClass);
            result.AddToClassList(WindowName + "-waiting");
            window.Add(result);
            return window;
        }

        private static Label Text(string name, string text)
        {
            var label = new Label(text) { name = name, pickingMode = PickingMode.Ignore, enableRichText = false };
            label.AddToClassList(name);
            return label;
        }

        private static DiePips Face(int roll, int? hitTarget)
        {
            var face = new DiePips { Illustrated = true, pickingMode = PickingMode.Ignore };
            face.AddToClassList("battle-pips");
            face.AddToClassList(DiceName + "-die");
            face.EnableInClassList("battle-pips-hit", hitTarget.HasValue && roll >= hitTarget);
            face.Apply(roll);
            return face;
        }

        // What the dice count, in the card's own terms.
        public static string Rule(PresentationCue cue)
        {
            if (!cue.HitTarget.HasValue)
                return "Each die scores its number";
            var target = cue.HitTarget.Value + "+";
            return cue.RollKind switch
            {
                "HealingRoll" => "Each " + target + " heals one Corruption",
                "CandlesRoll" => "Each " + target + " adds one Corruption",
                "RingRecoveryRoll" => "Each " + target + " is a success",
                _ => "Hits on " + target
            };
        }

        // The roll's outcome: successes for the Fellowship's and the Rings' rolls, hits for an army's.
        public static string Result(PresentationCue cue)
        {
            var hits = cue.Hits ?? (cue.HitTarget.HasValue ? cue.Rolls.Count(roll => roll >= cue.HitTarget) : cue.Rolls.Sum());
            return cue.RollKind is "HealingRoll" or "CandlesRoll" or "RingRecoveryRoll"
                ? hits == 0 ? "No successes" : hits == 1 ? "1 success" : hits + " successes"
                : PresentationPace.ResultText(hits);
        }
    }

    // UI-541: registered first, so a card's dice window goes as the next action's first scene begins.
    public sealed class CardRollWindowRetirement : ICuePresenter
    {
        private readonly CardRollPresenter rolls;

        public CardRollWindowRetirement(CardRollPresenter rolls) => this.rolls = rolls;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            rolls.Retire(cue);
            return null;
        }
    }
}
