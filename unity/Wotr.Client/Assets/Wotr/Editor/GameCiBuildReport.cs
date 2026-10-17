using System;
using UnityEditor.Build.Reporting;

namespace Wotr.Editor
{
    // GameCI (game-ci/cli UnityBuildValidation) accepts a build only when the editor output carries the summary its own
    // build method prints: the "Build results" section and the literal "Build succeeded!". Our build methods print it
    // themselves, once every step after BuildPlayer has passed too.
    internal static class GameCiBuildReport
    {
        internal static void Succeeded(BuildSummary summary)
        {
            Console.WriteLine(
                "\n###########################\n#      Build results      #\n###########################\n\n" +
                $"Duration: {summary.totalTime}\nWarnings: {summary.totalWarnings}\nErrors: {summary.totalErrors}\n" +
                $"Size: {summary.totalSize} bytes\n\nBuild succeeded!");
        }
    }
}
