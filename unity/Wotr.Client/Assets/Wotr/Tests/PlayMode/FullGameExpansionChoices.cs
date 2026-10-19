using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class FullGameExpansionChoices
    {
        public static CommandKind? Select(PlayerView view) => view.Decision.Kind switch
        {
            DecisionKind.CouncilVersions => CommandKind.ChooseCompanionVersions,
            DecisionKind.CouncilHomelands => CommandKind.ChooseCouncilHomelands,
            DecisionKind.SpecialDieChoice => CommandKind.ChooseSpecialDie,
            DecisionKind.RulerDiscard => CommandKind.DiscardRulerDie,
            DecisionKind.Dismay => CommandKind.DeclineDismay,
            DecisionKind.KeepActionDie => CommandKind.DeclineVilya,
            _ => null
        };

        public static async Task<bool> Execute(MatchTestRoom room, MountedMatchClient client, CommandKind kind,
            Action<MountedMatchClient> inspect)
        {
            if (kind is CommandKind.DeclineDismay or CommandKind.DeclineVilya)
            {
                inspect?.Invoke(client);
                await room.Direct(client, "action-" + kind);
                return true;
            }
            var option = client.Board.View.Options.Single(o => o.Kind == kind && o.Enabled);
            switch (kind)
            {
                case CommandKind.ChooseCompanionVersions:
                    await ChooseVersions(room, client, option);
                    break;
                case CommandKind.ChooseCouncilHomelands:
                    client.Board.CompanionSetup.Open();
                    await room.Settle();
                    Assert.IsTrue(client.Board.Tray.Query<Label>("homeland-location").ToList()
                        .All(label => label.text == "Fellowship"), "This script explicitly confirms an empty homeland set.");
                    break;
                case CommandKind.ChooseSpecialDie:
                case CommandKind.DiscardRulerDie:
                    var revision = client.Board.View.Revision;
                    await room.Settle();
                    // UI-267: a special-die choice with one legal die is sent by the client itself.
                    if (client.Board.View.Revision > revision)
                        return true;
                    inspect?.Invoke(client);
                    // UI-264: the die is chosen in its pool.
                    CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(option.EligibleDice[0]));
                    await room.At(revision + 1);
                    return true;
                default:
                    return false;
            }
            await room.Settle();
            inspect?.Invoke(client);
            await room.Confirm(client);
            return true;
        }

        private static async Task ChooseVersions(MatchTestRoom room, MountedMatchClient client, ActionOption option)
        {
            // UI-269: on the Fellowship panel's portraits.
            client.Board.Fellowship.Open();
            await room.Settle();
            foreach (var group in option.CompanionVersions)
            {
                PointerGestureEvents.ClickAt(client.Board.Fellowship.Root.Q<Button>("fellowship-portrait-" + group.FigureId));
                await room.Settle();
                var id = group.FigureId == "base.character.gandalf-grey" ? group.AlternateId : group.BaseId;
                PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>("version-" + id));
                await room.Settle();
            }
            PointerGestureEvents.ClickAt(client.Board.Fellowship.Root.Q<Button>("starting-guide-base.character.gandalf-grey"));
        }
    }
}
