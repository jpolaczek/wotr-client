using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class RegionInspectionTests
    {
        [UnityTest]
        public IEnumerator EmptySettlementsRequireClickInBothPerspectivesAndAllLayouts()
        {
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var fixture = new BoardFixture();
                        fixture.View.You = side;
                        if (side == Side.Shadow)
                        {
                            fixture.View.Decision = null;
                            fixture.View.Options.Clear();
                        }
                        using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
                        var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        texture.Create();
                        client.Panel.targetTexture = texture;
                        client.Panel.scale = scale;
                        client.Apply(fixture.View);
                        yield return Settle();
                        var board = client.Board;
                        board.Map.Focus("pelargir");
                        yield return Settle();
                        var point = Terrain(board, "pelargir");
                        Move(board, point);
                        yield return new WaitForSecondsRealtime(.3f);
                        Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
                        ClickTerrain(board, point);
                        yield return Settle();
                        Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
                        Assert.AreEqual("pelargir", board.Hover.Panel.Region);
                        Assert.AreEqual("No forces", board.Hover.Panel.Root.Q<Label>("hover-empty").text);
                        Assert.AreEqual(0, board.View.Revision);
                        Assert.IsEmpty(board.ActionDie.DieId);
                        LayoutTests.Save(texture, $"ui47-context-empty-{side}-{width}-{scale:0.00}.png");
                        CardGestures.Move(board.Phase);
                        yield return new WaitForSecondsRealtime(.25f);
                        Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display, "Click inspection survives pointer exit.");
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
        }

        [UnityTest]
        public IEnumerator OccupiedRegionsNeverOpenOnHoverAndClickedContentsFollowTheProjection()
        {
            var fixture = new BoardFixture();
            var character = new CharacterView
            {
                Id = "saruman", DefinitionId = "base.character.saruman", Location = "pelargir", Side = Side.Shadow
            };
            fixture.View.Characters.Add(character);
            using var client = new MountedMatchClient("http://localhost", fixture.View.MatchId);
            client.Apply(fixture.View);
            yield return Settle();
            var board = client.Board;
            board.Map.Focus("pelargir");
            yield return Settle();
            var point = Terrain(board, "pelargir");
            Move(board, point);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            ClickTerrain(board, point);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            Assert.NotNull(board.Hover.Panel.Root.Q("hover-figure-saruman"));

            character.Location = "orthanc";
            client.Refresh(fixture.View);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            Assert.IsNull(board.Hover.Panel.Root.Q("hover-figure-saruman"));
            board.Hover.Close();
            Move(board, point);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            ClickTerrain(board, point);
            client.Refresh(fixture.View);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);

            board.Hover.Close();
            fixture.View.Fellowship.RegionId = "pelargir";
            fixture.View.Fellowship.Progress = 2;
            client.Refresh(fixture.View);
            Move(board, point);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display);
            ClickTerrain(board, point);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            StringAssert.Contains("Progress 2", board.Hover.Panel.Root.Q<Label>("hover-fellowship").text);
            Assert.IsNull(board.Hover.Panel.Root.Q("hover-empty"));
            fixture.View.Fellowship.MordorStep = 0;
            client.Refresh(fixture.View);
            yield return Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.Hover.Panel.Root.resolvedStyle.display);
            Assert.IsNotNull(board.Hover.Panel.Root.Q("hover-empty"));
            Assert.AreEqual(0, board.View.Revision);
        }

        internal static Vector2 Terrain(BoardScreen board, string region)
        {
            var anchor = board.Map.Data.Shapes[region].Anchor;
            var point = board.Map.LocalToWorld(board.Map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
            Assert.AreEqual(region, board.Map.HitTest(board.Map.WorldToLocal(point)));
            var picked = board.Root.panel.Pick(point);
            Assert.IsTrue(picked == board.Map || board.Map.Contains(picked), $"Terrain covered by {picked?.name}");
            return point;
        }

        internal static void Move(BoardScreen board, Vector2 point)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point });
            board.Root.panel.visualTree.SendEvent(move);
        }

        internal static void ClickTerrain(BoardScreen board, Vector2 point)
        {
            Move(board, point);
            var picked = board.Root.panel.Pick(point);
            Assert.AreSame(board.Map, picked, "Click the terrain, not a marker or an overlay.");
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(up);
        }

        private static IEnumerator Settle()
        {
            for (var frame = 0; frame < 6; frame++)
                yield return null;
        }
    }
}
