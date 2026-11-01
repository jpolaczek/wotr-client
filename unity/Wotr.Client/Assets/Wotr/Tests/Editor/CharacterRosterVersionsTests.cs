using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-438 (owner, 2026-11-01): "two different versions of the Mouth are shown separately, unlike Aragorn, who has one
    // marker and switches inside; make every character behave like Aragorn/Strider". Every character with more than one
    // version in the content (base, Lords, Kings, Fate, Council) has one row in every section of the Characters panel; the
    // row shows one version and its window switches between them. Entries and their payloads stay the versions'.
    public sealed class CharacterRosterVersionsTests
    {
        private const string Mouth = "base.character.mouth-of-sauron";
        private const string LordsMouth = "lords.character.mouth-of-sauron";
        private const string WitchKing = "base.character.witch-king";
        private const string Chief = "lords.character.witch-king";
        private static readonly string[] Catalogs = { "base-characters", "lords-characters", "kings-characters", "fate-characters" };

        [Test]
        public void EveryCharacterOfTheContentHasOneRowInEverySection()
        {
            var versions = ContentVersions();
            Assert.That(versions.Values.Count(list => list.Count > 1), Is.GreaterThanOrEqualTo(9), "The content has characters with several versions.");
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                foreach (var lifecycle in new[] { CharacterRosterLifecycle.NotEntered, CharacterRosterLifecycle.OnBoard,
                             CharacterRosterLifecycle.InFellowship, CharacterRosterLifecycle.Eliminated })
                {
                    // A character with one version is never listed in the Fellowship (it has nothing to change into).
                    var identities = versions.Where(entry => entry.Key.Side == side &&
                        (lifecycle != CharacterRosterLifecycle.InFellowship || entry.Value.Count > 1)).ToList();
                    var view = Roster(side, lifecycle, identities);
                    var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
                    panel.Apply(view);
                    var label = side + " " + lifecycle;
                    var rows = panel.Root.Query<Button>(className: "character-roster-row").ToList();
                    var shown = rows.Select(row => view.OwnCharacterRoster.Single(c => "character-roster-" + c.FigureId == row.name).Identity).ToList();
                    CollectionAssert.AllItemsAreUnique(shown, label + ": no character has two rows");
                    CollectionAssert.AreEquivalent(identities.Select(entry => entry.Key.Identity), shown, label + ": every character has its row");
                    foreach (var entry in identities.Where(entry => entry.Value.Count > 1))
                        OneWindowWithEveryVersion(view, entry.Value, label);
                }
        }

        // Without an entry offered the Mouth's one row shows the first version; with one version's entry offered it shows
        // that version and its click sends that version's entry, as the version's own row did (UI-271).
        [Test]
        public void TheMouthsOneRowShowsTheVersionItsEntryBringsIn()
        {
            var view = Shadow();
            var panel = Panel(view);
            var row = Row(panel, Mouth);
            Assert.AreEqual("Lieutenant", Detail(row));
            var enterLordsMouth = Entry(LordsMouth);
            panel.Actions.Apply(view, new TurnDraft(), new[] { enterLordsMouth, Entry(Mouth, false) }, true);
            Assert.AreSame(enterLordsMouth, panel.Actions.Entry(Mouth), "The Mouth's row sends the Lords Mouth's entry.");
            Assert.AreEqual(Mouth, CharacterRosterActions.Row(view, enterLordsMouth));
            Assert.IsTrue(row.ClassListContains(CharacterRosterPanel.EntryClass));
            Assert.AreEqual("Númenórean", Detail(row));
            StringAssert.StartsWith("Click to bring The Mouth of Sauron, Black Númenórean into play", CharacterRowTooltip.Plain(row.tooltip));
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            draft.Change(payload => payload.CharacterId = LordsMouth);
            Assert.AreEqual(Mouth, CharacterRosterActions.EnteringFigure(view, draft), "The entering version waits on the Mouth's row.");
        }

        // With both versions' entries offered by one die the row opens the window, where the other version's tab glows and the
        // shown version's Enter sends it.
        [Test]
        public void TwoVersionsThatMayEnterAreChosenInTheWindow()
        {
            var view = Shadow();
            var panel = Panel(view);
            var row = Row(panel, Mouth);
            panel.Actions.Apply(view, new TurnDraft(), new[] { Entry(Mouth), Entry(LordsMouth) }, true);
            Assert.IsNull(panel.Actions.Entry(Mouth), "No single entry: the click opens the window.");
            Assert.IsFalse(row.ClassListContains(CharacterRosterPanel.EntryClass));
            Assert.IsTrue(row.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith(CharacterRosterPresentation.ChooseVersionOffer, CharacterRowTooltip.Plain(row.tooltip));
            panel.Show(Mouth, true);
            var inspector = panel.Inspector;
            Assert.AreEqual(Mouth, inspector.DefinitionId);
            Assert.IsTrue(inspector.Root.Q<Button>("character-version-" + LordsMouth).ClassListContains(ChoiceGlow.ClassName));
            Assert.IsFalse(inspector.Root.Q<Button>("character-version-" + Mouth).ClassListContains(ChoiceGlow.ClassName));
            Assert.IsNotNull(inspector.Actions.Query<Button>().ToList().SingleOrDefault(b => b.text == "Enter"), "The shown version's Enter.");
        }

        // The Black Captain in play: his one row turns him into the Chief in place; his window has both versions.
        [Test]
        public void TheWitchKingsOneRowTurnsHimInPlace()
        {
            var view = Shadow();
            var panel = Panel(view);
            var turnIntoChief = Entry(Chief);
            var move = new ComponentAction { Kind = CommandKind.MoveShadowCharacters, OwnerId = WitchKing, Enabled = true };
            panel.Actions.Apply(view, new TurnDraft(), new[] { turnIntoChief, move }, true);
            var row = Row(panel, WitchKing);
            Assert.AreEqual("characters-section-OnBoard", row.parent.parent.name);
            Assert.AreSame(turnIntoChief, panel.Actions.Entry(WitchKing));
            Assert.IsTrue(panel.Actions.Available(WitchKing));
            Assert.AreEqual("Click to turn The Witch-king, the Black Captain into The Witch-king, Chief of the Ringwraiths",
                CharacterRosterPresentation.EntryOffer(view.OwnCharacterRoster.Single(r => r.FigureId == WitchKing), Chief));
            panel.Show(WitchKing, true);
            Assert.AreEqual("Black Captain", panel.Inspector.Root.Q<Button>("character-version-" + WitchKing).text);
            Assert.AreEqual("Chief", panel.Inspector.Root.Q<Button>("character-version-" + Chief).text);
            Assert.IsNull(panel.Root.Q<Button>("character-roster-" + WitchKing + "@" + Chief), "No row of the version alone.");
        }

        private static void OneWindowWithEveryVersion(PlayerView view, IReadOnlyList<string> versions, string label)
        {
            var row = view.OwnCharacterRoster.Single(r => versions.Contains(r.CurrentDefinitionId) || r.FutureVersions.Any(v => versions.Contains(v.DefinitionId)));
            var shown = new[] { row.CurrentDefinitionId }.Concat(row.FutureVersions.Select(v => v.DefinitionId)).Where(id => id.Length > 0).ToList();
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, row.FigureId), label + " " + row.FigureId);
            var strip = inspector.Root.Q(className: "character-version-tabs");
            var tabs = strip.style.display.value == DisplayStyle.None ? new List<Button>() : strip.Query<Button>(className: "character-version-tab").ToList();
            Assert.AreEqual(shown.Count > 1 ? shown.Count : 0, tabs.Count, label + " " + row.FigureId + ": one tab per version");
            CollectionAssert.AllItemsAreUnique(tabs.Select(tab => tab.text), label + " " + row.FigureId + ": the tabs tell the versions apart");
        }

        // Each side's characters by identity, with every version the content holds for that side (as the server groups them).
        private static Dictionary<(Side Side, string Identity), List<string>> ContentVersions()
        {
            var versions = new Dictionary<(Side, string), List<string>>();
            foreach (var catalog in Catalogs)
                foreach (var card in JArray.Parse(Resources.Load<TextAsset>(catalog).text))
                {
                    var role = (string)card["role"];
                    var identity = (string)card["identity"];
                    if (role is not ("Companion" or "Minion") || identity is "gollum" or "smeagol")
                        continue;
                    var key = ((string)card["side"] == "Shadow" ? Side.Shadow : Side.FreePeoples, identity);
                    if (!versions.TryGetValue(key, out var list))
                        versions.Add(key, list = new List<string>());
                    list.Add((string)card["id"]);
                }
            return versions;
        }

        // One server row per character: not entered, every version may enter; in play or in the Fellowship, the first
        // version is current and the others may replace it; removed, no version is left.
        private static PlayerView Roster(Side side, CharacterRosterLifecycle lifecycle, IEnumerable<KeyValuePair<(Side Side, string Identity), List<string>>> identities)
        {
            var view = new PlayerView { You = side, DecisionOwner = side };
            foreach (var entry in identities)
            {
                var ids = entry.Value;
                var current = lifecycle == CharacterRosterLifecycle.NotEntered ? "" : ids[0];
                var row = new CharacterRosterView
                {
                    FigureId = ids[0], Identity = entry.Key.Identity, Side = side, CurrentDefinitionId = current,
                    Title = CharacterLibrary.Name(ids[0]), Lifecycle = lifecycle,
                    RegionId = lifecycle == CharacterRosterLifecycle.OnBoard ? "minas-tirith" : ""
                };
                if (lifecycle != CharacterRosterLifecycle.Eliminated)
                    row.FutureVersions.AddRange(ids.Where(id => id != current).Select(id => Version(id, false)));
                view.OwnCharacterRoster.Add(row);
            }
            return view;
        }

        private static CharacterRosterPanel Panel(PlayerView view)
        {
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Enterable = id => panel.Actions.Entry(id) != null;
            panel.Apply(view);
            return panel;
        }

        private static Button Row(CharacterRosterPanel panel, string figure)
        {
            var row = panel.Root.Q<Button>("character-roster-" + figure);
            Assert.IsNotNull(row, figure);
            return row;
        }

        private static string Detail(Button row) => row.Q<Label>("character-roster-detail").text;

        private static ComponentAction Entry(string version, bool enabled = true) => new ComponentAction
        {
            Key = "character-" + version + "-", Kind = CommandKind.EnterCharacter, OwnerId = version, Enabled = enabled,
            Character = new CharacterActionOption { CharacterId = version, Title = CharacterLibrary.Name(version) }
        };

        private static CharacterEntryVersionView Version(string id, bool legal) => new CharacterEntryVersionView
        {
            DefinitionId = id, Title = CharacterLibrary.Name(id), Mode = CharacterEntryMode.FirstEntry,
            Entry = new ActionAvailabilityView { CanAct = legal, Title = CharacterLibrary.Name(id), Cost = "Muster" }
        };

        private static PlayerView Shadow() => new PlayerView
        {
            You = Side.Shadow, DecisionOwner = Side.Shadow,
            OwnCharacterRoster =
            {
                new CharacterRosterView
                {
                    FigureId = Mouth, Identity = "mouth-of-sauron", Side = Side.Shadow, Title = "The Mouth of Sauron",
                    Lifecycle = CharacterRosterLifecycle.NotEntered, FutureVersions = { Version(Mouth, true), Version(LordsMouth, true) }
                },
                new CharacterRosterView
                {
                    FigureId = WitchKing, Identity = "witch-king", Side = Side.Shadow, Title = "The Witch-king",
                    CurrentDefinitionId = WitchKing, Lifecycle = CharacterRosterLifecycle.OnBoard, RegionId = "minas-morgul", Level = 3,
                    FutureVersions = { Version(Chief, true) }
                }
            }
        };
    }
}
