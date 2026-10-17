using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // A fixed acceptance script using only the acting client's legal options.
    // It is deliberately separate from the product and does not inspect server state.
    internal static class FullGameChoices
    {
        private static readonly string[] Corridor =
        {
            "rivendell", "fords-of-bruinen", "high-pass", "goblins-gate", "old-ford", "carrock",
            "old-forest-road", "northern-rhovanion", "southern-rhovanion", "noman-lands", "dagorlad", "morannon"
        };

        public static CommandKind Select(PlayerView view)
        {
            var expansion = FullGameExpansionChoices.Select(view);
            if (expansion.HasValue)
                return expansion.Value;
            bool Has(CommandKind kind) => view.Options.Any(o => o.Kind == kind && o.Enabled);
            switch (view.Decision.Kind)
            {
                case DecisionKind.Action:
                    if (Has(CommandKind.HideFellowship)) return CommandKind.HideFellowship;
                    if (Has(CommandKind.MoveFellowship)) return CommandKind.MoveFellowship;
                    if (view.Options.Any(o => o.Kind == CommandKind.DiscardDie && o.Enabled && o.EligibleDice.Count > 0))
                        return CommandKind.DiscardDie;
                    if (Has(CommandKind.Pass)) return CommandKind.Pass;
                    throw new AssertionException("No supported legal action in the full-game script.");
                case DecisionKind.FellowshipPhase:
                    if (Has(CommandKind.EnterMordor)) return CommandKind.EnterMordor;
                    if (view.Fellowship.Progress > 0 && Has(CommandKind.DeclareFellowship))
                        return CommandKind.DeclareFellowship;
                    return CommandKind.ContinueFellowship;
                case DecisionKind.AllocateHunt: return CommandKind.AllocateHunt;
                case DecisionKind.DiscardCards: return CommandKind.DiscardCards;
                case DecisionKind.Reaction: return CommandKind.DeclineReaction;
                case DecisionKind.HuntReroll: return CommandKind.RerollHunt;
                case DecisionKind.HuntDamage: return CommandKind.ResolveHuntDamage;
                case DecisionKind.FellowshipRoute: return CommandKind.RevealFellowship;
                default: throw new AssertionException("Unexpected full-game decision: " + view.Decision.Kind);
            }
        }

        public static async Task Execute(MatchTestRoom room, MountedMatchClient client, CommandKind kind,
            Action<MountedMatchClient> inspect = null)
        {
            if (await FullGameExpansionChoices.Execute(room, client, kind, inspect))
                return;
            var option = client.Board.View.Options.Single(o => o.Kind == kind && o.Enabled);
            var revision = client.Board.View.Revision;
            SelectCost(client, option);
            await room.Settle();
            if (kind == CommandKind.DeclineReaction)
            {
                inspect?.Invoke(client);
                await room.Direct(client, "action-" + kind);
                return;
            }
            if (kind is CommandKind.DiscardDie or CommandKind.Pass)
            {
                inspect?.Invoke(client);
                await ComponentGestures.Select(client, kind.ToString());
                await room.At(revision + 1);
                return;
            }
            if (kind == CommandKind.EnterMordor)
            {
                inspect?.Invoke(client);
                await FellowshipGestures.EnterMordor(client);
                await room.At(revision + 1);
                return;
            }
            var action = FellowshipButton(kind);
            if (action != null)
            {
                client.Board.Fellowship.Open();
                await room.Settle();
                inspect?.Invoke(client);
                FellowshipGestures.Action(client, action);
                if (kind != CommandKind.DeclareFellowship)
                {
                    await room.At(revision + 1);
                    return;
                }
            }
            if (kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship)
            {
                var end = await Route(room, client, option.Travel.Single());
                inspect?.Invoke(client);
                FellowshipGestures.FinishRoute(client, end);
                await room.At(revision + 1);
                return;
            }
            if (kind == CommandKind.ResolveHuntDamage)
            {
                inspect?.Invoke(client);
                MatchTestRoom.Click(client, "hunt-response-Ring");
                await room.At(revision + 1);
                return;
            }
            if (kind == CommandKind.AllocateHunt)
                foreach (var id in option.EligibleIds.Take(option.Minimum))
                {
                    Assert.IsTrue(client.Board.OwnDice.Q<Button>(id).ClassListContains("hunt-candidate"));
                    PointerGestureEvents.ClickAt(client.Board.OwnDice.Q<Button>(id));
                    await room.Settle();
                    Assert.IsTrue(client.Board.OwnDice.Q<Button>(id).ClassListContains("hunt-allocated"));
                    Assert.AreEqual(revision, client.Board.View.Revision,
                        "Selecting the exact allocation does not submit it before the final confirmation.");
                }
            if (kind == CommandKind.DiscardCards)
            {
                await CardGestures.Open(client);
                inspect?.Invoke(client);
                CardGestures.Discard(client, option.EligibleIds[0]);
                await room.At(revision + 1);
                return;
            }
            await room.Settle();
            inspect?.Invoke(client);
            await room.Confirm(client);
        }

        private static string FellowshipButton(CommandKind kind) => kind switch
        {
            CommandKind.ContinueFellowship => "fellowship-continue",
            CommandKind.DeclareFellowship => "fellowship-declare",
            CommandKind.MoveFellowship => "fellowship-progress",
            CommandKind.HideFellowship => "fellowship-status",
            _ => null
        };

        private static void SelectCost(MountedMatchClient client, ActionOption option)
        {
            if (option.EligibleDice.Count == 0)
                return;
            var die = client.Board.View.Dice.Where(d => option.EligibleDice.Contains(d.Id))
                .OrderByDescending(d => d.Face is DieFace.Character or DieFace.WillOfTheWest).First();
            ArmyGestures.Die(client, die.Id, die.Face == DieFace.ArmyMuster ? DieFace.Army : DieFace.Character);
        }

        private static async Task<string> Route(MatchTestRoom room, MountedMatchClient client, TravelOption travel)
        {
            var start = Array.IndexOf(Corridor, travel.Origin);
            Assert.GreaterOrEqual(start, 0, "The acceptance corridor must contain the current origin.");
            var end = Math.Min(Corridor.Length - 1, start + travel.Range);
            while (end > start && !travel.Targets.Contains(Corridor[end]))
                end--;
            Assert.Contains(Corridor[end], travel.Targets, "The corridor must end in a legal region.");
            for (var index = start + 1; index <= end; index++)
            {
                await room.Settle();
                Assert.Contains(Corridor[index], client.Board.Map.DraftOverlay.Targets.ToList(),
                    $"Route from {travel.Origin}, step {index - start}; available map targets: {string.Join(",", client.Board.Map.DraftOverlay.Targets)}.");
                FellowshipGestures.Region(client, Corridor[index]);
                await room.Settle();
            }
            await room.Settle();
            return Corridor[end];
        }
    }
}
