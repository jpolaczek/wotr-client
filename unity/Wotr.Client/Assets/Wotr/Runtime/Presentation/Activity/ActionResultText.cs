using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-166: what one recorded action achieved, in a heading and at most three lines — the result a player needs,
    // not each step. The full record stays in the Log.
    public sealed class ActionResult
    {
        public string Header { get; }
        public IReadOnlyList<string> Lines { get; }
        public string CardId { get; }
        public DieFace? Die { get; }
        public string CardTitle => CardId.Length > 0 ? CardLibrary.Title(CardId) : "";
        public string Tooltip => Lines.Count == 0 ? Header : Header + "\n" + Lines[0];

        public ActionResult(string header, IReadOnlyList<string> lines, string cardId, DieFace? die)
        {
            Header = header;
            Lines = lines;
            CardId = cardId;
            Die = die;
        }
    }

    // Lines come only from the recorded facts of the public action log (R11): the opponent's hand, committed Combat
    // cards and private choices never reach them. Order of importance (SIGNALS_UX A): the end of the game, captured
    // settlements, battles, movement, recruitment and losses, politics, the Fellowship and the Hunt, characters and
    // dice added to the Hunt Box.
    public static class ActionResultText
    {
        public const int MaxLines = 3;
        private static readonly CommandKind[] BattleCommands =
        {
            CommandKind.CommitCombatCard, CommandKind.SetCombatCardPolicy, CommandKind.RerollBattle, CommandKind.TakeBattleLosses,
            CommandKind.ContinueBattle, CommandKind.StopBattle, CommandKind.RetreatBattle, CommandKind.HoldBattle,
            CommandKind.AdvanceBattle, CommandKind.FightFieldBattle, CommandKind.RetreatIntoSiege, CommandKind.ExtendSiege
        };

        public static ActionResult For(ActionLogView view, BoardPresentation data) =>
            For(view.Action.Command, view.Action.Initiator, view.Steps, data);

        public static ActionResult For(IEnumerable<ActionLogStep> steps, BoardPresentation data) => For(null, null, steps, data);

        private static ActionResult For(CommandKind? command, Side? initiator, IEnumerable<ActionLogStep> source, BoardPresentation data)
        {
            var steps = source.OrderBy(step => step.Revision).ThenBy(step => step.Order).ToList();
            command ??= steps.FirstOrDefault(step => step.Command.HasValue)?.Command;
            initiator ??= steps.FirstOrDefault(step => step.Actor.HasValue)?.Actor;
            var lines = Results(steps, data).Distinct().ToList();
            if (lines.Count == 0 && steps.Count > 0)
                lines.Add(Recorded(steps.Last(), initiator));
            var card = PlayedCard(steps);
            return new ActionResult(Header(command, steps, card, data), Fit(lines), card, Die(steps));
        }

        // More results than lines: the last line counts the rest.
        private static IReadOnlyList<string> Fit(List<string> lines)
        {
            if (lines.Count <= MaxLines)
                return lines;
            var shown = lines.Take(MaxLines - 1).ToList();
            shown.Add("+" + (lines.Count - shown.Count) + " more");
            return shown;
        }

        private static IEnumerable<string> Results(List<ActionLogStep> steps, BoardPresentation data) =>
            Outcome(steps)
                .Concat(Captures(steps, data))
                .Concat(Battles(steps, data))
                .Concat(Moves(steps, data))
                .Concat(Recruits(steps, data))
                .Concat(Losses(steps, data))
                .Concat(Politics(steps))
                .Concat(Fellowship(steps, data))
                .Concat(Characters(steps, data))
                .Concat(HuntBox(steps));

        private static IEnumerable<string> Outcome(List<ActionLogStep> steps)
        {
            var outcome = steps.Select(step => step.Facts.Outcome).LastOrDefault(fact => fact?.Winner != null);
            if (outcome != null)
                yield return ActionCardText.Actor(outcome.Winner) + " wins · " + VictoryPresentation.Reason(outcome.Victory);
        }

        private static IEnumerable<string> Captures(List<ActionLogStep> steps, BoardPresentation data)
        {
            var changes = steps.SelectMany(step => step.Facts.Control).GroupBy(change => change.RegionId).Select(group => group.Last());
            foreach (var change in changes)
                yield return Capture(change, data);
        }

        private static string Capture(ActionControlChange change, BoardPresentation data)
        {
            var name = ActionCardText.Region(change.RegionId, data);
            if (!change.After.HasValue)
                return name + " · No longer controlled";
            data.Regions.TryGetValue(change.RegionId, out var region);
            if (region?.OriginalOwner == change.After)
                return name + " recaptured";
            var points = region?.VictoryPoints ?? 0;
            return name + " captured" + (points > 0 ? " · +" + points + " VP" : "");
        }

        private static IEnumerable<string> Battles(List<ActionLogStep> steps, BoardPresentation data)
        {
            foreach (var battle in steps.Where(InBattle).GroupBy(BattleKey))
                yield return "Battle at " + BattleRegion(battle, data) + BattleResult(battle.ToList());
        }

        private static bool InBattle(ActionLogStep step) => step.Context.BattleId.Length > 0 || step.Facts.Battle != null;

        private static string BattleKey(ActionLogStep step) => step.Context.BattleId.Length > 0 ? step.Context.BattleId : step.Facts.Battle.Id;

        private static string BattleRegion(IEnumerable<ActionLogStep> battle, BoardPresentation data)
        {
            var region = battle.Select(step => step.Facts.BattlePlace?.RegionId ?? step.Facts.Battle?.RegionId ?? "")
                .FirstOrDefault(id => id.Length > 0) ?? battle.SelectMany(step => step.Context.Regions).LastOrDefault() ?? "";
            return ActionCardText.Region(region, data);
        }

        private static string BattleResult(List<ActionLogStep> battle)
        {
            var retreat = battle.SelectMany(step => step.Movements).FirstOrDefault(move => move.Kind == ActionMovementKind.Retreat);
            if (retreat?.Participants.Count > 0)
                return " · " + ActionCardText.Actor(retreat.Participants[0].Side) + " retreats";
            if (battle.Any(step => step.Command == CommandKind.StopBattle))
                return " · Attack stopped";
            if (battle.Any(step => step.Command == CommandKind.RetreatIntoSiege))
                return " · Defender withdraws into the stronghold";
            if (battle.SelectMany(step => step.Movements).Any(move => move.Kind == ActionMovementKind.Advance))
                return " · Attacker advances";
            var round = battle.Select(step => step.Facts.Battle?.Round ?? 0).DefaultIfEmpty(0).Max();
            return round > 0 ? " · Round " + round : "";
        }

        // Marches and travel; an attack, a retreat and an advance are told by the battle line.
        private static IEnumerable<string> Moves(List<ActionLogStep> steps, BoardPresentation data)
        {
            var moves = steps.SelectMany(step => step.Movements)
                .Where(move => move.Kind is ActionMovementKind.March or ActionMovementKind.Travel);
            foreach (var move in moves)
            {
                var figures = Figures(move.Participants);
                var route = Route(move.From, move.To, data);
                yield return figures.Length > 0 ? figures + " · " + route : route;
            }
        }

        public static string Figures(IEnumerable<ActionParticipant> participants)
        {
            var list = participants.ToList();
            var units = list.Where(p => p.UnitKind.HasValue).GroupBy(p => p.UnitKind.Value).Select(g => g.Count() + " " + g.Key);
            var characters = list.Where(p => !p.UnitKind.HasValue && p.DefinitionId.Length > 0)
                .Select(p => CharacterLibrary.Name(p.DefinitionId)).Distinct();
            return string.Join(" + ", units.Concat(characters));
        }

        private static string Route(string from, string to, BoardPresentation data)
        {
            if (from.Length == 0)
                return ActionCardText.Region(to, data);
            return ActionCardText.Region(from, data) + " → " + ActionCardText.Region(to, data);
        }

        // New units on the board, by region: a unit that came from the reserve (ActionBoardChanges records its places).
        private static IEnumerable<string> Recruits(List<ActionLogStep> steps, BoardPresentation data)
        {
            var placed = steps.SelectMany(step => step.Facts.Figures)
                .Where(change => change.After?.UnitKind != null && OnBoard(change.To, data) && !OnBoard(change.From, data));
            foreach (var region in placed.GroupBy(change => change.To))
                yield return "+" + Figures(region.Select(change => change.After)) + " · " + ActionCardText.Region(region.Key, data);
        }

        // Figures that left the board outside a battle (events, the Hunt); a battle's losses belong to its line.
        private static IEnumerable<string> Losses(List<ActionLogStep> steps, BoardPresentation data)
        {
            var removed = steps.Where(step => !InBattle(step)).SelectMany(step => step.Facts.Figures)
                .Where(change => change.Before != null && OnBoard(change.From, data) && !OnBoard(change.To, data) && change.To != "fellowship");
            foreach (var region in removed.GroupBy(change => change.From))
            {
                var figures = Figures(region.Select(change => change.Before));
                if (figures.Length > 0)
                    yield return "−" + figures + " · " + ActionCardText.Region(region.Key, data);
            }
        }

        private static bool OnBoard(string location, BoardPresentation data) => data.Regions.ContainsKey(location);

        private static IEnumerable<string> Politics(List<ActionLogStep> steps)
        {
            foreach (var nation in steps.SelectMany(step => step.Facts.Nations).GroupBy(change => change.Id).Select(group => group.Last()))
                yield return FigureGrid.Name(nation.Id) + " · " + Politics(nation);
        }

        private static string Politics(ActionNationChange nation)
        {
            if (nation.StepsToWar == 0)
                return "At war";
            if (nation.Active && nation.WasActive == false)
                return "Active · " + StepsToWar(nation.StepsToWar);
            return StepsToWar(nation.StepsToWar);
        }

        private static string StepsToWar(int steps) => steps + (steps == 1 ? " step" : " steps") + " to war";

        private static IEnumerable<string> Fellowship(List<ActionLogStep> steps, BoardPresentation data)
        {
            var before = steps.Select(step => step.Facts.FellowshipBefore).FirstOrDefault(value => value != null);
            var after = steps.Select(step => step.Facts.FellowshipAfter).LastOrDefault(value => value != null);
            if (after == null)
                yield break;
            var parts = FellowshipChanges(before, after, data).ToList();
            if (parts.Count > 0)
                yield return string.Join(" · ", parts);
        }

        private static IEnumerable<string> FellowshipChanges(HuntOutcomeView before, HuntOutcomeView after, BoardPresentation data)
        {
            if (before == null)
            {
                yield return after.Hidden ? "Fellowship hidden" : "Fellowship revealed";
                yield return "Corruption " + after.Corruption;
                yield break;
            }
            if (before.Corruption != after.Corruption)
                yield return "Corruption " + before.Corruption + " → " + after.Corruption;
            if (before.Hidden && !after.Hidden)
                yield return "Revealed" + (after.RegionId.Length > 0 ? " in " + ActionCardText.Region(after.RegionId, data) : "");
            else if (!before.Hidden && after.Hidden)
                yield return "Hidden again";
            if (before.MordorStep != after.MordorStep && after.MordorStep.HasValue)
                yield return "Mordor step " + after.MordorStep;
            else if (before.Progress != after.Progress && after.Hidden)
                yield return "Progress " + before.Progress + " → " + after.Progress;
        }

        // A character entering play, or a new version of one already in play (Gandalf the White).
        private static IEnumerable<string> Characters(List<ActionLogStep> steps, BoardPresentation data)
        {
            foreach (var change in steps.SelectMany(step => step.Facts.Figures))
            {
                var after = change.After?.DefinitionId ?? "";
                if (after.Length == 0 || change.After.UnitKind.HasValue)
                    continue;
                // A Companion leaving the Fellowship is not a character entering play.
                var entered = OnBoard(change.To, data) && !OnBoard(change.From, data) && change.From != "fellowship";
                var changed = change.Before != null && change.Before.DefinitionId.Length > 0 && change.Before.DefinitionId != after;
                if (entered || changed)
                    yield return CharacterLibrary.Name(after) + " enters" + (change.To.Length > 0 ? " · " + ActionCardText.Region(change.To, data) : "");
            }
        }

        private static IEnumerable<string> HuntBox(List<ActionLogStep> steps)
        {
            var dice = steps.SelectMany(step => step.Facts.Dice).Count(die => die.To == DieLocation.Hunt && die.From != DieLocation.Hunt);
            if (dice > 0)
                yield return "Hunt Box +" + dice;
        }

        // Only a card revealed to both players: a played Event or a revealed Combat card.
        private static string PlayedCard(List<ActionLogStep> steps) => steps.SelectMany(step => step.Facts.Cards)
            .FirstOrDefault(card => card.Use is ActionCardUse.Event or ActionCardUse.CombatReveal && CardLibrary.Cards.ContainsKey(card.Id))?.Id ?? "";

        private static DieFace? Die(List<ActionLogStep> steps)
        {
            var cost = steps.SelectMany(step => step.Costs).FirstOrDefault(item => item.Kind == ActionCostKind.Die);
            return cost?.Use ?? cost?.Face;
        }

        // The action's own record when no fact describes a result, without the side the card already shows.
        private static string Recorded(ActionLogStep step, Side? actor) => ActionCardText.WithoutSide(step.Text, actor ?? step.Actor);

        private static string Header(CommandKind? command, List<ActionLogStep> steps, string card, BoardPresentation data)
        {
            var label = Label(command);
            var subject = Subject(command, steps, card, data);
            return subject.Length > 0 ? label + " · " + subject : label;
        }

        private static string Label(CommandKind? command)
        {
            if (!command.HasValue)
                return "Action";
            if (BattleCommands.Contains(command.Value))
                return "Battle";
            return command.Value switch
            {
                CommandKind.MoveArmies => "Army move",
                CommandKind.AttackArmy => "Attack",
                CommandKind.Recruit => "Muster",
                CommandKind.PlayEvent => "Event",
                CommandKind.MoveFellowship => "Fellowship moved",
                CommandKind.DeclareFellowship => "Fellowship declared",
                CommandKind.RevealFellowship => "Fellowship revealed",
                CommandKind.HideFellowship => "Fellowship hidden",
                CommandKind.AdvancePolitics => "Politics",
                CommandKind.EnterCharacter => "Character",
                CommandKind.UseCharacterAbility => "Ability",
                CommandKind.MoveCompanions => "Companions moved",
                CommandKind.SeparateCompanions => "Companions separated",
                CommandKind.MoveShadowCharacters => "Minions moved",
                CommandKind.DrawCard => "Card drawn",
                CommandKind.Pass => "Pass",
                CommandKind.DiscardDie => "Die discarded",
                CommandKind.AllocateHunt => "Hunt dice",
                CommandKind.ContinueFellowship => "Fellowship phase",
                CommandKind.DiscardCards => "Cards discarded",
                CommandKind.UseRing => "Elven Ring",
                _ => ActionCommandText.Describe(command.Value).TrimEnd('.')
            };
        }

        private static string Subject(CommandKind? command, List<ActionLogStep> steps, string card, BoardPresentation data)
        {
            if (command == CommandKind.PlayEvent)
                return card.Length > 0 ? CardLibrary.Title(card) : "";
            if (command == CommandKind.MoveFellowship)
                return steps.Any(step => step.Facts.Hunt != null || step.Context.HuntChainId.Length > 0) ? "Hunt" : "";
            if (command == CommandKind.AdvancePolitics)
                return Nation(steps);
            if (command == CommandKind.UseCharacterAbility)
                return steps.Select(step => step.Facts.SourceId).Where(id => id.Length > 0).Select(ActionCardText.Source).FirstOrDefault() ?? "";
            if (command.HasValue && (BattleCommands.Contains(command.Value) || command == CommandKind.AttackArmy))
            {
                var battle = steps.Where(InBattle).ToList();
                if (battle.Count > 0)
                    return BattleRegion(battle, data);
            }
            return Place(steps, data);
        }

        private static string Nation(List<ActionLogStep> steps)
        {
            var id = steps.SelectMany(step => step.Facts.Nations).Select(nation => nation.Id).FirstOrDefault()
                ?? steps.Select(step => step.Context.NationId).FirstOrDefault(value => value.Length > 0) ?? "";
            return id.Length > 0 ? FigureGrid.Name(id) : "";
        }

        // Where it happened: the last destination of a move or new figure, else the last region the action names.
        private static string Place(List<ActionLogStep> steps, BoardPresentation data)
        {
            var destination = steps.SelectMany(step => step.Movements).Select(move => move.To).LastOrDefault(id => id.Length > 0)
                ?? steps.SelectMany(step => step.Facts.Figures).Select(change => change.To).LastOrDefault(id => data.Regions.ContainsKey(id))
                ?? steps.SelectMany(step => step.Context.Regions).LastOrDefault(data.Regions.ContainsKey);
            return destination == null ? "" : ActionCardText.Region(destination, data);
        }
    }
}
