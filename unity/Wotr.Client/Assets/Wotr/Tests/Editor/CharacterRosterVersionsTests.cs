using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-271: the Lords Mouth of Sauron and Witch-king list each version as its own Characters row.
    public sealed class CharacterRosterVersionsTests
    {
        private const string Mouth = "base.character.mouth-of-sauron";
        private const string LordsMouth = "lords.character.mouth-of-sauron";
        private const string WitchKing = "base.character.witch-king";
        private const string Chief = "lords.character.witch-king";
        private const string Gothmog = "lords.character.gothmog";

        [Test]
        public void AlternativeVersionsGetTheirOwnRowsAndProgressionsDoNot()
        {
            var view = Roster();
            var keys = CharacterRosterPresentation.Rows(view).Select(row => row.FigureId).ToList();
            CollectionAssert.AreEquivalent(new[]
            {
                CharacterRosterVersions.Key(Mouth, Mouth), CharacterRosterVersions.Key(Mouth, LordsMouth),
                WitchKing, CharacterRosterVersions.Key(WitchKing, Chief), Gothmog
            }, keys);
            Assert.IsEmpty(CharacterRosterPresentation.Rows(view).Single(row => row.FigureId == WitchKing).FutureVersions,
                "The Witch-king in play keeps his own row; the other version is a row of its own.");
            Assert.AreEqual(CharacterRosterVersions.Key(WitchKing, Chief), CharacterRosterVersions.RowKey(view, WitchKing, Chief));
            Assert.AreEqual(WitchKing, CharacterRosterVersions.RowKey(view, WitchKing, ""));
            Assert.AreEqual(Mouth, CharacterRosterVersions.Figure(CharacterRosterVersions.Key(Mouth, LordsMouth)));
            Assert.AreEqual(LordsMouth, CharacterRosterVersions.Version(CharacterRosterVersions.Key(Mouth, LordsMouth)));
            var strider = new CharacterRosterView
            {
                FigureId = "base.character.strider", Side = Side.FreePeoples, CurrentDefinitionId = "lords.character.strider",
                Lifecycle = CharacterRosterLifecycle.InFellowship, FutureVersions = { Version("base.character.aragorn", true) }
            };
            Assert.AreEqual(1, CharacterRosterVersions.Split(strider).Count());
        }

        [Test]
        public void EachVersionRowNamesItsVersionAndWhatItsClickDoes()
        {
            var view = Roster();
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var lieutenant = Row(panel, CharacterRosterVersions.Key(Mouth, Mouth));
            var numenorean = Row(panel, CharacterRosterVersions.Key(Mouth, LordsMouth));
            Assert.AreEqual("Lieutenant", lieutenant.Q<Label>("character-roster-detail").text);
            Assert.AreEqual("Númenórean", numenorean.Q<Label>("character-roster-detail").text);
            StringAssert.StartsWith("The Mouth of Sauron, Black Númenórean", CharacterRowTooltip.Plain(numenorean.tooltip));
            var chief = Row(panel, CharacterRosterVersions.Key(WitchKing, Chief));
            Assert.AreEqual("characters-section-NotInPlay", chief.parent.parent.name);
            Assert.AreEqual("characters-section-OnBoard", Row(panel, WitchKing).parent.parent.name);
            Assert.AreEqual("Chief", chief.Q<Label>("character-roster-detail").text);
            var switchRow = CharacterRosterPresentation.Rows(view).Single(row => row.FigureId == CharacterRosterVersions.Key(WitchKing, Chief));
            Assert.AreEqual("Click to turn The Witch-king, the Black Captain into The Witch-king, Chief of the Ringwraiths",
                CharacterRosterPresentation.EntryOffer(switchRow));
            Assert.AreEqual("May replace the version in play", CharacterRosterPresentation.State(switchRow));
        }

        [Test]
        public void AnEntryBelongsToItsVersionRowAndTheFigureRowKeepsItsOtherActions()
        {
            var view = Roster();
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var enterLordsMouth = Entry(LordsMouth);
            var turnIntoChief = Entry(Chief);
            var move = new ComponentAction { Kind = CommandKind.MoveShadowCharacters, OwnerId = WitchKing, Enabled = true };
            panel.Actions.Apply(view, new TurnDraft(), new[] { enterLordsMouth, turnIntoChief, move }, true);
            Assert.AreSame(enterLordsMouth, panel.Actions.Entry(CharacterRosterVersions.Key(Mouth, LordsMouth)));
            Assert.IsNull(panel.Actions.Entry(CharacterRosterVersions.Key(Mouth, Mouth)));
            Assert.AreSame(turnIntoChief, panel.Actions.Entry(CharacterRosterVersions.Key(WitchKing, Chief)));
            Assert.IsNull(panel.Actions.Entry(WitchKing), "The Witch-king's own row moves him; it does not change his version.");
            Assert.IsTrue(panel.Actions.Available(WitchKing));
            Assert.IsTrue(panel.Actions.Available(CharacterRosterVersions.Key(WitchKing, Chief)));
            Assert.IsFalse(panel.Actions.Available(CharacterRosterVersions.Key(Mouth, Mouth)));
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            draft.Change(payload => payload.CharacterId = Chief);
            Assert.AreEqual(CharacterRosterVersions.Key(WitchKing, Chief), CharacterRosterActions.EnteringFigure(view, draft));
        }

        private static Button Row(CharacterRosterPanel panel, string key)
        {
            var row = panel.Root.Q<Button>("character-roster-" + key);
            Assert.IsNotNull(row, key);
            return row;
        }

        private static ComponentAction Entry(string version) => new ComponentAction
        {
            Kind = CommandKind.EnterCharacter, OwnerId = version, Enabled = true,
            Character = new CharacterActionOption { CharacterId = version }
        };

        private static CharacterEntryVersionView Version(string id, bool legal) => new CharacterEntryVersionView
        {
            DefinitionId = id, Title = CharacterLibrary.Name(id), Mode = CharacterEntryMode.FirstEntry,
            Entry = new ActionAvailabilityView { CanAct = legal, Title = CharacterLibrary.Name(id), Cost = "Muster" }
        };

        private static PlayerView Roster() => new PlayerView
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
                },
                new CharacterRosterView
                {
                    FigureId = Gothmog, Identity = "gothmog", Side = Side.Shadow, Title = "Gothmog",
                    Lifecycle = CharacterRosterLifecycle.NotEntered, FutureVersions = { Version(Gothmog, false) }
                }
            }
        };
    }
}
