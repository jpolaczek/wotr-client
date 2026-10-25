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
    // UI-332 (owner 2026-10-25): a tile the Hunt pool gains — a special tile put into play, the prepared tiles joining the
    // pool at the Mordor entry — shows its face in the middle of the board and drops into the console's Hunt plaque; the
    // Hunt window lists the tiles waiting for Mordor apart from the pool. A quiet view and Skip animations draw nothing.
    public sealed class HuntTileDropTests
    {
        private static readonly HuntTileFaceView Special = new HuntTileFaceView { Pool = "special-sh", Value = "1", Reveal = true, Stop = true, Source = "base" };

        [UnityTest]
        public IEnumerator ATilePutIntoPlayShowsItsFaceThenDropsIntoTheHuntPlaque()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-tile-drop");
            var view = View(1);
            client.Apply(view);
            yield return new WaitForSecondsRealtime(.3f);
            var board = client.Board;
            Assert.AreEqual(0, board.HuntDrops.Shown, "The first view is only the point of comparison.");
            var prepared = View(2);
            prepared.HuntPool.Prepared.Add(Copy(Special));
            client.Apply(prepared);
            yield return new WaitForSecondsRealtime((float)(HuntTileDrop.ShowSeconds + HuntTileDrop.HoldSeconds / 2));
            Assert.AreEqual(1, board.HuntDrops.Shown, "The gained tile shows.");
            var tile = board.HuntDrops.Root.Q(className: HuntTileDrop.TileClass);
            Assert.IsNotNull(tile.Q(className: "hunt-face-pool-special-sh"), "Its own face, without text.");
            Assert.IsEmpty(tile.Query<Label>().ToList().Where(label => label.text.Length > 2), "No explanation beside it.");
            var shown = tile.worldBound.center;
            var plaque = board.Hunt.Tab.worldBound.center;
            LayoutTests.Save(client.Viewport, "ui332-tile-shown.png");
            yield return new WaitForSecondsRealtime((float)(HuntTileDrop.HoldSeconds / 2 + HuntTileDrop.DropSeconds / 2));
            Assert.Less(Vector2.Distance(tile.worldBound.center, plaque), Vector2.Distance(shown, plaque), "It drops toward the Hunt plaque.");
            LayoutTests.Save(client.Viewport, "ui332-tile-dropping.png");
            yield return new WaitForSecondsRealtime((float)HuntTileDrop.DropSeconds / 2 + .1f);
            Assert.AreEqual(0, board.HuntDrops.Shown, "It has landed.");
            Assert.IsTrue(PresentationGlow.IsLit(board.Hunt.Tab), "The plaque lights as it lands.");

            // The Hunt window lists it apart from the pool, under its own heading.
            var pool = board.Hunt.Pool.Root;
            var waiting = pool.Q("hunt-pool-waiting");
            Assert.AreEqual(DisplayStyle.Flex, waiting.style.display.value);
            Assert.AreEqual("Join on the Mordor track · 1", waiting.Q<Label>("hunt-pool-prepared-heading").text);
            Assert.AreEqual(1, waiting.Q("hunt-pool-prepared").childCount);
            Assert.IsFalse(pool.Q("hunt-pool-body").Contains(waiting), "Not part of the pool's block.");

            // The Mordor entry: the prepared tile joins the pool and drops into the plaque again.
            var mordor = View(3);
            mordor.HuntPool.Tiles.Add(Copy(Special));
            mordor.HuntPool.Count++;
            client.Apply(mordor);
            yield return new WaitForSecondsRealtime((float)HuntTileDrop.ShowSeconds);
            Assert.AreEqual(1, board.HuntDrops.Shown, "The joining tile drops into the plaque.");
            Assert.AreEqual(DisplayStyle.None, pool.Q("hunt-pool-waiting").style.display.value, "Nothing waits any more.");
        }

        [UnityTest]
        public IEnumerator AQuietViewAndSkipAnimationsDropNothing()
        {
            using (var client = new MountedMatchClient("http://localhost", "hunt-tile-quiet"))
            {
                client.Apply(View(1));
                yield return new WaitForSecondsRealtime(.3f);
                var quiet = View(2);
                quiet.HuntPool.Prepared.Add(Copy(Special));
                client.Board.ApplyQuietly(quiet);
                yield return null;
                Assert.AreEqual(0, client.Board.HuntDrops.Shown, "A quiet view only sets the point of comparison.");
            }
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var gained = stage.Latest();
            gained.Revision++;
            gained.HuntPool ??= new HuntPoolView();
            gained.HuntPool.Prepared.Add(Copy(Special));
            stage.Board.Apply(gained);
            yield return null;
            Assert.AreEqual(0, stage.Board.HuntDrops.Shown, "Skip animations draws nothing.");
        }

        private static PlayerView View(long revision)
        {
            var view = new BoardFixture().View;
            view.Revision = revision;
            view.HuntPool = new HuntPoolView
            {
                Count = 3,
                Tiles = { new HuntTileFaceView { Pool = "standard", Value = "eye", Count = 2, Source = "base" },
                    new HuntTileFaceView { Pool = "standard", Value = "2", Count = 1, Source = "base" } }
            };
            return view;
        }

        private static HuntTileFaceView Copy(HuntTileFaceView face) => new HuntTileFaceView
        {
            Pool = face.Pool, Value = face.Value, Reveal = face.Reveal, Stop = face.Stop, Source = face.Source, Count = 1
        };
    }
}
