using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-409 (owner): a roster row's tooltip reads in clear parts without growing.
    public sealed class CharacterRowTooltipTests
    {
        [Test]
        public void ThePartsStandApartAndTheEntryKeepsItsCostOnOneLine()
        {
            var parts = new CharacterRowTooltip.Parts
            {
                Name = "Saruman", Edition = "Base", State = "Not entered", Stats = "Level 0 · Move 0 · Combat level 0 · Leadership 1",
                Entry = new ActionAvailabilityView
                {
                    Title = "Enter Saruman", Cost = "One Muster-capable result.", WindowOpen = true, Source = "BASE p.41",
                    Conditions = new List<AvailabilityConditionView>
                    {
                        new AvailabilityConditionView { Description = "An eligible entry die is available.", Met = false },
                        new AvailabilityConditionView { Description = "Saruman has never entered.", Met = true }
                    }
                },
                Abilities = new[] { "Bound to Orthanc · Never leaves Orthanc", "The Voice of Saruman · Muster die: a Regular" }
            };
            var formatted = CharacterRowTooltip.Format("Click to bring Saruman into play", parts);
            StringAssert.StartsWith("<color=#FFD670><b>Click to bring Saruman into play</b></color>\n", formatted);
            StringAssert.Contains("<color=#F2D488><b>Saruman</b></color> · Base", formatted);
            StringAssert.Contains("<color=#F2D488>Enter Saruman</color> · Cost: One Muster-capable result.", formatted);
            StringAssert.Contains("<size=11><color=#8E8672>BASE p.41</color></size>", formatted);
            StringAssert.EndsWith("<b>The Voice of Saruman</b> · Muster die: a Regular", formatted);
            var plain = CharacterRowTooltip.Plain(formatted);
            StringAssert.StartsWith("Click to bring Saruman into play\nSaruman · Base\nNot entered\n", plain);
            StringAssert.EndsWith("\nBound to Orthanc · Never leaves Orthanc\nThe Voice of Saruman · Muster die: a Regular", plain);
            StringAssert.Contains("  ○ An eligible entry die is available.", plain);
            StringAssert.Contains("  ✓ Saruman has never entered.", plain);
            // The entry's title and cost share a line: ten full lines and two thin gaps, where the plain text had eleven lines.
            Assert.AreEqual(12, plain.Split('\n').Length);
        }

        [Test]
        public void GameTextNeverOpensATag()
        {
            var formatted = CharacterRowTooltip.Format("", new CharacterRowTooltip.Parts { Name = "A <b> name" });
            StringAssert.Contains("A ‹b> name", formatted);
        }
    }
}
