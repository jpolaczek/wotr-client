using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed partial class CouncilHomelandPanelNetworkTests
    {
        [UnityTest]
        public IEnumerator FateGimliUsesEitherHomelandWithOrWithoutNewCitiesAndNoNationIcon() => MatchTestRoom.Wait(Fate());

        private static async Task Fate()
        {
            foreach (var cities in new[] { false, true })
                foreach (var destination in new[] { "iron-hills", "ered-luin" })
                {
                    using var room = await UI10TestRoom.Connect($"ui69-fate-{cities}-{destination}");
                    var board = room.Fp.Board;
                    var choice = board.View.Options.Single().CharacterActions.Single();
                    CollectionAssert.AreEquivalent(new[] { "iron-hills", "ered-luin" }, choice.Targets);
                    CollectionAssert.DoesNotContain(choice.Targets, "erebor");
                    Assert.AreEqual("—", Portrait(board, Gimli).Q<Label>("homeland-nation").text);
                    StringAssert.Contains("No nation icon", Portrait(board, Gimli).tooltip);
                    var politics = JsonConvert.SerializeObject(board.View.Nations);
                    var shadow = JsonConvert.SerializeObject(room.Sh.Board.View);
                    await Place(room, Gimli, "iron-hills", false);
                    await Place(room, Gimli, "ered-luin", true);
                    await Press(room, board, "homeland-stay");
                    Assert.AreEqual("Fellowship", Portrait(board, Gimli).Q<Label>("homeland-location").text);
                    Assert.AreEqual("No tokens", Effect(board));
                    await Place(room, Gimli, destination, destination == "ered-luin");
                    Assert.AreEqual(0, room.Boundary.Commands);
                    Assert.AreEqual(shadow, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                        "api/matches/" + board.View.MatchId, room.Token)));
                    await Confirm(room);
                    var figure = room.Sh.Board.View.Characters.Single(c => c.Id == Gimli);
                    Assert.AreEqual(destination, figure.Location);
                    Assert.AreEqual("fate.character.gimli-dwarven-lord", figure.DefinitionId);
                    Assert.IsEmpty(figure.NationIcon);
                    Assert.AreEqual(politics, JsonConvert.SerializeObject(board.View.Nations),
                        "Placement with no nation icon must not activate or advance a nation.");
                    Assert.IsFalse(board.View.Fellowship.CompanionIdentities.Contains(Gimli));
                    Assert.AreEqual(CharacterRosterLifecycle.OnBoard,
                        board.View.OwnCharacterRoster.Single(c => c.FigureId == Gimli).Lifecycle);
                    Assert.AreEqual(DecisionKind.CouncilToken, room.Sh.Board.View.Decision.Kind);
                    Assert.IsEmpty(room.Sh.Board.View.CouncilTokens);
                }
        }
    }
}
