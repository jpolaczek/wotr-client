using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // UI-218: the political view's stripes, its captor rule and the stored choice.
    public class PoliticalViewTests
    {
        private static MapContourRing Ring(params float[] xy)
        {
            var points = Enumerable.Range(0, xy.Length / 2).Select(i => new[] { xy[2 * i], xy[2 * i + 1] }).ToList();
            points.Add(points[0]);
            return new MapContourRing { Points = points.ToArray() };
        }

        private static bool Inside(Vector2 point, float min, float max) =>
            point.x >= min - .01f && point.x <= max + .01f && point.y >= min - .01f && point.y <= max + .01f;

        [Test]
        public void StripesFillASquareAndStayInsideIt()
        {
            var square = new List<MapContourRing> { Ring(0, 0, 100, 0, 100, 100, 0, 100) };
            var segments = MapHatching.Segments(square, 10);
            // Lines x + y = 10 … 190 cross the square; the corners (0 and 200) touch it in a single point.
            Assert.AreEqual(19, segments.Count);
            Assert.IsTrue(segments.All(s => Inside(s.From, 0, 100) && Inside(s.To, 0, 100)));
            Assert.IsTrue(segments.All(s => Mathf.Abs(s.From.x + s.From.y - (s.To.x + s.To.y)) < .01f), "every stripe is diagonal");
            var diagonal = segments.Single(s => Mathf.Abs(s.From.x + s.From.y - 100) < .01f);
            Assert.AreEqual(100 * Mathf.Sqrt(2), Vector2.Distance(diagonal.From, diagonal.To), .01f);
        }

        [Test]
        public void AHoleStaysUnstriped()
        {
            var rings = new List<MapContourRing>
            {
                Ring(0, 0, 100, 0, 100, 100, 0, 100),
                new MapContourRing { Hole = true, Points = Ring(40, 40, 40, 60, 60, 60, 60, 40).Points }
            };
            var segments = MapHatching.Segments(rings, 10);
            foreach (var (from, to) in segments)
                for (var step = 0; step <= 20; step++)
                {
                    var point = Vector2.Lerp(from, to, step / 20f);
                    Assert.IsFalse(point.x > 40.5f && point.x < 59.5f && point.y > 40.5f && point.y < 59.5f,
                        $"stripe point {point} lies in the hole");
                }
            var through = segments.Where(s => Mathf.Abs(s.From.x + s.From.y - 100) < .01f).ToList();
            Assert.AreEqual(2, through.Count, "the stripe through the hole is cut in two");
        }

        [Test]
        public void NothingToStripeGivesNoSegments()
        {
            Assert.IsEmpty(MapHatching.Segments(new List<MapContourRing>(), 10));
            Assert.IsEmpty(MapHatching.Segments(new List<MapContourRing> { Ring(0, 0, 10, 0, 10, 10) }, 0));
        }

        [Test]
        public void OnlyASettlementHeldByTheOtherSideHasACaptor()
        {
            var minasTirith = new RegionDefinition { Id = "minas-tirith", Nation = "gondor", OriginalOwner = Side.FreePeoples };
            var osgiliath = new RegionDefinition { Id = "osgiliath", OriginalOwner = null };
            var control = new Dictionary<string, Side?> { ["minas-tirith"] = Side.FreePeoples, ["osgiliath"] = null };
            Assert.IsNull(MapPoliticalView.Captor(minasTirith, control));
            Assert.IsNull(MapPoliticalView.Captor(osgiliath, control));
            control["minas-tirith"] = Side.Shadow;
            Assert.AreEqual(Side.Shadow, MapPoliticalView.Captor(minasTirith, control));
            Assert.IsNull(MapPoliticalView.Captor(new RegionDefinition { Id = "unknown", OriginalOwner = Side.Shadow }, control));
        }

        [Test]
        public void TheChosenViewIsKeptAndUnknownSettingsStartNormal()
        {
            var key = "test.map-view." + Guid.NewGuid().ToString("N");
            try
            {
                Assert.IsFalse(new MapViewPreference(key).Political);
                new MapViewPreference(key).SetPolitical(true);
                Assert.IsTrue(new MapViewPreference(key).Political);
                PlayerPrefs.SetString(key, "{\"Version\":2,\"Political\":true}");
                Assert.IsFalse(new MapViewPreference(key).Political);
                PlayerPrefs.SetString(key, "not json");
                Assert.IsFalse(new MapViewPreference(key).Political);
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
