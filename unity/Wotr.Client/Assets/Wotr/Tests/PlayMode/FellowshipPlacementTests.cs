using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-513 (owner, a picture of Dagorlad): the Fellowship's figure stands well inside its region beside a Shadow army,
    // not on the region's border.
    public sealed class FellowshipPlacementTests
    {
        [UnityTest]
        public IEnumerator TheFellowshipStandsInsideDagorladBesideAnArmy()
        {
            var fixture = new BoardFixture(false);
            fixture.View.MatchId = "fellowship-placement";
            fixture.View.Fellowship.RegionId = "dagorlad";
            fixture.Put("sauron", UnitKind.Regular, "dagorlad", 3);
            fixture.Put("sauron", UnitKind.Elite, "dagorlad", 1);
            fixture.Refresh();
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            var texture = new RenderTexture(1366, 768, 24);
            texture.Create();
            client.Panel.targetTexture = texture;
            client.Apply(fixture.View);
            yield return Settle();
            client.Board.Hover.Close();
            var map = client.Board.Map;
            var feet = map.Stands.Stands("dagorlad").Fellowship;
            map.FocusOn(feet, Mathf.Min(map.FitZoom * 2.2f, map.Camera.MaximumZoom));
            yield return Settle();
            LayoutTests.Save(texture, "ui513-dagorlad.png");
            Assert.IsTrue(MapContourMath.Contains(map.Data.Shapes["dagorlad"], feet), "The Fellowship's feet stand in Dagorlad.");
            client.Panel.targetTexture = client.Viewport;
            texture.Release();
            Object.Destroy(texture);
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 4; i++)
                yield return null;
        }
    }
}
