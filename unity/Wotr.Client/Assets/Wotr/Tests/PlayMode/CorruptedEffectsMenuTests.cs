using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // UI-414 (owner, 2026-10-29): the Effects tab names each Corrupted ruler by its ability, lists only passive effects
    // and shows no locate button for a place its text names. The Shadow's optional uses belong to the Characters tab.
    public sealed class CorruptedEffectsMenuTests
    {
        [UnityTest]
        public IEnumerator TheEffectsTabNamesEachCorruptedRulersPassiveAbility() => MatchTestRoom.Wait(Effects());

        private static async Task Effects()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui414-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-corrupted-recruitment", token);
            await Task.Delay(500, token);
            var board = client.Board;
            board.TableEffects.Open();
            await Task.Delay(400, token);
            var titles = board.TableEffects.Root.Query<Button>(className: "effect-source").ToList().Select(b => b.text).ToList();
            CollectionAssert.IsSupersetOf(titles, new[]
            {
                "Corrupted ruler · Brand", "Corrupted ruler · Dáin", "Corrupted ruler · Denethor", "Corrupted ruler · Théoden",
                "Corrupted ruler · Thranduil", "Mark of the White Hand · Théoden"
            });
            CollectionAssert.DoesNotContain(titles, "Dáin Ironfoot", "A button names the ability, not only the character.");
            Assert.IsFalse(board.View.Effects.Any(e => e.Description.Contains("Shadow may")), "An optional use is not an effect.");
            // UI-446 (owner): Inspect unrolls the ability's description as plain text under its read-only tile, as a hover
            // does, and opens no details, so no locate button stands for Erebor, which the text names (UI-414).
            const string dain = "kings.character.dain.corrupted";
            board.TableEffects.Inspect(dain);
            await Task.Delay(300, token);
            var tile = board.TableEffects.Root.Q<Button>("effect-source-" + dain);
            Assert.AreEqual("Corrupted ruler · Dáin", tile.text);
            Assert.AreEqual("Corrupted ruler · Dáin · A public ability in play", tile.tooltip);
            Assert.IsTrue(ReadOnlyControl.Is(tile), "A click on the ability does nothing.");
            EffectsPanelChecks.UnrolledText(board, dain, "ui414");
            StringAssert.Contains("Erebor", board.TableEffects.Root.Q<Label>("effect-unroll-text").text);
            Assert.AreEqual(DisplayStyle.None, board.TableEffects.Details.resolvedStyle.display);
            Assert.IsNull(board.TableEffects.Root.Q<Button>("effect-locate-erebor"), "The text names Erebor.");
            LayoutTests.Save(client.Panel.targetTexture, "ui414-corrupted-effects.png");
        }
    }
}
