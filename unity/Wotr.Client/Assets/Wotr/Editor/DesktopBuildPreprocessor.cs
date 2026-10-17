using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Wotr.Editor
{
    public sealed class DesktopBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            DesktopBuildSetup.RequirePlatform(report.summary.platform);
            DesktopBuildSetup.Prepare();
            ClientBuildIdentity.RequireEmbedded(report.summary.platform);
            BuildServerAddresses.RequireEmbedded();
        }
    }
}
