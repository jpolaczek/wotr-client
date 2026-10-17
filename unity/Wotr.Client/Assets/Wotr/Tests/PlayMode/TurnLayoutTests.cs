using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public class TurnLayoutTests
    {
        [UnityTest]
        public IEnumerator RingHuntAndEightCardsFitWithTenDiceAndNoScroll()
        {
            var bootstrap = Object.FindAnyObjectByType<ClientBootstrap>();
            if (bootstrap == null)
                bootstrap = new GameObject("Turn layout").AddComponent<ClientBootstrap>();
            bootstrap.ShowFixture();
            var screen = bootstrap.BoardScreen;
            var document = bootstrap.GetComponent<UIDocument>();
            foreach (var width in new[] { 1366, 1920 })
            {
                var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                target.Create();
                document.panelSettings.targetTexture = target;
                var view = View(screen.Presentation);
                var tray = new DecisionTray(screen.Tray);
                var draft = new TurnDraft();
                foreach (var kind in new[] { CommandKind.UseRing, CommandKind.DiscardCards, CommandKind.AllocateHunt })
                {
                    view.Options.Clear();
                    view.Decision = new DecisionView { Id = kind.ToString(), Kind = kind == CommandKind.DiscardCards ? DecisionKind.DiscardCards : kind == CommandKind.AllocateHunt ? DecisionKind.AllocateHunt : DecisionKind.Action };
                    view.Options.Add(new ActionOption
                    {
                        Kind = kind,
                        EligibleIds = kind == CommandKind.UseRing ? view.Rings.Select(r => r.Id).ToList()
                            : kind == CommandKind.DiscardCards ? view.Hand.ToList() : view.Dice.Where(d => d.Owner == Side.Shadow).Select(d => d.Id).ToList(),
                        EligibleDice = kind == CommandKind.UseRing ? view.Dice.Where(d => d.Owner == Side.Shadow).Select(d => d.Id).ToList() : new System.Collections.Generic.List<string>(),
                        Faces = { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.ArmyMuster, DieFace.Event, DieFace.Eye },
                        Minimum = kind == CommandKind.DiscardCards ? 2 : 1,
                        Maximum = kind == CommandKind.DiscardCards ? 2 : 7
                    });
                    draft.Choose(kind);
                    screen.Apply(view);
                    tray.Render(view, draft, false, false);
                    tray.Preview(false, "Select your components, preview the cost, then confirm.");
                    for (var frame = 0; frame < 12; frame++)
                        yield return null;
                    LayoutTests.Save(target, $"turn-{kind}-{width}.png");
                    Assert.AreEqual(0, screen.Tray.Query<ScrollView>().ToList().Count);
                    foreach (var button in screen.Root.Query<Button>().ToList().Where(b => b.ClassListContains("action") && !b.ClassListContains("army")))
                    {
                        Assert.LessOrEqual(button.worldBound.yMax, screen.Root.worldBound.yMax + 1, button.text);
                        Assert.LessOrEqual(button.worldBound.xMax, screen.Root.worldBound.xMax + 1, button.text);
                    }
                }
                document.panelSettings.targetTexture = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        private static PlayerView View(BoardPresentation data)
        {
            var view = new PlayerView
            {
                MatchId = "layout",
                You = Side.Shadow,
                Turn = 4,
                Phase = GamePhase.ActionResolution,
                Fellowship = new FellowshipView { RegionId = "rivendell", Hidden = true, CompanionCount = 7 },
                Control = data.Board.Regions.ToDictionary(r => r.Id, r => r.OriginalOwner)
            };
            view.Units = data.Board.Units.Select(u => new UnitView
            {
                Id = u.Id,
                Nation = u.Nation,
                Kind = u.Kind,
                Location = u.Location,
                Side = data.Board.Nations.Single(n => n.Id == u.Nation).Side
            }).ToList();
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                for (var i = 0; i < (side == Side.Shadow ? 10 : 6); i++)
                    view.Dice.Add(new DieView { Id = side + "." + i, Owner = side, Face = (DieFace)(i % 5), Location = DieLocation.Available });
            for (var i = 0; i < 3; i++)
                view.Rings.Add(new RingView { Id = "ring." + i, Owner = Side.Shadow });
            for (var i = 1; i <= 8; i++)
                view.Hand.Add("base.sh.character." + i.ToString("D2"));
            return view;
        }
    }
}
