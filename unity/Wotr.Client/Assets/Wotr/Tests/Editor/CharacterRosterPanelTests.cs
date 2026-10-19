using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class CharacterRosterPanelTests
    {
        [Test]
        public void RealRosterKeepsWhiteReturnOutOfRemovedAndDoesNotDuplicateFellowship()
        {
            var view = Read("CharacterRosterWire.json");
            var data = BoardPresentation.Load();
            var panel = new CharacterRosterPanel(new VisualElement(), data, _ => Assert.Fail("Inspection moved the camera"));
            panel.Apply(view);
            var white = panel.Root.Q<Button>("character-roster-base.character.gandalf-grey");
            Assert.IsNotNull(white);
            StringAssert.Contains("White", white.Q<Label>("character-roster-title").text);
            Assert.AreEqual("characters-section-NotInPlay", white.parent.parent.name);
            Assert.IsNull(panel.Root.Q<Button>("character-roster-base.character.gandalf-white"));
            Assert.IsFalse(CharacterRosterPresentation.Rows(view).Any(r => r.Identity is "gollum" or "smeagol"));
            panel.Show("base.character.gandalf-grey", true);
            Assert.IsNull(panel.Inspector.Root.Q<Button>("enter-character"));
            Assert.AreEqual(DisplayStyle.None, panel.Inspector.Root.Q<Button>("locate-character").style.display.value);
        }

        [Test]
        public void TransformRefreshKeepsTheSameRowInspectorAndPinnedChoice()
        {
            var view = Read("CharacterRosterWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            const string id = "base.character.gandalf-grey";
            var button = panel.Root.Q<Button>("character-roster-" + id);
            panel.Show(id, true);
            var row = view.OwnCharacterRoster.Single(r => r.FigureId == id);
            row.CurrentDefinitionId = "base.character.gandalf-white";
            row.Title = "Gandalf the White";
            row.Lifecycle = CharacterRosterLifecycle.OnBoard;
            row.RegionId = "fangorn";
            row.FutureVersions.Clear();
            panel.Apply(view);
            Assert.AreSame(button, panel.Root.Q<Button>("character-roster-" + id));
            Assert.AreEqual("characters-section-OnBoard", button.parent.parent.name);
            Assert.IsTrue(panel.InspectorPinned);
            Assert.AreEqual(id, panel.Inspector.FigureId);
            Assert.AreEqual(DisplayStyle.Flex, panel.Inspector.Root.Q<Button>("locate-character").style.display.value);
        }

        [Test]
        public void FateStatisticsSeparateMovementFromCombatAndHuntLevel()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            var text = inspector.Root.Q("character-statistics").tooltip;
            StringAssert.Contains("Level 1", text);
            StringAssert.Contains("Move 2", text);
            StringAssert.Contains("Combat level 1", text);
            Assert.IsFalse(text.Contains("Level 2"));
        }

        [Test]
        public void CardStatisticsAreSymbolsThatExplainThemselves()
        {
            var nazgul = new CharacterRosterView { Level = 3, MovementLevel = 3, CombatLevel = 3, Leadership = 2,
                UnlimitedMovement = true, NationIcon = "sauron", CurrentDefinitionId = "base.character.witch-king" };
            var facts = CharacterCardFacts.For(nazgul, nazgul.CurrentDefinitionId, true);
            CollectionAssert.AreEqual(new[] { "level", "flight", "combat", "leadership", "nation" }, facts.Select(f => f.Key));
            Assert.AreEqual("∞", facts.Single(f => f.Key == "flight").Value);
            Assert.AreEqual("sauron", facts.Single(f => f.Key == "nation").Nation);
            Assert.IsTrue(facts.All(f => f.Tooltip.Length > 0));
            StringAssert.Contains("Move Flight", CharacterCardFacts.Summary(facts));
            var white = CharacterCardFacts.For(new CharacterRosterView(), "base.character.gandalf-white", false);
            Assert.AreEqual("Nation All Free Peoples", white.Single(f => f.Key == "nation").Label);
            Assert.AreEqual("removed", CharacterCardFacts.Status(new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.Eliminated }, "").Key);
            var onBoard = CharacterCardFacts.Status(new CharacterRosterView { Lifecycle = CharacterRosterLifecycle.OnBoard, Garrison = true }, "Erebor");
            Assert.AreEqual(("on-board", "Erebor · Garrison", "Erebor"), (onBoard.Key, onBoard.Text, onBoard.Short));
        }

        // UI-240: an enterable row says what its click does: a change into the next version or an entry.
        [Test]
        public void AnEnterableRowNamesTheChangeItsClickMakes()
        {
            var strider = new CharacterRosterView
            {
                FigureId = "base.character.strider", CurrentDefinitionId = "base.character.strider", Lifecycle = CharacterRosterLifecycle.OnBoard,
                FutureVersions = { new CharacterEntryVersionView { DefinitionId = "base.character.aragorn", Entry = new ActionAvailabilityView { CanAct = true } } }
            };
            StringAssert.StartsWith("Click to turn Strider into Aragorn", CharacterRosterPresentation.EntryOffer(strider));
            var white = new CharacterRosterView
            {
                FigureId = "base.character.gandalf-white", Lifecycle = CharacterRosterLifecycle.NotEntered,
                FutureVersions = { new CharacterEntryVersionView { DefinitionId = "base.character.gandalf-white", Entry = new ActionAvailabilityView { CanAct = true } } }
            };
            Assert.AreEqual("Click to bring Gandalf the White into play", CharacterRosterPresentation.EntryOffer(white));
            Assert.AreEqual("Click to separate Merry from the Fellowship",
                FellowshipRoster.Offer(new PlayerView(), "base.character.merry", CommandKind.SeparateCompanions));
            Assert.AreEqual("", FellowshipRoster.Offer(new PlayerView(), "base.character.merry", null));
        }

        [Test]
        public void InspectorShowsStatusStatisticsAndAbilityChipsWithoutUnrolledText()
        {
            var view = Read("CharacterRosterWire.json");
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "base.character.gimli"));
            Assert.AreEqual("Gimli", inspector.Root.Q<Label>(className: "title").text);
            Assert.AreEqual("Fate", inspector.Root.Q<Label>("character-edition").text);
            Assert.IsTrue(inspector.Root.Q("character-status-icon").ClassListContains("character-status-on-board"));
            var facts = inspector.Root.Query(className: CharacterFactStrip.ItemClass).ToList()
                .Where(item => item.style.display.value != DisplayStyle.None).ToList();
            Assert.GreaterOrEqual(facts.Count, 4);
            Assert.IsTrue(facts.All(item => item.tooltip.Length > 0));
            var chips = inspector.Root.Query<Button>(className: CharacterAbilityChips.ChipClass).ToList()
                .Where(chip => chip.style.display.value != DisplayStyle.None).ToList();
            Assert.IsNotEmpty(chips);
            Assert.IsTrue(chips.All(chip => chip.tooltip.StartsWith(chip.Q<Label>(className: CharacterAbilityChips.TitleClass).text + "\n")));
            Assert.AreEqual(DisplayStyle.None, inspector.Root.Q<Label>("character-ability-text").style.display.value,
                "The full ability text unrolls only after a click.");
        }

        [Test]
        public void RosterRowCarriesTheLevelAndAStatusSymbol()
        {
            var view = Read("CharacterRosterWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var gimli = panel.Root.Q<Button>("character-roster-base.character.gimli");
            Assert.IsTrue(gimli.Q("character-roster-status").ClassListContains("character-status-on-board"));
            Assert.AreEqual(view.OwnCharacterRoster.Single(r => r.FigureId == "base.character.gimli").Level.ToString(),
                gimli.Q<Label>("character-roster-level").text);
            StringAssert.Contains("Level ", gimli.tooltip);
        }

        [Test]
        public void EnemyPublicInspectionDoesNotReadOwnCandidatesOrCreateForeignControls()
        {
            var view = Read("CharacterRosterShadowWire.json");
            view.Characters.Add(new CharacterView { Id = "public-gimli", DefinitionId = "fate.character.gimli-dwarven-lord",
                Side = Side.FreePeoples, Location = "iron-hills", Level = 1, Leadership = 1 });
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "public-gimli"));
            StringAssert.Contains("Move 2", inspector.Root.Q("character-statistics").tooltip);
            Assert.IsEmpty(inspector.Actions.Children());
            Assert.AreEqual(DisplayStyle.None, inspector.Root.Q<Label>("character-entry").style.display.value);
            Assert.IsFalse(CharacterRosterPresentation.Rows(view).Any(r => r.FigureId == "public-gimli"));
        }

        [Test]
        public void EmptyOlderRosterIsHonestAndRemovedStartsCollapsed()
        {
            var view = new PlayerView();
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            Assert.AreEqual(DisplayStyle.Flex, panel.Root.Q<Label>("characters-empty").style.display.value);
            Assert.IsFalse(panel.Root.Q<Foldout>("characters-removed").value);
            Assert.IsNull(CharacterRosterPresentation.Inspect(view, "absent"));
            Assert.AreEqual(CharacterRosterSection.NotInPlay, CharacterRosterPresentation.Section(new CharacterRosterView()));
        }

        [Test]
        public void ShadowFullRosterShowsEightNamedFiguresAndServerEntryConditions()
        {
            var view = Read("CharacterRosterShadowWire.json");
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var rows = panel.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("character-roster-")).ToList();
            // UI-271: the Mouth of Sauron and the Witch-king list their base and Lords versions as two rows each.
            Assert.AreEqual(10, rows.Count);
            Assert.AreEqual(8, rows.Select(b => CharacterRosterVersions.Figure(b.name.Substring("character-roster-".Length))).Distinct().Count());
            var future = view.OwnCharacterRoster.Single(r => r.Identity == "black-serpent").FutureVersions.Single();
            StringAssert.Contains(future.Entry.Source, CharacterRosterPresentation.Conditions(future.Entry));
            StringAssert.Contains(future.Entry.Cost, CharacterRosterPresentation.Conditions(future.Entry));
            Assert.IsTrue(future.CouncilEntry.CanAct);
        }

        [Test]
        public void EveryPublicCharacterAbilityHasReadableTextAndItsReviewedSource()
        {
            foreach (var catalog in new[] { "base-characters", "lords-characters", "kings-characters", "fate-characters" })
            {
                var cards = JsonConvert.DeserializeObject<CharacterText[]>(Resources.Load<TextAsset>(catalog).text);
                foreach (var card in cards)
                {
                    var descriptions = CharacterAbilityLibrary.For(card.Id);
                    CollectionAssert.AreEquivalent(card.Abilities, descriptions.Select(a => a.Id), card.Id);
                    foreach (var ability in descriptions)
                    {
                        Assert.Greater(ability.Text.Length, 30, card.Id + " / " + ability.Id);
                        Assert.IsNotEmpty(ability.Title);
                        Assert.IsNotEmpty(ability.Source);
                        Assert.IsFalse(ability.Source.Contains(".json"), "Repository paths do not belong in the player's tooltip.");
                    }
                }
            }
            Assert.IsEmpty(CharacterAbilityLibrary.For("unprovided-version"));
        }

        [Test]
        public void PublicAbilityInspectionExplainsParticipationAndTheFiveDiceLimit()
        {
            var view = Read("CharacterRosterShadowWire.json");
            view.Characters.Add(new CharacterView { Id = "public-gimli", DefinitionId = "base.character.gimli",
                Side = Side.FreePeoples, Location = "iron-hills", Level = 2, Leadership = 1 });
            var inspector = new CharacterInspector(BoardPresentation.Load(), _ => { }, () => { }, () => { });
            Assert.IsTrue(inspector.Apply(view, "public-gimli"));
            var ability = inspector.Root.Q<Button>("character-ability-combat-strength-plus-one");
            StringAssert.Contains("Captain of the West", ability.Q<Label>(className: CharacterAbilityChips.TitleClass).text);
            StringAssert.Contains("participat", ability.tooltip.ToLowerInvariant());
            StringAssert.Contains("5 combat dice", ability.tooltip.ToLowerInvariant());
            Assert.IsTrue(ability.focusable);
        }

        private static PlayerView Read(string file) => JsonConvert.DeserializeObject<PlayerView>(
            File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor", file)));
    }
}
