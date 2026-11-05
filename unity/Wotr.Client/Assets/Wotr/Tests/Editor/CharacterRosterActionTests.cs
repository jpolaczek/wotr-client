using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class CharacterRosterActionTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string White = "base.character.gandalf-white";

        [Test]
        public void VersionOwnershipUsesOneFigureAndDoesNotClaimUnownedOrEmptySources()
        {
            var view = Entry();
            view.OwnCharacterRoster.Add(new CharacterRosterView { FigureId = "not-entered", Side = view.You });
            Assert.AreEqual(Grey, CharacterRosterActions.Figure(view, White));
            Assert.IsEmpty(CharacterRosterActions.Figure(view, ""));
            Assert.IsEmpty(CharacterRosterActions.Figure(view, "enemy"));
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "west");
            var actions = ComponentActions.For(view, draft).ToArray();
            Assert.AreEqual(1, CharacterRosterActions.For(view, Grey, White, actions).Count());
            Assert.IsEmpty(CharacterRosterActions.For(view, Grey, Grey, actions));
            Assert.IsEmpty(CharacterRosterActions.For(view, "enemy", White, actions));
        }

        [Test]
        public void EntryRetainsTheExplicitWestAndRejectsAStaleCostWithoutReplacingIt()
        {
            var view = Entry();
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "west");
            var action = ComponentActions.For(view, draft).Single();
            Assert.IsTrue(ComponentActions.Select(view, draft, action));
            Assert.AreEqual("west", draft.Payload.DieId);
            Assert.AreEqual(White, draft.Payload.CharacterId);
            Assert.IsEmpty(draft.Payload.RegionId, "The player must choose one of two entry regions.");
            CharacterDraft.Region(draft, view.Options[0], "fangorn");
            Assert.IsTrue(draft.Ready(view.Options[0]));
            var stale = new TurnDraft();
            stale.Change(p => p.DieId = "west");
            view.Options[0].CharacterActions[0].Dice.Clear();
            view.Options[0].CharacterActions[0].Dice.Add("other-west");
            Assert.IsFalse(ComponentActions.Select(view, stale, action));
            Assert.IsNull(stale.Kind);
            Assert.AreEqual("west", stale.Payload.DieId);
        }

        [Test]
        public void CouncilEntryKeepsItsExactTokenAndWaitsForAnExplicitDie()
        {
            var view = Entry();
            var choice = view.Options[0].CharacterActions[0];
            choice.CouncilTokens.Add(ExpansionDraft.CouncilTravel);
            choice.RequiresCouncilToken = true;
            var draft = new TurnDraft();
            draft.Change(p => p.CouncilTokenId = ExpansionDraft.CouncilTravel);
            var action = ComponentActions.For(view, draft).Single();
            Assert.IsTrue(ComponentActions.Select(view, draft, action));
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilTravel, draft.Payload.CouncilTokenId);
            CharacterDraft.Region(draft, view.Options[0], "fangorn");
            Assert.IsFalse(draft.Ready(view.Options[0]));
            draft.Change(p => p.DieId = "west");
            Assert.IsTrue(draft.Ready(view.Options[0]));
        }

        [Test]
        public void CouncilTravelUsesItsTokenAndReplacingTheUnsentOriginDoesNotCreateADie()
        {
            var view = Entry();
            view.You = Side.Shadow;
            view.DecisionOwner = Side.Shadow;
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.UseCouncilToken, Enabled = true, EligibleIds = { ExpansionDraft.CouncilTravel },
                Travel =
                {
                    new TravelOption { Id = "first", Origin = "moria", Range = 2, Targets = { "moria", "hollin" } },
                    new TravelOption { Id = "second", Origin = "dale", Range = 2, Targets = { "dale", "erebor" } }
                }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseCouncilToken);
            draft.Change(p => p.AbilityId = ExpansionDraft.CouncilTravel);
            // UI-379: the token's travel plans its groups through CouncilTravel; the general travel path stays out of it.
            Assert.IsNull(TravelActions.Option(view, draft, new ActionDieContext()));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, "first"));
            Assert.IsTrue(CouncilTravel.Move(view, draft, "hollin", BoardPresentation.Load()));
            Assert.IsTrue(CouncilTravel.Choose(view, draft, "second"));
            CollectionAssert.AreEqual(new[] { "second" }, CouncilTravel.Chosen(draft));
            Assert.AreEqual("hollin", draft.Payload.CompanionMoves.Single().Route.Last());
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual(ExpansionDraft.CouncilTravel, draft.Payload.AbilityId);
            Assert.IsFalse(draft.Payload.Stepwise);
        }

        // UI-108/UI-142: a character enters from its row by a click; the entry opens no window and has no button.
        [Test]
        public void AnEntryComesFromItsRowAndNeverHostsTheInspector()
        {
            var view = Entry();
            var panel = new CharacterRosterPanel(new VisualElement(), BoardPresentation.Load(), _ => { });
            panel.Apply(view);
            var tray = new VisualElement();
            var dock = new VisualElement();
            dock.Add(tray);
            panel.Attach(tray, dock);
            var chosen = new TurnDraft();
            chosen.Change(p => p.DieId = "west");
            panel.Actions.Apply(view, chosen, ComponentActions.For(view, chosen), true);
            var entry = panel.Actions.Entry(Grey);
            Assert.IsNotNull(entry, "Gandalf's row brings the White into play for the chosen die.");
            Assert.AreEqual(White, entry.Character.CharacterId);
            panel.Show(Grey, true);
            panel.Inspector.SelectVersion(White);
            Assert.IsNull(panel.Inspector.Actions.Q<Button>("component-" + entry.Key), "The inspector has no entry button.");
            var idle = new TurnDraft();
            panel.Actions.Apply(view, idle, ComponentActions.For(view, idle), true);
            Assert.IsNull(panel.Actions.Entry(Grey), "Without the die the row cannot enter.");

            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter, "west");
            CharacterDraft.Select(draft, view, view.Options[0].CharacterActions[0]);
            panel.Close();
            panel.Actions.Apply(view, draft, ComponentActions.For(view, draft), true);
            Assert.IsFalse(CharacterRosterActions.OwnsDecision(view, draft));
            Assert.AreSame(dock, tray.parent, "The prepared entry stays in the decision dock.");
            Assert.AreEqual(DisplayStyle.None, panel.Inspector.Root.style.display.value, "Preparing an entry opens no window.");
            // UI-243: the chosen row stays chosen and asks for a region while the regions glow on the map.
            Assert.AreEqual(Grey, panel.Actions.EnteringFigure);
            var row = panel.Root.Q<Button>("character-roster-" + Grey);
            Assert.IsTrue(row.ClassListContains("entry-chosen"));
            Assert.IsFalse(row.ClassListContains(ChoiceGlow.ClassName), "A chosen row no longer pulses as an offer.");
            Assert.AreEqual("Choose a highlighted region", row.Q<Label>("character-roster-detail").text);
            panel.Actions.Apply(view, idle, ComponentActions.For(view, idle), true);
            Assert.IsFalse(row.ClassListContains("entry-chosen"), "Back clears the choice.");
            Assert.AreNotEqual("Choose a highlighted region", row.Q<Label>("character-roster-detail").text);
        }

        [Test]
        public void ReactionLinksDoNotExposeEnemyOptionsOrTakeOverTheirDecision()
        {
            var view = Entry();
            view.Decision.Kind = DecisionKind.Reaction;
            view.Decision.SourceId = Grey;
            Assert.IsTrue(CharacterRosterActions.HasReaction(view, Grey));
            Assert.IsFalse(CharacterRosterActions.OwnsDecision(view, new TurnDraft()));
            view.OwnCharacterRoster[0].CurrentDefinitionId = White;
            view.OwnCharacterRoster[0].FutureVersions.Clear();
            view.Decision.SourceId = "battle";
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                EffectChoices = { new EffectChoiceView { Id = "character.white-rider", SourceId = White } }
            });
            Assert.IsTrue(CharacterRosterActions.HasReaction(view, Grey),
                "White Rider belongs to the stable Gandalf figure after its version changes.");
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(CharacterRosterActions.HasReaction(view, Grey));
        }

        // UI-423 (owner's playtest pattern, like UI-390): a response made on its own object has no window for Open decision to
        // open — Galadriel's Nenya at its socket, the Chief's Spear of Terror at the Character deck and He Sees, He Knows at
        // his figure — so the character's window links none; a response of his made in a window still links.
        [Test]
        public void AResponseMadeOnItsObjectIsNotLinkedFromItsCharacter()
        {
            const string galadriel = "lords.character.galadriel";
            var view = Reaction(Side.FreePeoples, galadriel, NamedRings.NenyaReaction);
            Assert.IsFalse(CharacterRosterActions.HasReaction(view, galadriel), "Nenya is used at its socket.");

            const string chief = "lords.character.witch-king";
            view = Reaction(Side.Shadow, chief, ChiefReactionObjects.Spear, ChiefReactionObjects.Follow);
            Assert.IsFalse(CharacterRosterActions.HasReaction(view, chief), "Spear of Terror and He Sees, He Knows stand on their objects.");
            view.Options[0].EligibleIds.Add("lords.chief.window-response");
            view.Options[0].EffectChoices.Add(new EffectChoiceView { Id = "lords.chief.window-response", SourceId = chief });
            Assert.IsTrue(CharacterRosterActions.HasReaction(view, chief), "A response made in a window is still linked.");
        }

        private static PlayerView Reaction(Side side, string character, params string[] responses)
        {
            var option = new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true };
            foreach (var response in responses)
            {
                option.EligibleIds.Add(response);
                option.EffectChoices.Add(new EffectChoiceView { Id = response, SourceId = character });
            }
            return new PlayerView
            {
                MatchId = "roster-reactions", You = side, DecisionOwner = side,
                Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction, SourceId = "base.hunt.eye" },
                OwnCharacterRoster = { new CharacterRosterView { FigureId = character, CurrentDefinitionId = character, Side = side } },
                Options = { option }
            };
        }

        private static PlayerView Entry() => new PlayerView
        {
            MatchId = "roster-actions", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Dice = { new DieView { Id = "west", Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Location = DieLocation.Available } },
            OwnCharacterRoster =
            {
                new CharacterRosterView
                {
                    FigureId = Grey, Identity = "gandalf", CurrentDefinitionId = Grey,
                    Lifecycle = CharacterRosterLifecycle.EliminatedWithReplacement, Side = Side.FreePeoples,
                    FutureVersions = { new CharacterEntryVersionView { DefinitionId = White, Title = "Gandalf the White" } }
                }
            },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.EnterCharacter, EligibleDice = { "west" },
                    CharacterActions = { new CharacterActionOption { CharacterId = White, Dice = { "west" }, Targets = { "fangorn", "lorien" }, TargetCount = 1 } }
                }
            }
        };
    }
}
