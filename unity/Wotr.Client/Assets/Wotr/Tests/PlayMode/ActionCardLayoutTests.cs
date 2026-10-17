using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class ActionCardLayoutTests
    {
        [UnityTest]
        public IEnumerator FourLayoutsKeepVirtualRowsCardGesturesAndWindowBounds()
        {
            using var client = new MountedMatchClient("https://localhost", "action-layout");
            var view = new BoardFixture().View;
            view.MatchId = "action-layout";
            client.Apply(view);
            var board = client.Board;
            var log = board.Log;
            var page = JsonConvert.DeserializeObject<ActionLogPage>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/ActionLogWire.json")));
            var model = new ActionLogModel();
            model.Merge(page);
            model.Loaded = true;
            log.Bind(model);
            log.SetStatus("", false, false);
            var group = model.Rows[0].Group;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    client.Panel.targetTexture = texture;
                    client.Panel.scale = scale;
                    board.Windows.Clear();
                    log.Open();
                    yield return Settle();
                    var row = log.Root.Q<Button>("log-open-" + group.Action.Id);
                    AssertHit(row);
                    PointerGestureEvents.ClickAt(row);
                    log.ShowAction(group, true);
                    yield return Settle();
                    Assert.IsTrue(log.Pinned);
                    Inside(log.Root, board.Map);
                    Inside(log.Popup, board.Map);
                    AssertHit(log.Popup.Q<Button>("close-action-summary"));
                    var before = log.Root.layout.position;
                    var header = log.Root.Q(className: FloatingWindow.HandleClass);
                    header.Focus();
                    using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.RightArrow }))
                        header.SendEvent(key);
                    yield return Settle();
                    Assert.That(log.Root.layout.x, Is.GreaterThan(before.x + 8));
                    var moved = log.Root.layout.position;
                    log.Close();
                    log.Open();
                    yield return Settle();
                    Assert.That(Vector2.Distance(moved, log.Root.layout.position), Is.LessThan(1));
                    Assert.AreEqual(view.Revision, board.View.Revision);
                    // UI-106: effects only — no die or cost row, no numbered step headings and no sources; the side
                    // appears once, in the heading.
                    Assert.IsNull(log.Card.Root.Q(className: "action-cost"));
                    Assert.IsNull(log.Card.Root.Q(className: "action-step-heading"));
                    var texts = log.Card.Root.Query<Label>().ToList().Where(l => l.resolvedStyle.display != DisplayStyle.None)
                        .Select(l => l.text).ToList();
                    Assert.IsFalse(texts.Any(t => t.StartsWith("Source ·")), "No sources on the card");
                    Assert.IsFalse(texts.Any(t => t.Contains(" die")), "No die on the card");
                    var side = ActionCardText.Actor(group.Action.Initiator);
                    StringAssert.StartsWith(side, log.Card.Root.Q<Label>("action-card-heading").text);
                    Assert.AreEqual(1, texts.Count(t => t.StartsWith(side + " ·")), "The side is named once");
                    log.Popup.Focus();
                    yield return Settle();
                    LayoutTests.Save(texture, $"ui41-action-log-{width}-{scale:0.00}.png");
                    log.CloseCard();
                    log.Close();
                    client.Panel.targetTexture = client.Viewport;
                    texture.Release();
                    Object.Destroy(texture);
                }
        }

        [UnityTest]
        public IEnumerator NewPagePreservesFocusedControlAndReadingAnchor()
        {
            using var client = new MountedMatchClient("https://localhost", "list-focus");
            var view = new BoardFixture().View;
            view.MatchId = "list-focus";
            client.Apply(view);
            var log = client.Board.Log;
            var model = new ActionLogModel();
            model.Merge(Page(0, 80));
            model.Loaded = true;
            log.Bind(model);
            log.Open();
            yield return Settle();
            log.List.Root.scrollOffset = new Vector2(0, 1000);
            yield return Settle();
            var focusId = model.Rows[model.AnchorIndex() + 1].Id;
            var button = log.Root.Q<Button>("log-open-" + focusId);
            button.Focus();
            yield return Settle();
            log.List.SaveAnchor();
            var anchor = model.Anchor;
            var offset = model.AnchorOffset;
            model.Merge(Page(80, 82));
            log.Changed();
            yield return Settle();
            Assert.AreEqual(anchor, model.Anchor);
            Assert.That(model.AnchorOffset, Is.EqualTo(offset).Within(1));
            Assert.AreSame(button, log.Root.Q<Button>("log-open-" + focusId));
            Assert.AreSame(button, log.Root.panel.focusController.focusedElement);
            AssertHit(button);
            Assert.Less(log.List.RenderedCount, 12);
        }

        private static ActionLogPage Page(int from, int to)
        {
            var page = new ActionLogPage { MatchId = "list-focus" };
            for (var i = from; i < to; i++)
            {
                page.Actions.Add(new ActionLogSummary { Id = "a" + i, Version = "s" + i, StepCount = 1,
                    Initiator = Side.Shadow, Command = CommandKind.Pass, Status = ActionLogStatus.Completed });
                page.Steps.Add(new ActionLogStep { Id = "s" + i, ActionId = "a" + i, Revision = i,
                    Actor = Side.Shadow, Command = CommandKind.Pass, Text = "Shadow passed." });
            }
            return page;
        }

        private static void AssertHit(VisualElement element)
        {
            Assert.NotNull(element);
            var hit = element.panel.Pick(element.worldBound.center);
            Assert.IsTrue(hit == element || element.Contains(hit), element.name + " covered by " + hit?.name);
        }

        private static void Inside(VisualElement element, VisualElement area)
        {
            Assert.That(element.worldBound.xMin, Is.GreaterThanOrEqualTo(area.worldBound.xMin - 1));
            Assert.That(element.worldBound.yMin, Is.GreaterThanOrEqualTo(area.worldBound.yMin - 1));
            Assert.That(element.worldBound.xMax, Is.LessThanOrEqualTo(area.worldBound.xMax + 1));
            Assert.That(element.worldBound.yMax, Is.LessThanOrEqualTo(area.worldBound.yMax + 1));
        }

        private static IEnumerator Settle() { for (var frame = 0; frame < 12; frame++) yield return null; }
    }
}
