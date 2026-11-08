using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-516 (owner, 2026-11-07): a card leaving the table is seen to leave, by the Effects tab — in the open panel its tile
    // is struck through and then fades where it stood; with the panel closed its struck-through name stands beside the tab;
    // under Skip animations the tile goes at once.
    public sealed class TableCardLeavingTests
    {
        private const string Tom = "base.fp.strategy.03";
        private const string TomTitle = "Tom Bombadil";

        [UnityTest]
        public IEnumerator InTheOpenPanelTheLeavingTileIsStruckThroughThenFades()
        {
            var view = WithCard();
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Frames(6);
            client.Board.TableEffects.Open();
            yield return new WaitForSecondsRealtime(.4f);
            var effects = client.Board.TableEffects;
            Assert.IsNotNull(effects.Root.Q("effect-source-" + Tom), "The card is on the table.");
            client.Apply(Without(view));
            yield return null;
            Assert.IsNull(effects.Root.Q("effect-source-" + Tom), "Gone for every query at once.");
            var leaving = effects.Root.Q(className: TableCardLeaving.LeavingClass);
            Assert.IsNotNull(leaving, "Its tile stays where it stood, leaving.");
            Assert.IsNotNull(leaving.Q(TableCardLeaving.StrikeName), "Struck through.");
            Assert.IsTrue(leaving.Query<VisualElement>().ToList().All(element => element.pickingMode == PickingMode.Ignore), "Nothing to click.");
            yield return Frames(3);
            LayoutTests.Save(client.Viewport, "ui516-leaving-open.png");
            yield return new WaitForSecondsRealtime((TableCardLeaving.StrikeMilliseconds + Appearance.CloseMilliseconds + 400) / 1000f);
            Assert.IsNull(leaving.parent, "Then it fades and goes.");
        }

        [UnityTest]
        public IEnumerator WithThePanelClosedTheStruckNameStandsBesideTheTab()
        {
            var view = WithCard();
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Frames(6);
            var effects = client.Board.TableEffects;
            Assert.IsFalse(effects.IsOpen);
            client.Apply(Without(view));
            yield return Frames(4);
            var notice = client.Board.Root.Q<Label>(TableCardLeaving.NoticeName);
            Assert.IsNotNull(notice, "The leaving card is named beside the tab.");
            StringAssert.Contains(TomTitle, notice.text);
            StringAssert.Contains("<s>", notice.text, "Struck through.");
            Assert.AreEqual(PickingMode.Ignore, notice.pickingMode);
            Assert.GreaterOrEqual(notice.worldBound.xMin, effects.Tab.worldBound.xMax, "Beside the tab.");
            Assert.AreEqual(effects.Tab.worldBound.yMin, notice.worldBound.yMin, 1f, "Level with it.");
            LayoutTests.Save(client.Viewport, "ui516-leaving-closed.png");
            yield return new WaitForSecondsRealtime((TableCardLeaving.NoticeMilliseconds + Appearance.CloseMilliseconds + 400) / 1000f);
            Assert.IsNull(client.Board.Root.Q<Label>(TableCardLeaving.NoticeName), "Then it goes.");
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsTheTileGoesAtOnce()
        {
            var view = WithCard();
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Interface.Preferences.SetReducedMotion(true);
            client.Apply(view);
            yield return Frames(6);
            client.Board.TableEffects.Open();
            yield return new WaitForSecondsRealtime(.4f);
            client.Apply(Without(view));
            yield return null;
            Assert.IsNull(client.Board.TableEffects.Root.Q(className: TableCardLeaving.LeavingClass), "No fading copy.");
            Assert.IsNull(client.Board.TableEffects.Root.Q("effect-source-" + Tom));
        }

        private static PlayerView WithCard()
        {
            var view = new BoardFixture().View;
            view.MatchId = "table-card-leaving";
            view.Effects.Add(new ActiveEffectView
            {
                Id = "tom", SourceId = Tom, SourceKind = "Card", Title = TomTitle, Owner = Side.FreePeoples,
                Targets = { new EffectTargetView { Kind = "Region", Id = "old-forest", Label = "Old Forest" } }, ExpiryDescription = "Until removed."
            });
            return view;
        }

        // The next revision without the card.
        private static PlayerView Without(PlayerView view)
        {
            var next = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            next.Revision = view.Revision + 1;
            next.Effects.RemoveAll(effect => effect.SourceId == Tom);
            return next;
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }
    }
}
