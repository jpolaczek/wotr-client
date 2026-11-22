using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-310 on a mounted Shadow board: the Free Peoples move the Fellowship and its Hunt follows, recorded as the engine
    // records it. The Hunt window opens on the Hunt's start and shows the roll, the tile and the damage entry by entry;
    // the tracks move after the Hunt in TrackSequence's order and never twice; a reveal flies the figure to its public
    // region, a casualty leaves the roster, a new Guide takes the badge, a response marks its object; hiding, healing and
    // entering Mordor show on the Ring, the corruption track and the Mordor track. Frames go to .artifacts/screenshots
    // (ui310-*).
    public sealed class OpponentHuntPresentationTests
    {
        private const string Chain = "presented-hunt/hunt/1";
        private const string Gimli = "base.character.gimli";
        private const string Legolas = "base.character.legolas";
        private const string Merry = "base.character.merry";
        private const string Mithril = "base.fp.character.05";
        private const string AxeAndBow = "base.fp.character.06";
        private const string Balrog = "base.sh.character.17";
        private const string Smeagol = "lords.character.smeagol";

        [UnityTest]
        public IEnumerator AMissOpensTheHuntShowsTheRollAndOnlyThenMovesTheProgress()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Then(o => o.Progress = 2).Entry(HuntReportKind.Progressed)
                .Entry(HuntReportKind.Rolled, Snapshot("BeforeDraw", 3, 2)).Entry(HuntReportKind.HuntCompleted, Snapshot("Done", 3, 2));
            var steps = script.Finish();
            var progress = Plaque(stage, "progress");
            var before = progress.text;
            stage.Play(script.Latest, null, steps);
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.Hunt.Tab));
            Assert.AreEqual(DisplayStyle.Flex, stage.Board.Hunt.Root.style.display.value, "The Hunt window opens on the Hunt's start.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.Hunt.Tab), "The Hunt Box glows.");
            yield return stage.Until(() => Die(stage, "d0") != null);
            Assert.AreEqual(before, progress.text, "The progress waits for the Hunt.");
            // UI-520: the miss is named under the dice and stays readable before the Hunt moves on.
            yield return stage.Until(() => Result(stage) != null);
            var named = Time.realtimeSinceStartup;
            Assert.AreEqual("No hits", Result(stage).text);
            Assert.GreaterOrEqual(Result(stage).worldBound.yMin, stage.Board.Hunt.Dice.Root.worldBound.yMax - 1, "Under the dice.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui310-miss-roll.png");
            yield return stage.Until(() => progress.text != before);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - named, PresentationPace.ResultSeconds - .2f, "It holds while it is read.");
            Assert.IsTrue(stage.Playback.Holding, "The progress moves inside the presentation.");
            yield return stage.Released();
            Assert.AreEqual(2, stage.Board.View.Fellowship.Progress);
            Assert.IsFalse(stage.Board.TracksStaged, "Nothing moves twice at the release.");
        }

        // UI-570 (owner, 2026-11-22: "leave it as in the rules, but add a 'start the hunt' button at the beginning for both
        // players"): with Settings' "Roll my own dice and draws" on, the Hunt waits behind Start the Hunt for the side that
        // moved and for the Shadow alike: its window is open, no dice until the click; then the Shadow's roll plays without a
        // second button. By the rules the Shadow draws the tile (Draw a Hunt tile, the Shadow's only); a tile drawn again by
        // Mithril Coat and Sting is the Free Peoples' (theirs only). Frames: ui570-hunt-start-*.png.
        [UnityTest]
        public IEnumerator TheHuntWaitsForStartAndEachSideDrawsItsOwnTiles([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = viewer == Side.Shadow ? Watching() : new PresentedBoardStage(1366, 1f, false, Side.FreePeoples);
            stage.Preferences.SetConfirmOwnDraws(true);
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Entry(HuntReportKind.Rolled, roll).Entry(HuntReportKind.TileDrawn, Tile(roll, 1))
                .Entry(HuntReportKind.TileReplaced, Tile(roll, 0)).Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 0)));
            var steps = script.Finish();
            if (viewer == Side.FreePeoples)
            {
                steps[0].Actor = Side.FreePeoples;
                steps[0].Costs.Clear();
            }
            stage.Play(script.Latest, null, steps);
            var start = Gate(stage);
            yield return stage.Until(() => (start = Gate(stage)) != null);
            Assert.AreEqual("Start the Hunt", Text(start), viewer + ": the Hunt waits for both players.");
            Assert.AreEqual(DisplayStyle.Flex, stage.Board.Hunt.Root.style.display.value, "Its window is open.");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNull(Die(stage, "d0"), "No dice until the click.");
            Assert.IsTrue(stage.Playback.Holding);
            LayoutTests.Save(stage.Texture, "ui570-hunt-start-" + viewer + ".png");
            PointerGestureEvents.ClickAt(start);
            yield return stage.Until(() => Die(stage, "d0") != null);
            Assert.IsNull(Gate(stage), "The roll follows Start the Hunt.");
            Button draw = null;
            yield return stage.Until(() => (draw = Gate(stage)) != null);
            Assert.AreEqual("Draw a Hunt tile", Text(draw));
            Assert.IsNull(draw.Q<DiePips>(), "A draw's button wears its words alone.");
            PointerGestureEvents.ClickAt(draw);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsNull(Gate(stage), viewer + ": one tile draw is theirs — the Shadow's own draw or the Free Peoples' Mithril.");
            yield return stage.Released();
        }

        private static Button Gate(PresentedBoardStage stage) => stage.Board.Root.Q<Button>(DrawGate.Name);

        private static string Text(Button gate) => gate.Q<Label>(className: DrawGate.Name + "-text").text;

        private static Label Result(PresentedBoardStage stage) =>
            stage.Board.Root.Query<Label>(className: PresentationPace.ResultClass).ToList().FirstOrDefault();

        // UI-343 (owner 2026-10-26: "once the Hunt reaches its end and the next actions start to animate, the Hunt window is
        // to close; if the user closed it earlier it does not open again, only for a separate new Hunt").
        [UnityTest]
        public IEnumerator TheHuntWindowClosesAsTheNextActionBegins()
        {
            using var stage = Watching();
            var north = stage.Before.Nations.Single(n => n.Id == "north");
            north.Active = false;
            north.StepsToWar = 3;
            yield return stage.Ready();
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Then(o => o.Progress = 2).Entry(HuntReportKind.Progressed)
                .Entry(HuntReportKind.Rolled, Snapshot("BeforeDraw", 3, 2)).Entry(HuntReportKind.HuntCompleted, Snapshot("Done", 3, 2));
            var steps = script.Finish().ToList();
            var next = stage.Step(1, 2);
            next.Facts.Nations.Add(new ActionNationChange { Id = "north", WasActive = false, PreviousStepsToWar = 3, Active = false, StepsToWar = 2 });
            steps.Add(next);
            script.Latest.Revision = 2;
            script.Latest.Nations.Single(n => n.Id == "north").StepsToWar = 2;
            var hunt = stage.Board.Hunt;
            var progress = Plaque(stage, "progress");
            var before = progress.text;
            stage.Play(script.Latest, null, steps.ToArray());
            yield return stage.Until(() => hunt.ShowsFinishedHunt);
            yield return stage.Until(() => progress.text != before);
            Assert.IsTrue(hunt.IsOpen, "The result stays while the Fellowship's tracks move after its own Hunt.");
            yield return stage.Until(() => !hunt.IsOpen);
            Assert.IsTrue(stage.Playback.Holding, "It closes as the next action begins, within the presentation.");
            yield return stage.Until(() => !Appearance.Closed(stage.Board.Nations.Drawer));
            LayoutTests.Save(stage.Texture, "ui343-next-action.png");
            yield return stage.Released();
            Assert.IsFalse(hunt.IsOpen, "It does not open again at the release.");
        }

        [UnityTest]
        public IEnumerator AHuntWindowTheUserClosedDoesNotOpenAgainWithinThatHunt()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var steps = Hit(stage, out var latest);
            var hunt = stage.Board.Hunt;
            stage.Play(latest, null, steps);
            yield return stage.Until(() => hunt.IsOpen);
            hunt.Close();
            var reopened = false;
            while (stage.Playback.Holding)
            {
                reopened |= hunt.IsOpen;
                yield return null;
            }
            yield return null;
            reopened |= hunt.IsOpen;
            Assert.IsFalse(reopened, "Closed by the player, it stays closed through the rest of its Hunt and the release.");
        }

        [UnityTest]
        public IEnumerator AHitShowsItsTileAndDamageThenTheProgressAndTheCorruptionAsTwoStages()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var steps = Hit(stage, out var latest);
            var progress = Plaque(stage, "progress");
            var corruption = Plaque(stage, "corruption");
            var order = new List<string>();
            var (shownProgress, shownCorruption) = (progress.text, corruption.text);
            stage.Play(latest, null, steps);
            var tileSeen = false;
            while (stage.Playback.Holding)
            {
                tileSeen |= stage.Board.Hunt.Tile.Root.resolvedStyle.display == DisplayStyle.Flex;
                if (progress.text != shownProgress)
                {
                    order.Add(tileSeen ? "progress after the tile" : "progress before the tile");
                    shownProgress = progress.text;
                }
                if (corruption.text != shownCorruption)
                {
                    order.Add("corruption");
                    shownCorruption = corruption.text;
                }
                yield return null;
            }
            CollectionAssert.AreEqual(new[] { "progress after the tile", "corruption" }, order, "On the journey the progress comes first.");
            yield return stage.Released();
            Assert.AreEqual((2, 4), (stage.Board.View.Fellowship.Progress, stage.Board.View.Fellowship.Corruption));
            Assert.IsFalse(stage.Board.TracksStaged, "The release does not stage the tracks again.");
        }

        [UnityTest]
        public IEnumerator TheRingResponseIsMarkedOnTheFellowshipsRing()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var steps = Hit(stage, out var latest);
            stage.Play(latest, null, steps);
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.Fellowship.Status.Q("fellowship-ring")));
            LayoutTests.Save(stage.Texture, "ui310-ring-response.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ARevealFliesTheFellowshipToItsPublicRegionAfterTheTile()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 5);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Then(o => o.Progress = 2).Entry(HuntReportKind.Progressed).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, Tile(roll, 0, reveal: true)).Entry(HuntReportKind.DamageDetermined, Tile(roll, 0, reveal: true))
                .Then(o =>
                {
                    o.Hidden = false;
                    o.RegionId = "moria";
                    o.Progress = 0;
                }).Entry(HuntReportKind.Revealed).Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 0, reveal: true)));
            var steps = script.Finish();
            stage.Play(script.Latest, "grey-havens", steps);
            yield return stage.Until(() => stage.Board.Map.Figures.Moves.Any(move => move.To.Key == "fellowship"));
            var flight = stage.Board.Map.Figures.Moves.Single(move => move.To.Key == "fellowship");
            Assert.AreEqual(("rivendell", "moria"), (flight.From.Region, flight.To.Region), "The revealed figure flies between public regions.");
            Assert.IsTrue(stage.Board.Hunt.Tile.Root.resolvedStyle.display == DisplayStyle.Flex, "The tile came first.");
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.Fellowship.Status.Q("fellowship-status-condition")));
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui310-reveal.png");
            yield return stage.Released();
            Assert.AreEqual("◆", stage.Board.Map.FellowshipMarker.Symbol);
            Assert.IsFalse(FellowshipStateBadge.ShowsHidden(stage.Board.Map.FellowshipMarker.State), "UI-427: its plaque shows ◆.");
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator ACasualtyLeavesTheRosterThroughItsPortrait()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Then(o => o.Progress = 2).Entry(HuntReportKind.Progressed).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, Tile(roll, 1)).Entry(HuntReportKind.DamageDetermined, Tile(roll, 1))
                .Entry(HuntReportKind.Response, Tile(roll, 1), response: HuntResponse.RandomCompanion)
                .Entry(HuntReportKind.CompanionLeft, character: Merry).Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 1)));
            var steps = script.Finish();
            script.Latest.Fellowship.CompanionIdentities.Remove("merry");
            script.Latest.Fellowship.CompanionCount = 3;
            stage.Play(script.Latest, null, steps);
            yield return stage.Until(() => PresentationGlow.IsLit(Portrait(stage, "merry")));
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui310-casualty.png");
            yield return stage.Until(() => Portrait(stage, "merry") == null);
            Assert.IsTrue(stage.Playback.Holding, "The roster changes inside the presentation.");
            Assert.IsNotNull(Portrait(stage, "pippin"), "Only the casualty leaves.");
            yield return stage.Released();
            Assert.IsNull(Portrait(stage, "merry"));
        }

        [UnityTest]
        public IEnumerator ANewGuideTakesTheBadgeOnItsPortrait()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw")).Then(o => o.GuideId = Legolas)
                .Entry(HuntReportKind.GuideChanged).Entry(HuntReportKind.HuntCompleted, Snapshot("Done"));
            var steps = script.Finish();
            stage.Play(script.Latest, null, steps);
            yield return stage.Until(() => Portrait(stage, "legolas")?.ClassListContains("current-guide") == true);
            Assert.IsTrue(stage.Playback.Holding);
            Assert.IsFalse(Portrait(stage, "gimli").ClassListContains("current-guide"), "One Guide.");
            yield return stage.Until(() => PresentationGlow.IsLit(Portrait(stage, "legolas")));
            yield return stage.Released();
        }

        // UI-462 (owner, 2026-11-03: "we need a graphical animation presenting Sméagol joining the Fellowship"): his tile's
        // new Guide is no Companion yet — the tile glows, his portrait flies out of it into his new place in the roster, which
        // stays empty until it lands, and there takes the Guide's badge.
        [UnityTest]
        public IEnumerator SmeagolsPortraitFliesFromHisTileIntoTheRosterAndTakesTheBadge()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var steps = Joins(stage, out var latest);
            stage.Play(latest, null, steps);
            yield return stage.Until(() => Joining(stage) != null);
            var portrait = Portrait(stage, "smeagol");
            Assert.IsNotNull(portrait, "His place in the roster is there as he flies.");
            Assert.AreEqual(0f, portrait.resolvedStyle.opacity, "It stays empty until he lands.");
            Assert.IsFalse(portrait.ClassListContains("current-guide"), "The badge waits for him.");
            Assert.IsTrue(Portrait(stage, "gimli").ClassListContains("current-guide"), "Gimli still guides.");
            Assert.IsNotNull(Joining(stage).Q(className: "figure-art-image"), "The flying portrait wears his artwork.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.Hunt.Tile.Root), "The tile he comes from glows.");
            var tile = stage.Board.Hunt.Tile.Root.worldBound.center;
            var cell = portrait.worldBound.center;
            yield return stage.Until(() => Joining(stage) is not { } flying ||
                Vector2.Distance(flying.worldBound.center, cell) < Vector2.Distance(tile, cell) * .6f);
            Assert.IsNotNull(Joining(stage), "He is on his way.");
            LayoutTests.Save(stage.Texture, "ui462-join-flight.png");
            yield return stage.Until(() => Portrait(stage, "smeagol").ClassListContains("current-guide"));
            Assert.IsNull(Joining(stage), "He has landed.");
            Assert.AreEqual(1f, Portrait(stage, "smeagol").resolvedStyle.opacity, "His portrait shows.");
            Assert.IsFalse(Portrait(stage, "gimli").ClassListContains("current-guide"), "One Guide.");
            Assert.IsTrue(stage.Playback.Holding, "He joins inside the presentation.");
            yield return stage.Until(() => PresentationGlow.IsLit(Portrait(stage, "smeagol")));
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui462-join-landed.png");
            yield return stage.Released();
            Assert.IsTrue(Portrait(stage, "smeagol").ClassListContains("current-guide"));
            Assert.AreEqual(5, stage.Board.Fellowship.Root.Query(className: "companion-token").ToList().Count, "Five Companions.");
        }

        // UI-462: the player whose own move drew the tile sees him join too — the Hunt is a shared resolution.
        [UnityTest]
        public IEnumerator TheFreePeoplesWhoseMoveDrewHisTileSeeHimJoinToo()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.FreePeoples);
            var fellowship = stage.Before.Fellowship;
            fellowship.RegionId = "rivendell";
            fellowship.Guide = Gimli;
            fellowship.CompanionIdentities.AddRange(new[] { "gimli", "legolas" });
            fellowship.CompanionCount = 2;
            foreach (var id in new[] { Gimli, Legolas })
                stage.Before.OwnCompanions.Add(new CharacterView { Id = id, DefinitionId = id, Side = Side.FreePeoples, Location = "fellowship", Level = 2 });
            yield return stage.Ready();
            var steps = Joins(stage, out var latest);
            steps[0].Actor = Side.FreePeoples;
            steps[0].Costs.Clear();
            latest.OwnCompanions.Add(new CharacterView
            {
                Id = Smeagol, DefinitionId = Smeagol, Side = Side.FreePeoples, Location = "fellowship"
            });
            stage.Play(latest, null, steps);
            yield return stage.Until(() => Joining(stage) != null);
            Assert.AreEqual(0f, Portrait(stage, Smeagol).resolvedStyle.opacity, "His own roster's place waits for him.");
            yield return stage.Until(() => Portrait(stage, Smeagol).ClassListContains("current-guide"));
            Assert.IsNull(Joining(stage));
            yield return stage.Released();
            Assert.IsTrue(Portrait(stage, Smeagol).ClassListContains("current-guide"));
        }

        // UI-462 under Skip animations: no flight — he stands in the roster with the badge and the mark at once.
        [UnityTest]
        public IEnumerator UnderSkipAnimationsSmeagolStandsInTheRosterWithTheBadgeAtOnce()
        {
            using var stage = Watching(reduced: true);
            yield return stage.Ready();
            var steps = Joins(stage, out var latest);
            stage.Play(latest, null, steps);
            var flew = false;
            var deadline = Time.realtimeSinceStartup + 10;
            while (Portrait(stage, "smeagol")?.ClassListContains("current-guide") != true && Time.realtimeSinceStartup < deadline)
            {
                flew |= Joining(stage) != null;
                yield return null;
            }
            Assert.IsFalse(flew, "No flight.");
            Assert.IsTrue(Portrait(stage, "smeagol").ClassListContains("current-guide"), "He takes the badge.");
            Assert.AreEqual(1f, Portrait(stage, "smeagol").resolvedStyle.opacity, "Shown at once.");
            yield return stage.Until(() => PresentationGlow.IsLit(Portrait(stage, "smeagol")));
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator ATableCardAnsweringTheHuntIsMarkedAtTheEffectsTab()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw")).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, Tile(roll, 1)).Entry(HuntReportKind.Reaction, Tile(roll, 1), source: Mithril)
                .Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 0)));
            var steps = script.Finish();
            var buttons = Controls(stage);
            stage.Play(script.Latest, null, steps);
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.TableEffects.Tab));
            Assert.AreEqual(buttons + 1, Controls(stage), "Only the Skip target is added: no response, Continue or result window.");
            yield return stage.Released();
        }

        // UI-546 (owner, 2026-11-18: "when a card's effect triggers during a battle or the Hunt, we must see an animation
        // presenting that card, so it is clear where the effects come from"): Axe and Bow answering the Hunt comes out of
        // the Free Peoples' hand and stands beside the Hunt window, "Takes effect", before its entry is marked. Its second
        // entry (the damage changed) presents it no more, and the entries whose source is no card present nothing.
        // Frame: ui546-hunt-card.
        [UnityTest]
        public IEnumerator ACardAnsweringTheHuntIsPresentedOnceBeforeItsEffect()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw")).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, Tile(roll, 2)).Entry(HuntReportKind.DamageDetermined, Tile(roll, 2))
                .Entry(HuntReportKind.Reaction, Tile(roll, 2), source: AxeAndBow).Entry(HuntReportKind.DamageChanged, Tile(roll, 1), source: AxeAndBow)
                .Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 1)));
            var steps = script.Finish();
            stage.Play(script.Latest, null, steps);
            var appearances = 0;
            var others = new List<string>();
            var shown = false;
            var saved = false;
            var shownAt = -1f;
            var litAt = -1f;
            var deadline = Time.realtimeSinceStartup + 25;
            while (Time.realtimeSinceStartup < deadline && (litAt < 0 || shown))
            {
                var time = Time.realtimeSinceStartup;
                var now = Presented(stage, AxeAndBow);
                if (Triggered(stage) && !now)
                    others.Add(stage.Board.Cards.CardId);
                if (now && !shown)
                {
                    appearances++;
                    shownAt = shownAt < 0 ? time : shownAt;
                    Assert.IsTrue(stage.Board.Hunt.IsOpen, "The Hunt window is open: the card answers it.");
                    Assert.IsFalse(PresentationGlow.IsLit(stage.Board.TableEffects.Tab), "Its entry is not marked yet.");
                }
                if (now && !saved && time - shownAt > CardFlight.Seconds + .3f)
                {
                    Assert.AreEqual(CardTriggerPresenter.TriggerNote, stage.Board.Cards.Note);
                    Assert.IsFalse(stage.Board.Cards.Root.worldBound.Overlaps(stage.Board.Hunt.Root.worldBound),
                        "Beside the Hunt window, never over it.");
                    LayoutTests.Save(stage.Texture, "ui546-hunt-card.png");
                    saved = true;
                }
                if (litAt < 0 && PresentationGlow.IsLit(stage.Board.TableEffects.Tab))
                    litAt = time;
                shown = now;
                yield return null;
            }
            Assert.AreEqual(1, appearances, "Presented once for its two entries.");
            CollectionAssert.IsEmpty(others, "No other source is presented as a card.");
            Assert.IsTrue(saved, "It stood to be read.");
            Assert.Greater(litAt, shownAt + (float)CardTriggerPresenter.HoldSeconds, "Its entry is marked after the card was read.");
            yield return stage.Released();
        }

        // UI-546: the Shadow's Balrog of Moria, an Event revealed as the action began, is not presented again by the Hunt
        // it starts.
        [UnityTest]
        public IEnumerator AnEventThatStartsTheHuntIsNotPresentedTwice()
        {
            using var stage = Watching(viewer: Side.FreePeoples);
            yield return stage.Ready();
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"), source: Balrog)
                .Entry(HuntReportKind.Rolled, roll, source: Balrog).Entry(HuntReportKind.HuntCompleted, Done(roll), source: Balrog);
            var steps = script.Finish();
            steps[0].Kind = "EventPlayed";
            steps[0].Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Event, Id = Balrog, Deck = "character" });
            stage.Play(script.Latest, null, steps);
            var revealed = false;
            var triggered = false;
            var deadline = Time.realtimeSinceStartup + 25;
            while (Time.realtimeSinceStartup < deadline && stage.Playback.Holding)
            {
                revealed |= Presented(stage, Balrog) && stage.Board.Cards.Note == CardRevealPresenter.PlayedNote;
                triggered |= Triggered(stage);
                yield return null;
            }
            Assert.IsTrue(revealed, "The Event was revealed.");
            Assert.IsFalse(triggered, "Its Hunt does not present it again.");
            yield return stage.Released();
        }

        private static bool Presented(PresentedBoardStage stage, string card) => stage.Board.Cards.CardId == card &&
            stage.Board.Cards.Root.resolvedStyle.display == DisplayStyle.Flex;

        private static bool Triggered(PresentedBoardStage stage) => stage.Board.Cards.Note == CardTriggerPresenter.TriggerNote &&
            stage.Board.Cards.Root.resolvedStyle.display == DisplayStyle.Flex;

        [UnityTest]
        public IEnumerator HidingMarksTheRingAndHealingTheCorruptionTrack()
        {
            using var stage = Watching(revealed: true);
            yield return stage.Ready();
            var hide = stage.Paid(stage.Step(1), DieFace.Character);
            hide.Facts.FellowshipBefore = Outcome(stage.Before.Fellowship);
            hide.Facts.FellowshipAfter = Outcome(stage.Before.Fellowship);
            hide.Facts.FellowshipAfter.Hidden = true;
            var hidden = stage.Latest();
            hidden.Fellowship.Hidden = true;
            var state = stage.Board.Fellowship.Status.Q<Label>("fellowship-status-condition");
            var shown = state.text;
            stage.Play(hidden, null, hide);
            yield return stage.Until(() => PresentationGlow.IsLit(state));
            Assert.AreEqual(shown, state.text, "UI-315: the Fellowship's state lights up before it turns.");
            yield return stage.Until(() => state.text != shown);
            Assert.IsTrue(PresentationGlow.IsLit(state), "It turns while lit.");
            Assert.AreEqual("◇", stage.Board.Map.FellowshipMarker.Symbol, "The figure is hidden in place.");
            Assert.IsTrue(FellowshipStateBadge.ShowsHidden(stage.Board.Map.FellowshipMarker.State), "UI-427: its plaque shows ◇.");
            Assert.AreEqual(0, stage.Board.Map.Figures.Moves.Count, "Hiding moves nothing.");
            yield return stage.Released();
            var healed = stage.Next(hidden);
            healed.Fellowship.Corruption = 1;
            var heal = stage.Step(1, 2);
            heal.Facts.FellowshipBefore = Outcome(hidden.Fellowship);
            heal.Facts.FellowshipAfter = Outcome(healed.Fellowship);
            var corruption = Plaque(stage, "corruption");
            var before = corruption.text;
            stage.Play(healed, null, heal);
            yield return stage.Until(() => corruption.text != before && stage.Playback.Holding);
            var row = stage.Board.Fellowship.Root.Q("corruption-track");
            var target = PresentationTargets.Shown(row) ? row : corruption;
            yield return stage.Until(() => PresentationGlow.IsLit(target));
            LayoutTests.Save(stage.Texture, "ui310-healing.png");
            yield return stage.Released();
            Assert.AreEqual(1, stage.Board.View.Fellowship.Corruption);
        }

        [UnityTest]
        public IEnumerator EnteringMordorPutsTheFellowshipOnTheMordorTrack()
        {
            using var stage = Watching();
            stage.Before.Fellowship.RegionId = "minas-morgul";
            stage.Before.Fellowship.Progress = 0;
            yield return stage.Ready();
            var enter = stage.Paid(stage.Step(1), DieFace.Character);
            enter.Facts.FellowshipBefore = Outcome(stage.Before.Fellowship);
            var latest = stage.Latest();
            latest.Fellowship.RegionId = "mordor-track";
            latest.Fellowship.MordorStep = 0;
            enter.Facts.FellowshipAfter = Outcome(latest.Fellowship);
            stage.Play(latest, "minas-morgul", enter);
            var pan = stage.Board.Map.Camera.Pan;
            yield return stage.Until(() => stage.Playback.Holding && stage.Board.Fellowship.Root.Q("mordor-track")?.resolvedStyle.display == DisplayStyle.Flex);
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.Fellowship.Root.Q("mordor-track")));
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan, "The Mordor track is no region to frame: the camera stays.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui310-mordor.png");
            yield return stage.Released();
            Assert.AreEqual(0, stage.Board.View.Fellowship.MordorStep);
        }

        [UnityTest]
        public IEnumerator SkipEndsOnTheLatestTracksWithoutASecondStage()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var steps = Hit(stage, out var latest);
            stage.Play(latest, null, steps);
            yield return stage.Until(() => stage.Board.Hunt.Tile.Root.resolvedStyle.display == DisplayStyle.Flex);
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            Assert.IsFalse(stage.Board.TracksStaged, "Skip shows the latest tracks at once.");
            Assert.AreEqual((2, 4), (stage.Board.View.Fellowship.Progress, stage.Board.View.Fellowship.Corruption));
            Assert.IsEmpty(stage.Lit, "Nothing glows after Skip.");
        }

        [UnityTest]
        public IEnumerator AComplexHuntIsRecordedAsMutedClipsAtBothScales()
        {
            foreach (var (width, scale) in new[] { (1366, 1f), (1920, 1.25f) })
            {
                using var stage = Watching(width: width, scale: scale);
                yield return stage.Ready();
                var steps = Hit(stage, out var latest, casualty: true);
                stage.Play(latest, null, steps);
                var frame = 0;
                for (; stage.Playback.Holding && frame < 120; frame++)
                {
                    LayoutTests.Save(stage.Texture, "ui310-clip-" + width + "-" + frame.ToString("000") + ".png");
                    yield return new WaitForSecondsRealtime(.1f);
                }
                Assert.IsFalse(stage.Playback.Holding, "The Hunt ends within the clip.");
                Assert.Greater(frame, 10);
                Assert.AreEqual((2, 4), (stage.Board.View.Fellowship.Progress, stage.Board.View.Fellowship.Corruption));
            }
        }

        // The Shadow watches a hidden Fellowship at Rivendell: progress 1, corruption 2, Gimli its Guide, four Companions.
        internal static PresentedBoardStage Watching(bool revealed = false, int width = 1366, float scale = 1f, bool reduced = false,
            Side viewer = Side.Shadow)
        {
            var stage = new PresentedBoardStage(width, scale, reduced, viewer);
            var fellowship = stage.Before.Fellowship;
            fellowship.RegionId = "rivendell";
            fellowship.Progress = 1;
            fellowship.Corruption = 2;
            fellowship.Hidden = !revealed;
            fellowship.Guide = Gimli;
            fellowship.CompanionIdentities.AddRange(new[] { "gimli", "legolas", "merry", "pippin" });
            fellowship.CompanionCount = 4;
            return stage;
        }

        // A successful Hunt: two successes, a 2-damage tile taken as corruption with the Ring (progress 1 → 2, corruption
        // 2 → 4); with a casualty, a Companion leaves as well.
        internal static ActionLogStep[] Hit(PresentedBoardStage stage, out PlayerView latest, bool casualty = false)
        {
            var roll = Snapshot("BeforeDraw", 6, 5, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw"))
                .Then(o => o.Progress = 2).Entry(HuntReportKind.Progressed).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, Tile(roll, 2)).Entry(HuntReportKind.DamageDetermined, Tile(roll, 2))
                .Entry(HuntReportKind.Response, Tile(roll, 2), response: HuntResponse.Ring)
                .Then(o => o.Corruption = 4).Entry(HuntReportKind.Corruption);
            if (casualty)
                script.Entry(HuntReportKind.CompanionLeft, character: Merry);
            script.Entry(HuntReportKind.HuntCompleted, Done(Tile(roll, 2)));
            var steps = script.Finish();
            if (casualty)
            {
                script.Latest.Fellowship.CompanionIdentities.Remove("merry");
                script.Latest.Fellowship.CompanionCount = 3;
            }
            latest = script.Latest;
            return steps;
        }

        // UI-462: the Free Peoples' move draws Sméagol's tile — no damage, the tile leaves the game and Sméagol joins the
        // Fellowship as its Guide (lords-smeagol-joins); the Shadow's latest roster names him by identity.
        private static ActionLogStep[] Joins(PresentedBoardStage stage, out PlayerView latest)
        {
            var roll = Snapshot("BeforeDraw", 6, 2);
            var script = new Script(stage).Entry(HuntReportKind.Started, Snapshot("BeforeDraw")).Entry(HuntReportKind.Rolled, roll)
                .Entry(HuntReportKind.TileDrawn, SmeagolTile(roll)).Then(o => o.GuideId = Smeagol)
                .Entry(HuntReportKind.GuideChanged, character: Smeagol).Entry(HuntReportKind.HuntCompleted, Done(SmeagolTile(roll)));
            var steps = script.Finish();
            script.Latest.Fellowship.CompanionIdentities.Add("smeagol");
            script.Latest.Fellowship.CompanionCount += 1;
            latest = script.Latest;
            return steps;
        }

        private static HuntView SmeagolTile(HuntView roll)
        {
            var tile = Copy(roll);
            tile.Stage = "Table";
            tile.TileId = "lords.hunt.smeagol.1";
            tile.Face = new HuntTileFaceView { Pool = "smeagol", Value = "0" };
            return tile;
        }

        // UI-462: the portrait flying into the roster.
        private static VisualElement Joining(PresentedBoardStage stage) => stage.Board.Dissolve.Root.Q(className: FellowshipJoin.PortraitClass);

        private static HuntView Snapshot(string stage, params int[] results) => new HuntView
        {
            SourceId = "fellowship-move", Stage = stage, PoolCount = 12,
            Dice = results.Select((result, index) => new HuntDieView { Id = "d" + index, First = result, Result = result, Success = result >= 5 }).ToList()
        };

        private static HuntView Tile(HuntView roll, int damage, bool reveal = false)
        {
            var tile = Copy(roll);
            tile.Stage = "Table";
            tile.TileId = "base.hunt.standard.2.0.1";
            tile.Damage = damage;
            tile.Reveal = reveal;
            tile.Face = new HuntTileFaceView { Pool = "standard", Value = reveal ? "R" : damage.ToString(), Reveal = reveal };
            return tile;
        }

        private static HuntView Done(HuntView hunt)
        {
            var done = Copy(hunt);
            done.Stage = "Done";
            return done;
        }

        // The board's controls outside the Hunt window (its dice are the roll it shows) and the map.
        private static int Controls(PresentedBoardStage stage) => stage.Board.Root.Query<Button>().ToList()
            .Count(button => !stage.Board.Hunt.Root.Contains(button) && !stage.Board.OpponentDice.Contains(button) && !stage.Board.Map.Contains(button));

        private static Label Plaque(PresentedBoardStage stage, string value) =>
            stage.Board.Fellowship.Status.Q<Label>("fellowship-status-" + value);

        private static VisualElement Die(PresentedBoardStage stage, string id) => stage.Board.Hunt.Root.Q("hunt-die-" + id);

        private static VisualElement Portrait(PresentedBoardStage stage, string identity) =>
            stage.Board.Fellowship.Root.Q("fellowship-portrait-" + identity);

        private static HuntOutcomeView Outcome(FellowshipView fellowship) => new HuntOutcomeView
        {
            RegionId = fellowship.RegionId, Progress = fellowship.Progress, MordorStep = fellowship.MordorStep,
            Corruption = fellowship.Corruption, Hidden = fellowship.Hidden, GuideId = fellowship.Guide
        };

        private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

        // One revision of the Free Peoples' Fellowship move and its Hunt, as the engine records it: the move's step holds
        // the paid die and the whole command's Fellowship change; each Hunt fact step one report entry, with the snapshot
        // of the Hunt window and the Fellowship's state at that entry.
        private sealed class Script
        {
            private readonly PresentedBoardStage stage;
            private readonly ActionLogStep move;
            private readonly List<ActionLogStep> steps = new List<ActionLogStep>();
            private readonly HuntReportView report = new HuntReportView { Id = Chain, StartedRevision = 1 };
            private readonly HuntOutcomeView now;
            public PlayerView Latest { get; }

            public Script(PresentedBoardStage stage)
            {
                this.stage = stage;
                Latest = stage.Latest();
                now = Outcome(stage.Before.Fellowship);
                move = stage.Paid(stage.Step(1), DieFace.Character);
                move.Kind = "ActionDecision";
                move.Context.HuntChainId = Chain;
                move.Facts.FellowshipBefore = Outcome(stage.Before.Fellowship);
                steps.Add(move);
            }

            public Script Entry(HuntReportKind kind, HuntView hunt = null, string character = "", HuntResponse? response = null,
                string source = "fellowship-move")
            {
                var entry = new HuntReportEntryView
                {
                    Revision = 1, HuntId = 1, Kind = kind, SourceId = source, CharacterId = character, Response = response, Hunt = hunt,
                    Outcome = Copy(now)
                };
                report.Entries.Add(entry);
                var step = stage.Step(steps.Count + 1);
                step.Actor = null;
                step.Automatic = true;
                step.Kind = "HuntFact";
                step.Context.HuntChainId = Chain;
                step.Facts.Hunt = new ActionHuntFact { ChainId = Chain, Entry = Copy(entry), Completed = kind == HuntReportKind.HuntCompleted };
                steps.Add(step);
                return this;
            }

            // The Fellowship from the next entry on: the move's progress, damage, a reveal, a new Guide.
            public Script Then(Action<HuntOutcomeView> change)
            {
                change(now);
                return this;
            }

            // The recorded steps; the latest view holds the final Fellowship and the completed report.
            public ActionLogStep[] Finish()
            {
                move.Facts.FellowshipAfter = Copy(now);
                var fellowship = Latest.Fellowship;
                fellowship.RegionId = now.RegionId;
                fellowship.Progress = now.Progress;
                fellowship.MordorStep = now.MordorStep;
                fellowship.Corruption = now.Corruption;
                fellowship.Hidden = now.Hidden;
                fellowship.Guide = now.GuideId;
                report.Completed = true;
                Latest.HuntReport = report;
                Latest.Hunt = null;
                return steps.ToArray();
            }
        }
    }
}
