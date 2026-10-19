using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-266: an expansion's decisions are made on their objects. The watch records every opening of the generic
    // decision window on either client and checks that the decision in front of a player has an object host.
    internal sealed class GenericWindowWatch : IDisposable
    {
        private readonly List<(MountedMatchClient Client, Action Handler)> handlers = new List<(MountedMatchClient, Action)>();
        private readonly List<string> openings = new List<string>();

        public GenericWindowWatch(params MountedMatchClient[] clients)
        {
            foreach (var client in clients)
            {
                var watched = client;
                Action handler = () => openings.Add(Describe(watched));
                watched.Board.DecisionWindow.Opening += handler;
                handlers.Add((watched, handler));
            }
        }

        // The player's current decision lives on an object, and the window never opened so far.
        public void OnItsObject(MountedMatchClient client)
        {
            var view = client.Board.View;
            Assert.IsNotNull(view.Decision, "No decision to check.");
            Assert.AreEqual(view.You, view.DecisionOwner, Describe(client) + " belongs to the opponent.");
            Assert.AreNotEqual(DecisionHost.Window, DecisionRoutes.Of(view), Describe(client) + " has no object host.");
            Assert.IsFalse(client.Board.DecisionWindow.HasChoices, Describe(client) + " offers its choices in the generic window.");
            NeverOpened();
        }

        public void NeverOpened() =>
            Assert.IsEmpty(openings, "The generic decision window opened for: " + string.Join(", ", openings));

        public void Dispose()
        {
            foreach (var (client, handler) in handlers)
                client.Board.DecisionWindow.Opening -= handler;
        }

        // The decision, its owner's side and the tray's shown buttons, so a failure names what opened the window.
        private static string Describe(MountedMatchClient client)
        {
            var view = client.Board.View;
            var decision = view?.Decision;
            if (decision == null)
                return "no decision";
            var buttons = client.Board.Tray.Query<Button>().ToList()
                .Where(button => button.resolvedStyle.display != DisplayStyle.None).Select(button => button.name);
            return decision.Kind + " " + decision.Presentation + " (" + view.You + ": " + string.Join(" ", buttons) + ")";
        }
    }
}
