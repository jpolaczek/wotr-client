using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class MapContourDataTests
    {
        [Test]
        public void ReviewedRegionsRingsAndAnchorsShareOneCoordinateSystem()
        {
            var board = BoardPresentation.Load();
            var geometry = board.Geometry;
            Assert.AreEqual("contours-2", geometry.Version);
            Assert.AreEqual(105, geometry.Regions.Count);
            Assert.AreEqual(106, geometry.Regions.Sum(r => r.Rings.Count));
            Assert.AreEqual(26455, geometry.Regions.Sum(r => r.Rings.Sum(ring => ring.Points.Length - 1)));
            CollectionAssert.AreEquivalent(board.Regions.Keys, board.Shapes.Keys);
            foreach (var region in geometry.Regions)
            {
                Assert.AreEqual(region.Id, board.Contours.HitTest(new Vector2(region.Anchor[0], region.Anchor[1])), region.Id);
                foreach (var ring in region.Rings)
                    CollectionAssert.AreEqual(ring.Points[0], ring.Points[ring.Points.Length - 1]);
            }
            Assert.AreSame(board.Contours, BoardPresentation.Load().Contours, "Immutable contour index is reused.");
        }

        [Test]
        public void EveryReviewedPathRetainsItsOwnersAndAllLegalContacts()
        {
            var board = BoardPresentation.Load();
            var boundaries = board.Geometry.Boundaries;
            Assert.AreEqual(323, boundaries.Count);
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 323), boundaries.Select(b => b.SourceIndex));
            var contacts = boundaries.Where(b => b.Owners.All(o => o != null))
                .Select(b => Pair(b.Owners[0], b.Owners[1])).ToHashSet();
            Assert.AreEqual(282, contacts.Count);
            var legal = board.Board.Regions.SelectMany(r => r.Neighbors.Select(n => Pair(r.Id, n))).ToHashSet();
            Assert.AreEqual(232, legal.Count);
            Assert.IsTrue(legal.IsSubsetOf(contacts));
            Assert.AreEqual(50, contacts.Except(legal).Count(), "Physical contact does not grant movement.");
        }

        [Test]
        public void WaterHoleSeaAndConcaveNotchesDoNotSelectLand()
        {
            var board = BoardPresentation.Load();
            var hole = MapContourData.FromSource(board.Geometry, new Vector2(1839.875f, 1028.5f));
            Assert.IsNull(board.Contours.HitTest(hole), "Preserve the small Gorgoroth water cutout.");
            Assert.IsFalse(MapContourMath.Contains(board.Shapes["gorgoroth"], hole));
            Assert.IsNull(board.Contours.HitTest(Vector2.zero));
            Assert.IsNull(board.Contours.HitTest(new Vector2(-1, 100)));
            var concave = new RegionGeometry
            {
                Rings = new List<MapContourRing> { new MapContourRing { Points = new[]
                {
                    new float[] { 0, 0 }, new float[] { 4, 0 }, new float[] { 4, 1 },
                    new float[] { 1, 1 }, new float[] { 1, 4 }, new float[] { 0, 4 }, new float[] { 0, 0 }
                } } }
            };
            Assert.IsTrue(MapContourMath.Contains(concave, new Vector2(.5f, 3)));
            Assert.IsFalse(MapContourMath.Contains(concave, new Vector2(2, 2)));
        }

        [Test]
        public void SourceAnchorsAndPngUseTheDocumentedTranslationAndUniformAspectFit()
        {
            var board = BoardPresentation.Load();
            Assert.AreEqual(new Vector2(724, 209), MapContourData.FromSource(board.Geometry, new Vector2(1059, 249)));
            CollectionAssert.AreEqual(new float[] { 724, 209 }, board.Shapes["rivendell"].Anchor);
            var center = MapContourData.ToPng(board.Geometry, new Vector2(816, 669));
            Assert.That(Vector2.Distance(center, new Vector2(3072, 2518.5f)), Is.LessThan(.001f));
            Assert.Greater(MapContourData.ToPng(board.Geometry, Vector2.zero).x, 0, "SVG xMidYMid letterbox subpixel.");
        }

        [Test]
        public void SharedLineTieIsStableAcrossSourceOrderingAndCameraChanges()
        {
            var board = BoardPresentation.Load();
            var edge = board.Geometry.Boundaries.First(b => b.Owners.All(o => o != null));
            var point = new Vector2((edge.Points[0][0] + edge.Points[1][0]) / 2,
                (edge.Points[0][1] + edge.Points[1][1]) / 2);
            var expected = edge.Owners.OrderBy(id => id, StringComparer.Ordinal).First();
            Assert.AreEqual(expected, board.Contours.HitTest(point));
            var clone = Read();
            clone.Regions.Reverse();
            Assert.AreEqual(expected, new MapContourData(clone, board.Regions.Keys).HitTest(point));
            var canvas = new MapCanvas(board);
            canvas.Camera.Zoom = 2;
            canvas.Camera.Pan = new Vector2(22, -10);
            Assert.AreEqual(expected, canvas.HitTest(canvas.Camera.ToScreen(point)));
        }

        [TestCase("version")]
        [TestCase("scale")]
        [TestCase("foreign-id")]
        [TestCase("duplicate-id")]
        [TestCase("mask")]
        [TestCase("unclosed")]
        [TestCase("crossing")]
        [TestCase("water-anchor")]
        [TestCase("boundary")]
        [TestCase("missing-provenance")]
        [TestCase("nonfinite")]
        public void MalformedContentFailsBeforeTheMapIsCreated(string failure)
        {
            var geometry = Read();
            var region = geometry.Regions[0];
            switch (failure)
            {
                case "version": geometry.Version = "geometry-1"; break;
                case "scale": geometry.Transform.SourceScale = 2; break;
                case "foreign-id": region.Id = "not-a-region"; break;
                case "duplicate-id": region.Id = geometry.Regions[1].Id; break;
                case "mask": region.MaskNumber = 0; break;
                case "unclosed": region.Rings[0].Points[0][0] += 1; break;
                case "crossing": region.Rings[0].Points = CrossingRing(); break;
                case "water-anchor": geometry.Regions.Single(r => r.Id == "gorgoroth").Anchor = new[] { 1504.875f, 988.5f }; break;
                case "boundary": geometry.Boundaries[0].Points[0][0] += 1; break;
                case "missing-provenance": geometry.Boundaries[1].SourceIndex = 0; break;
                case "nonfinite": region.Anchor[0] = float.NaN; break;
            }
            Assert.Throws<InvalidDataException>(() => new MapContourData(geometry, BoardPresentation.Load().Regions.Keys));
        }

        private static float[][] CrossingRing() => new[]
        {
            new float[] { 10, 10 }, new float[] { 30, 10 }, new float[] { 10, 30 },
            new float[] { 30, 30 }, new float[] { 20, 40 }, new float[] { 10, 10 }
        };

        private static MapGeometry Read() => JsonConvert.DeserializeObject<MapGeometry>(Resources.Load<TextAsset>("map-geometry").text);
        private static string Pair(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + ":" + b : b + ":" + a;
    }
}
