using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-247: a reaction is used at its source — a battle portrait, the Guide's portrait in the Fellowship panel, a table
    // card's × in the Effects panel — or, without one, at its console or tray button.
    internal static class ReactionGestures
    {
        private const string PortraitPrefix = "fellowship-portrait-";

        public static async Task Use(MatchTestRoom room, MountedMatchClient client, string reactionId)
        {
            var view = client.Board.View;
            var fighter = view.Battle == null ? null : new[] { view.Battle.Attacker, view.Battle.Defender }
                .Where(f => f != null).SelectMany(f => f.Characters).FirstOrDefault(c => BattleReactions.Of(view, c)?.Id == reactionId);
            if (fighter != null)
            {
                var portrait = client.Board.Tray.Q("portrait-" + fighter.Id);
                Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName), "The reaction's portrait glows.");
                PointerGestureEvents.ClickAt(portrait);
                return;
            }
            if (FellowshipActions.ReactionOnRoster(view, reactionId))
            {
                client.Board.Fellowship.Open();
                await room.Settle();
                var guide = client.Board.Fellowship.Root.Query<Button>().ToList().First(b => b.name.StartsWith(PortraitPrefix) &&
                    FellowshipGuideText.IsGuide(view.Fellowship, b.name.Substring(PortraitPrefix.Length)));
                Assert.IsTrue(guide.ClassListContains(ChoiceGlow.ClassName), "The Guide's portrait glows.");
                PointerGestureEvents.ClickAt(guide);
                return;
            }
            PointerGestureEvents.ClickAt(MatchTestRoom.TrayOrConsole(client, "effect-choice-" + reactionId));
        }

        public static async Task TableCard(MatchTestRoom room, MountedMatchClient client, string card)
        {
            CardNetworkTests.Click(client.Board.Console.Status.Options.Q<Button>("console-option-table-cards"));
            var use = client.Board.TableEffects.Root.Q<Button>("remove-table-" + card);
            await room.Until(() => use.enabledInHierarchy && use.worldBound.width > 0);
            await room.Settle();
            CardNetworkTests.Click(use);
        }
    }
}
