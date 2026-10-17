using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-153: the Mordor track's geometry is presentation data; it must stay on the map, clear of settlements,
    // and lead from both gates through six steps to the Crack of Doom on Orodruin.
    public sealed class MordorTrackGeometryTests
    {
        // The crater of Orodruin on MapCanvas/terrain.jpg, in map units.
        private static readonly Vector2 Orodruin = new Vector2(1348, 850);

        [Test]
        public void SixStepsLeadFromBothGatesToTheCrackOfDoomOnOrodruin()
        {
            var board = BoardPresentation.Load();
            var track = board.MordorTrack;
            Assert.IsNotNull(track);
            Assert.AreEqual(6, track.Steps.Count);
            CollectionAssert.AreEquivalent(new[] { "morannon", "minas-morgul" }, track.Entrances.Select(e => e.Region));
            Assert.Less(Vector2.Distance(track.Step(MordorTrackGeometry.LastStep), Orodruin), 8f, "The last step is the crater.");
            foreach (var entrance in track.Entrances)
            {
                var anchor = board.Shapes[entrance.Region].Anchor;
                Assert.Less(Vector2.Distance(entrance.Gate, new Vector2(anchor[0], anchor[1])), 40f,
                    entrance.Region + "'s gate stands beside its settlement.");
            }
            var gates = track.Entrances.Select(e => e.Gate.x).Max();
            Assert.IsTrue(Enumerable.Range(0, 6).All(step => track.Step(step).x > gates), "The steps lie east of the gates, in Mordor.");
            Assert.IsTrue(new Rect(0, 0, board.Geometry.Width, board.Geometry.Height).Contains(track.Bounds.min));
            Assert.IsTrue(new Rect(0, 0, board.Geometry.Width, board.Geometry.Height).Contains(track.Bounds.max));
        }

        [TestCase("steps")]
        [TestCase("entrance")]
        [TestCase("gate")]
        [TestCase("settlement")]
        [TestCase("order")]
        public void InvalidTrackCannotLoad(string failure)
        {
            var board = BoardPresentation.Load();
            var track = JsonConvert.DeserializeObject<MordorTrackGeometry>(Resources.Load<TextAsset>(MordorTrackGeometry.Resource).text);
            switch (failure)
            {
                case "steps": track.Steps.RemoveAt(3); break;
                case "entrance": track.Entrances.RemoveAt(1); break;
                case "gate": track.Entrances[0].Path[track.Entrances[0].Path.Count - 1] = new[] { 1200f, 800f }; break;
                case "settlement": track.Steps[2] = board.Shapes["barad-dur"].Anchor.ToArray(); track.Path[4] = track.Steps[2]; break;
                case "order": (track.Steps[1], track.Steps[2]) = (track.Steps[2], track.Steps[1]); break;
            }
            Assert.Throws<InvalidDataException>(() => MordorTrackGeometry.Validate(track, board.Geometry));
        }
    }
}
