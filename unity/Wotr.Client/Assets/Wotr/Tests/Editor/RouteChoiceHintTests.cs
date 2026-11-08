using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-519 (owner, 2026-11-07): where the route itself matters the flows keep their region-by-region choice, and the
    // console says plainly that the route is the player's to choose, and why.
    public sealed class RouteChoiceHintTests
    {
        [Test]
        public void ARevelationSaysItsPathIsChosenAndWhyItMatters()
        {
            var hint = FellowshipDraft.PathHint(CommandKind.RevealFellowship);
            StringAssert.StartsWith("Choose the Fellowship's path region by region", hint);
            StringAssert.Contains("Shadow stronghold", hint);
            StringAssert.Contains("extra Hunt", hint);
        }

        [Test]
        public void ADeclarationSaysItsPathIsTheirs()
        {
            var hint = FellowshipDraft.PathHint(CommandKind.DeclareFellowship);
            StringAssert.StartsWith("Choose the Fellowship's path region by region", hint);
            StringAssert.Contains("yours to choose", hint);
        }

        [Test]
        public void ACardMarchSaysItsRouteIsChosenAndWhyItMatters()
        {
            StringAssert.StartsWith("Choose the route region by region", BoardDraft.MarchRoute);
            StringAssert.Contains("capture an empty settlement", BoardDraft.MarchRoute);
            StringAssert.Contains("rouse a nation", BoardDraft.MarchRoute);
        }
    }
}
