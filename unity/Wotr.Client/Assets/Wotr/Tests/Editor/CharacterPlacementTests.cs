using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-402 (TABLETOP_FIGURES_UX, "Stand places"): every character stands on its own place beside its army — a field's on
    // the six character places, a garrison's on the three garrison character places — keeps its place while others come and
    // go, and more characters than places stand as a stacked pair on the last place.
    public sealed class CharacterPlacementTests
    {
        private static readonly Dictionary<string, (StandKind Kind, int Index)> None = new Dictionary<string, (StandKind, int)>();

        [Test]
        public void FieldAndGarrisonCharactersTakeTheirOwnPlacesTheFieldArmysSideFirst()
        {
            var characters = new[]
            {
                Figure("witch-king", Side.Shadow, false),
                Figure("strider", Side.FreePeoples, false),
                Figure("gandalf", Side.FreePeoples, false),
                Figure("gimli", Side.FreePeoples, true)
            };
            var placements = CharacterPlacement.Assign(characters, Side.FreePeoples, true, Previous(None));
            Assert.AreEqual(4, placements.Count, "Each character stands alone.");
            Assert.IsTrue(placements.All(p => p.Members.Count == 1));
            var gimli = placements.Single(p => p.Front.Id == "gimli");
            Assert.AreEqual((StandKind.GarrisonCharacter, 0), (gimli.Kind, gimli.Index), "A garrison's character stands at the gate.");
            var field = placements.Where(p => p.Kind == StandKind.Character).OrderBy(p => p.Index).Select(p => p.Front.Id);
            CollectionAssert.AreEqual(new[] { "gandalf", "strider", "witch-king" }, field,
                "The field army's side stands nearest it; the other side's characters beyond.");
            var lone = CharacterPlacement.Assign(new[] { Figure("gimli", Side.FreePeoples, true) }, null, false, Previous(None)).Single();
            Assert.AreEqual(StandKind.Character, lone.Kind, "Without a garrison place a garrison's character stands with the field's.");
        }

        [Test]
        public void ACharacterKeepsItsPlaceWhenAnotherArrivesOrLeaves()
        {
            var places = new Dictionary<string, (StandKind, int)>();
            var first = new[] { Figure("strider", Side.FreePeoples, false), Figure("legolas", Side.FreePeoples, false) };
            Remember(places, CharacterPlacement.Assign(first, Side.FreePeoples, false, Previous(places)));
            Assert.AreEqual((StandKind.Character, 1), places["strider"]);
            var joined = first.Append(Figure("boromir", Side.FreePeoples, false)).ToArray();
            Remember(places, CharacterPlacement.Assign(joined, Side.FreePeoples, false, Previous(places)));
            Assert.AreEqual((StandKind.Character, 0), places["legolas"], "Nobody moves for a newcomer.");
            Assert.AreEqual((StandKind.Character, 1), places["strider"]);
            Assert.AreEqual((StandKind.Character, 2), places["boromir"], "A newcomer takes the next free place.");
            var left = joined.Where(c => c.Id != "legolas").ToArray();
            var after = CharacterPlacement.Assign(left, Side.FreePeoples, false, Previous(places));
            Assert.AreEqual(1, after.Single(p => p.Front.Id == "strider").Index, "Nobody moves when one leaves.");
            Assert.AreEqual(2, after.Single(p => p.Front.Id == "boromir").Index);
        }

        [Test]
        public void MoreThanSixCharactersStandAsAStackedPairOnTheLastPlace()
        {
            var characters = Enumerable.Range(0, 8).Select(i => Figure("companion-" + i, Side.FreePeoples, false)).ToArray();
            var placements = CharacterPlacement.Assign(characters, Side.FreePeoples, false, Previous(None));
            Assert.AreEqual(RegionStands.CharacterPlaces, placements.Count, "Six places, no more.");
            CollectionAssert.AreEqual(Enumerable.Range(0, RegionStands.CharacterPlaces), placements.Select(p => p.Index).OrderBy(i => i));
            var stack = placements.Single(p => p.Members.Count > 1);
            Assert.AreEqual(RegionStands.CharacterPlaces - 1, stack.Index, "The pair stands on the last place.");
            Assert.AreEqual(3, stack.Members.Count, "The sixth in front, the seventh and eighth behind it.");
            CollectionAssert.AreEquivalent(characters.Select(c => c.Id), placements.SelectMany(p => p.Members).Select(c => c.Id),
                "Every character stands somewhere.");
            var garrison = Enumerable.Range(0, 4).Select(i => Figure("guard-" + i, Side.FreePeoples, true)).ToArray();
            var gate = CharacterPlacement.Assign(garrison, Side.FreePeoples, true, Previous(None));
            Assert.AreEqual(RegionStands.GarrisonCharacterPlaces, gate.Count);
            Assert.AreEqual(2, gate.Single(p => p.Index == RegionStands.GarrisonCharacterPlaces - 1).Members.Count);
        }

        private static System.Func<string, (StandKind Kind, int Index)?> Previous(Dictionary<string, (StandKind, int)> places) =>
            id => places.TryGetValue(id, out var place) ? place : ((StandKind, int)?)null;

        private static void Remember(Dictionary<string, (StandKind, int)> places, IEnumerable<CharacterPlacement.Placement> placements)
        {
            places.Clear();
            foreach (var placement in placements)
                places[placement.Front.Id] = (placement.Kind, placement.Index);
        }

        private static CharacterFigure Figure(string id, Side side, bool garrison) =>
            new CharacterFigure { Id = id, DefinitionId = "base.character." + id, RegionId = "minas-tirith", Side = side, Garrison = garrison };
    }
}
