using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306: the presented revision as one presentation stages it — a copy of the board's view in which only recorded
    // facts change (movers relocated, a region's control). It drives the map's markers and settlements
    // (BoardScreen.Stage), and becomes the map's figure baseline, so the release moves only what the presentation did not
    // show. The board's view, decision and windows stay as they were. It is copied once per presentation and discarded
    // when the presentation ends.
    public sealed class StagedBoard
    {
        private readonly BoardScreen board;
        private PlayerView staged;
        public bool Changed { get; private set; }
        public PlayerView View => staged ??= JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(board.View));

        public StagedBoard(BoardScreen board)
        {
            this.board = board;
        }

        // The recorded participants standing on the staged board, as the map draws them now.
        public IEnumerable<ActionParticipant> Present(IEnumerable<ActionParticipant> participants) =>
            participants.Where(p => View.Units.Any(u => u.Id == p.Id) || View.Characters.Any(c => c.Id == p.Id));

        // UI-309: Companions leaving the Fellowship stand at their destination, and the staged Fellowship no longer holds
        // them, so the figure difference shows their separation exactly as the board would (FellowshipSeparation).
        public void Separate(IEnumerable<ActionParticipant> companions, string region)
        {
            foreach (var companion in companions)
            {
                View.Fellowship.CompanionIdentities.Remove(CharacterLibrary.Identity(companion.DefinitionId));
                View.Fellowship.CompanionCount = System.Math.Max(0, View.Fellowship.CompanionCount - 1);
                Add(companion, region);
            }
            Changed = true;
        }

        public void Move(IEnumerable<ActionParticipant> movers, string region)
        {
            foreach (var mover in movers)
            {
                var unit = View.Units.FirstOrDefault(u => u.Id == mover.Id);
                if (unit != null)
                {
                    unit.Location = region;
                    unit.Garrison = mover.Garrison;
                }
                var character = View.Characters.FirstOrDefault(c => c.Id == mover.Id);
                if (character != null)
                {
                    character.Location = region;
                    character.Garrison = mover.Garrison;
                }
            }
            Changed = true;
        }

        public void Control(string region, Side? controller)
        {
            View.Control[region] = controller;
            Changed = true;
        }

        // UI-307: one recorded figure change — a recruit or a character entering, an upgrade's halves, a removal to the
        // reserve or the casualties, a transformation, a Companion leaving the Fellowship. A known figure takes its
        // recorded place (and kind or version); an unknown one arriving on the board is added from its record.
        public void Apply(ActionFigureChange change)
        {
            var figure = change.After ?? change.Before;
            if (figure == null)
                return;
            var unit = View.Units.FirstOrDefault(u => u.Id == figure.Id);
            var character = View.Characters.FirstOrDefault(c => c.Id == figure.Id);
            // UI-344: an awakening sovereign's Leader gives way to his character, with the same id.
            if (unit != null && FigureCues.Awakened(change))
            {
                View.Units.Remove(unit);
                if (character == null)
                    Add(change.After, change.To);
                else
                    Place(character, change);
            }
            else if (unit != null)
                Place(unit, change);
            else if (character != null)
                Place(character, change);
            else if (change.After != null)
                Add(change.After, change.To);
            Changed = true;
        }

        // UI-310: the Fellowship as a recorded outcome left it — position, tracks, hiding and Guide.
        public void Fellowship(HuntOutcomeView outcome)
        {
            var fellowship = View.Fellowship;
            fellowship.RegionId = outcome.RegionId;
            fellowship.Progress = outcome.Progress;
            fellowship.MordorStep = outcome.MordorStep;
            fellowship.Corruption = outcome.Corruption;
            fellowship.Hidden = outcome.Hidden;
            fellowship.Guide = outcome.GuideId;
            Changed = true;
        }

        // UI-310: a new Guide recorded by the Hunt; the tracks wait for the Fellowship's own change.
        public void Guide(string id) => View.Fellowship.Guide = id;

        // UI-310: a Companion leaving the Fellowship through the Hunt (a casualty, a Hobbit separating as its guide). UI-344:
        // or out of play (Sméagol), named by the version his record gives.
        public void Leave(string characterId, string definitionId = "")
        {
            var own = View.OwnCompanions.FirstOrDefault(c => c.Id == characterId);
            var version = own?.DefinitionId ?? (definitionId.Length > 0 ? definitionId : characterId);
            View.Fellowship.CompanionIdentities.Remove(CharacterLibrary.Identity(version));
            View.Fellowship.CompanionCount = System.Math.Max(0, View.Fellowship.CompanionCount - 1);
            View.OwnCompanions.RemoveAll(c => c.Id == characterId);
        }

        // UI-310: the Hunt window as a recorded entry left it: the entry's snapshot (the last one when it has none) and the
        // report up to that entry; a completed chain keeps only its report.
        public void Hunt(string chain, HuntReportEntryView entry)
        {
            var report = View.HuntReport;
            if (report == null || report.Id != chain)
                View.HuntReport = report = new HuntReportView { Id = chain, StartedRevision = entry.Revision };
            if (!report.Entries.Any(known => SameEntry(known, entry)))
                report.Entries.Add(entry);
            report.Completed = entry.Kind == HuntReportKind.HuntCompleted;
            View.Hunt = report.Completed ? null : entry.Hunt ?? View.Hunt;
        }

        private static bool SameEntry(HuntReportEntryView a, HuntReportEntryView b) => a.Kind == b.Kind && a.Revision == b.Revision &&
            a.HuntId == b.HuntId && a.CharacterId == b.CharacterId && a.Response == b.Response;

        // UI-312: a recorded change of one die — its face (once rolled or changed by a Ring) and its place.
        public void Die(ActionDieChange change)
        {
            var die = View.Dice.FirstOrDefault(d => d.Id == change.Id);
            if (die == null)
                return;
            if (change.After.HasValue)
                die.Face = change.After;
            die.Location = change.To;
        }

        // UI-312: a side's hand after drawing (+) or discarding (−) cards; UI-361: its split by deck follows when the
        // card's deck is known, so the counter's tooltip agrees with its number.
        public void Hand(Side side, int change, string deck = "")
        {
            var player = View.Players.FirstOrDefault(p => p.Side == side);
            if (player == null)
                return;
            player.HandCount = System.Math.Max(0, player.HandCount + change);
            if (deck.Length == 0 || player.HandDeckCounts.Count == 0)
                return;
            player.HandDeckCounts.TryGetValue(deck, out var cards);
            player.HandDeckCounts[deck] = System.Math.Max(0, cards + change);
        }

        // UI-307: a recorded political change of one nation.
        public void Nation(string id, bool active, int stepsToWar)
        {
            var nation = View.Nations.FirstOrDefault(n => n.Id == id);
            if (nation == null)
                return;
            nation.Active = active;
            nation.StepsToWar = stepsToWar;
        }

        // UI-344: a sovereign as a recorded step left him; a Corrupted or Removed king is no figure any more.
        public void Sovereign(SovereignView sovereign)
        {
            var index = View.Sovereigns.FindIndex(s => s.Id == sovereign.Id);
            if (index < 0)
                return;
            View.Sovereigns[index] = sovereign;
            if (sovereign.Status is SovereignStatus.Corrupted or SovereignStatus.Removed)
                View.Units.RemoveAll(u => u.Id == sovereign.Id);
            Changed = true;
        }

        // UI-344: a Ruler, Dismay or Council token, or an Elven Ring, as a recorded change left it.
        public void Token(ActionResourceChange change)
        {
            switch (change.Kind)
            {
                case ActionCostKind.RulerToken:
                    var token = View.RulerTokens.FirstOrDefault(t => t.Id == change.Id);
                    if (token != null)
                        token.Location = change.To;
                    break;
                case ActionCostKind.CouncilToken:
                    var council = View.CouncilTokens.FirstOrDefault(t => t.Id == change.Id);
                    if (council == null && change.To == "Shadow")
                        View.CouncilTokens.Add(new CouncilTokenView { Id = change.Id });
                    else if (council != null)
                        council.Spent = change.To == "spent";
                    break;
                case ActionCostKind.Ring:
                    var ring = View.Rings.FirstOrDefault(r => r.Id == change.Id);
                    if (ring != null)
                        ring.Owner = System.Enum.TryParse<Side>(change.To, out var holder) ? holder : (Side?)null;
                    break;
            }
        }

        // Draw the staged nations on the Politics panel.
        public void ShowNations() => board.Nations.Apply(View);

        private static void Place(UnitView unit, ActionFigureChange change)
        {
            unit.Location = change.To;
            if (change.After == null)
                return;
            unit.Garrison = change.After.Garrison;
            if (change.After.UnitKind.HasValue)
                unit.Kind = change.After.UnitKind.Value;
        }

        private static void Place(CharacterView character, ActionFigureChange change)
        {
            character.Location = change.To;
            if (change.After == null)
                return;
            character.Garrison = change.After.Garrison;
            if (change.After.DefinitionId.Length > 0)
                character.DefinitionId = change.After.DefinitionId;
            if (change.After.Level.HasValue)
                character.Level = change.After.Level.Value;
        }

        private void Add(ActionParticipant figure, string region)
        {
            if (figure.UnitKind.HasValue)
                View.Units.Add(new UnitView
                {
                    Id = figure.Id, Nation = figure.NationId, Side = figure.Side, Kind = figure.UnitKind.Value, Location = region,
                    Garrison = figure.Garrison
                });
            else
                View.Characters.Add(new CharacterView
                {
                    Id = figure.Id, DefinitionId = figure.DefinitionId, Side = figure.Side, Location = region, Garrison = figure.Garrison,
                    Level = figure.Level ?? 0, Leadership = figure.Leadership ?? 0
                });
        }

        // Draw the staged markers and settlements on the presented board.
        public void Show() => board.Stage(View);

        // The staged positions are the map's figure baseline from here on.
        public void Settle()
        {
            if (Changed)
                board.Map.Figures.Rebase(View);
        }

        public void Reset()
        {
            staged = null;
            Changed = false;
        }
    }
}
