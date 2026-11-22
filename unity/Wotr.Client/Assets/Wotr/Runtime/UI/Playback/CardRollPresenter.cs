using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

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
    // UI-565 (owner, 2026-11-22: "an extra UI that shows what is drawn, as in Challenge of the King; the user must see the
    // draw clearly"): what a card draws at random stands in the same window, in the middle of the safe area: Challenge of
    // the King's three Hunt tiles turn up one by one with the card sound, then the result, and the Eyes that leave the game
    // dim; for Lure of the Ring a light runs over the Fellowship's Companions and stops on the one picked.
    public sealed class CardRollPresenter : ICuePresenter
    {
        public const double OpenSeconds = .4;
        public const double DiceSeconds = 1;
        public static double Seconds => OpenSeconds + DiceSeconds + PresentationPace.ResultSeconds;
        public const double DrawSeconds = 1.5;
        public const double PickStepSeconds = .12;
        public static double DrawnSeconds => OpenSeconds + DrawSeconds + PresentationPace.ResultSeconds;
        public const string WindowName = "presentation-card-dice";
        public const string TitleName = "presentation-card-dice-title";
        public const string RuleName = "presentation-card-dice-rule";
        public const string DiceName = "presentation-card-dice-row";
        public const string ResultName = "presentation-card-dice-result";
        public const string NoteName = "presentation-card-dice-note";
        public const string DrawnName = "presentation-card-dice-drawn";
        public const string ItemClass = "presentation-card-dice-item";
        public const string LitClass = "presentation-card-dice-item-lit";
        public const string OutClass = "presentation-card-dice-item-out";
        private readonly BoardScreen board;
        private readonly PresentationLayer layer;
        private readonly MapObservedTrace trace;
        private readonly PresentationSoundSink sounds;
        private readonly Func<Rect> safe;
        private MapObservedTrace.TraceSet set;
        private string action = "";

        public CardRollPresenter(BoardScreen board, PresentationLayer layer, MapObservedTrace trace, PresentationSoundSink sounds,
            Func<Rect> safe)
        {
            this.board = board;
            this.layer = layer;
            this.trace = trace;
            this.sounds = sounds;
            this.safe = safe;
        }

        // The window on the board now, or null.
        public VisualElement Window { get; private set; }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (cue.Kind == PresentationCueKind.RandomDraw)
                return BeginDraw(cue, now);
            if (cue.Kind != PresentationCueKind.Roll || cue.Rolls.Count == 0)
                return null;
            Close();
            action = cue.ActionId;
            Window = Build(cue, CardPlayNotes.Roll(board.View, cue.Source));
            var region = cue.Regions.FirstOrDefault(board.Map.Data.Shapes.ContainsKey);
            set = trace.Begin();
            if (region != null)
                set.Region(region);
            set.Window(region, Window, safe());
            Appearance.Arrive(Window);
            var window = Window;
            var dice = new List<(Action Start, double Seconds)>
            {
                (() => ShowDice(window, cue), DiceSeconds),
                (() => ShowResult(window), PresentationPace.ResultSeconds)
            };
            return Opened(now, cue, "Roll", true, () => window.Q(DiceName), dice);
        }

        // UI-570: the player's own roll or draw waits behind its button (Settings › Play); every other one opens and plays.
        private IPresentationBeat Opened(double now, PresentationCue cue, string gate, bool die, Func<VisualElement> anchor,
            List<(Action Start, double Seconds)> steps)
        {
            if (Gated(cue))
                return new DrawGate(layer, gate, die, anchor, at => new StepsBeat(at, () => { }, steps));
            steps.Insert(0, (() => { }, OpenSeconds));
            return new StepsBeat(now, () => { }, steps);
        }

        // The player's own: a card of their side played in their own revision (or, with no side, their own revision).
        private bool Gated(PresentationCue cue)
        {
            if (!board.ConfirmsOwnDraws || !cue.OwnCommand)
                return false;
            var owner = CardLibrary.Owner(cue.Source);
            return owner == null || owner == board.View?.You;
        }

        // UI-565: the draw's window in the middle of the safe area; its things turn up within the draw's beat.
        private IPresentationBeat BeginDraw(PresentationCue cue, double now)
        {
            var tiles = cue.Tiles.Where(HuntTiles.ContainsKey).ToList();
            var pool = tiles.Count > 0 ? tiles : Companions(cue);
            if (pool.Count == 0)
                return null;
            Close();
            action = cue.ActionId;
            Window = BuildDraw(cue, pool, tiles.Count > 0);
            set = trace.Begin();
            set.Window(null, Window, safe());
            Appearance.Arrive(Window);
            var window = Window;
            var steps = new List<(Action Start, double Seconds)>();
            if (tiles.Count > 0)
                steps.AddRange(tiles.Select((_, index) => ((Action)(() => Reveal(window, cue, index)), DrawSeconds / tiles.Count)));
            else
                steps.Add((() => Pick(window, cue, pool), DrawSeconds));
            steps.Add((() => Settle(window, cue, tiles), PresentationPace.ResultSeconds));
            return Opened(now, cue, tiles.Count > 0 ? "Draw tiles" : "Draw", false, () => window.Q(DrawnName), steps);
        }

        // The Companions Lure of the Ring picked from, as the Fellowship panel lists them, and the one it picked.
        private List<string> Companions(PresentationCue cue)
        {
            if (cue.Drawn.Count == 0)
                return new List<string>();
            var view = board.View;
            var ids = view == null ? new List<string>() : (view.You == Side.FreePeoples
                ? view.OwnCompanions.Select(c => c.Id) : view.Fellowship.CompanionIdentities).ToList();
            if (!ids.Contains(cue.Drawn[0]))
                ids.Add(cue.Drawn[0]);
            return ids;
        }

        private VisualElement BuildDraw(PresentationCue cue, IReadOnlyList<string> pool, bool tiles)
        {
            var window = new VisualElement { name = WindowName, pickingMode = PickingMode.Ignore };
            window.AddToClassList(WindowName);
            window.Add(Text(TitleName, cue.Source.Length > 0 ? CardLibrary.Title(cue.Source) : "Card draw"));
            var rule = Text(RuleName, DrawRule(cue.RollKind));
            rule.AddToClassList(RuleName + "-wrapped");
            window.Add(rule);
            var row = new VisualElement { name = DrawnName, pickingMode = PickingMode.Ignore };
            row.AddToClassList(DiceName);
            row.AddToClassList(DrawnName);
            foreach (var id in pool)
                row.Add(tiles ? Tile(id) : Portrait(id));
            window.Add(row);
            var result = Text(ResultName, tiles ? TilesResult(Eyes(cue.Tiles), CharacterName(cue.Note)) : PickResult(cue.Drawn[0]));
            result.AddToClassList(PresentationPace.ResultClass);
            result.AddToClassList(WindowName + "-waiting");
            window.Add(result);
            return window;
        }

        private VisualElement Tile(string id)
        {
            var tile = HuntTiles[id];
            var face = new HuntTileFace("show");
            face.Apply(new HuntTileFaceView { Pool = tile.Pool, Value = tile.Value, Reveal = tile.Reveal, Stop = tile.Stop });
            face.Root.name = ItemClass + "-" + id;
            face.Root.AddToClassList(ItemClass);
            face.Root.AddToClassList(WindowName + "-waiting");
            return face.Root;
        }

        private static VisualElement Portrait(string id)
        {
            var portrait = new VisualElement { name = ItemClass + "-" + id, pickingMode = PickingMode.Ignore };
            portrait.AddToClassList(ItemClass);
            portrait.AddToClassList("presentation-card-dice-portrait");
            portrait.Add(FigureArtwork.Image(FigureAppearances.Character(id, Side.FreePeoples)));
            var name = new Label(CharacterLibrary.ShortName(id)) { pickingMode = PickingMode.Ignore, enableRichText = false };
            name.AddToClassList("presentation-card-dice-portrait-name");
            portrait.Add(name);
            return portrait;
        }

        // One tile turns up, with the card's sound.
        private void Reveal(VisualElement window, PresentationCue cue, int index)
        {
            var items = window.Query(className: ItemClass).ToList();
            if (index >= items.Count)
                return;
            items[index].RemoveFromClassList(WindowName + "-waiting");
            sounds.Play(SoundCue.CardMove, "presented/" + cue.Id + "/" + index);
        }

        // The light runs over the Companions and stops on the one picked when the draw's beat ends.
        private void Pick(VisualElement window, PresentationCue cue, IReadOnlyList<string> pool)
        {
            var items = window.Query(className: ItemClass).ToList();
            var chosen = Math.Max(0, pool.ToList().IndexOf(cue.Drawn[0]));
            var count = (int)Math.Floor(DrawSeconds / PickStepSeconds);
            var start = Time.realtimeSinceStartupAsDouble;
            IVisualElementScheduledItem tick = null;
            tick = window.schedule.Execute(() =>
            {
                var step = Math.Min(count - 1, (int)((Time.realtimeSinceStartupAsDouble - start) / PickStepSeconds));
                var lit = ((chosen - (count - 1 - step)) % items.Count + items.Count) % items.Count;
                for (var index = 0; index < items.Count; index++)
                    items[index].EnableInClassList(LitClass, index == lit);
                if (step == count - 1)
                    tick?.Pause();
            }).Every(30);
        }

        // The result; the Eyes that leave the game dim, the Companion picked stays lit and the others dim.
        private void Settle(VisualElement window, PresentationCue cue, IReadOnlyList<string> tiles)
        {
            var items = window.Query(className: ItemClass).ToList();
            var allEyes = tiles.Count > 0 && tiles.All(Eye);
            for (var index = 0; index < items.Count; index++)
            {
                items[index].RemoveFromClassList(WindowName + "-waiting");
                if (tiles.Count > 0)
                {
                    items[index].EnableInClassList(OutClass, !allEyes && Eye(tiles[index]));
                }
                else
                {
                    var picked = items[index].name == ItemClass + "-" + cue.Drawn[0];
                    items[index].EnableInClassList(LitClass, picked);
                    items[index].EnableInClassList(OutClass, !picked);
                }
            }
            if (tiles.Count == 0)
                sounds.Play(SoundCue.CardMove, "presented/" + cue.Id + "/picked");
            Show(window, ResultName);
        }

        private static readonly IReadOnlyDictionary<string, HuntTileDefinition> NoTiles = new Dictionary<string, HuntTileDefinition>();

        private IReadOnlyDictionary<string, HuntTileDefinition> HuntTiles => board.Presentation?.HuntTiles ?? NoTiles;

        private bool Eye(string tile) => HuntTiles.TryGetValue(tile, out var face) && face.Value == "eye";

        private int Eyes(IEnumerable<string> tiles) => tiles.Count(Eye);

        private static string CharacterName(string id) => id.Length > 0 ? CharacterLibrary.Name(id) : "";

        // What the draw decides, in the card's own terms.
        public static string DrawRule(string kind) => kind == PresentationCueExtraction.ChallengeDraw
            ? "Three Hunt tiles · three Eyes eliminate Strider or Aragorn, else the Eyes leave the game"
            : "A Companion of the Fellowship, at random";

        public static string TilesResult(int eyes, string king) => eyes switch
        {
            0 => "No Eye · the tiles return to the pool",
            1 => "1 Eye leaves the game",
            3 => "Three Eyes · " + (king.Length > 0 ? king : "Strider or Aragorn") + " is eliminated",
            _ => eyes + " Eyes leave the game"
        };

        public static string PickResult(string companion) => CharacterLibrary.Name(companion) + " is picked";

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

        // The result, with the card's note when it has one (UI-568).
        private static void ShowResult(VisualElement window)
        {
            Show(window, ResultName);
            Show(window, NoteName);
        }

        private static VisualElement Build(PresentationCue cue, string note)
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
            // UI-568: what the card then leaves out, read with the result.
            if (note.Length > 0)
            {
                var line = Text(NoteName, note);
                line.AddToClassList(WindowName + "-waiting");
                window.Add(line);
            }
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
