using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Wotr.Editor
{
    // AUDIO-09: this turn-based game shares the desktop with music/video players. Opt out of the OS policy that
    // deprioritizes background apps. Fullscreen and Unity's audio remain available. See docs/ui/AUDIO_UX.md.
    public sealed class MacGameModeBuildPolicy : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report) => Apply(report.summary.platform, report.summary.outputPath);

        public static void Apply(BuildTarget target, string appPath)
        {
            if (target != BuildTarget.StandaloneOSX)
                return;

            // The platform module is optional in Windows-only editors. Its signer also works for CI cross-builds;
            // using it avoids requiring macOS's codesign executable on a Linux builder.
            var signer = FindSigner();
            DisableGameMode(Path.Combine(appPath, "Contents", "Info.plist"));
            try
            {
                // Unity already signed the bundle before this callback; editing Info.plist invalidates that seal.
                signer.Invoke(null, new object[] { appPath });
            }
            catch (TargetInvocationException error)
            {
                throw new BuildFailedException(new InvalidOperationException(
                    "Cannot sign the macOS app after disabling Game Mode.", error.InnerException ?? error));
            }
            finally
            {
                // Unity's signer shows a "Code signing app" progress bar that only Unity's own build steps close.
                // Called from this callback it stayed open after the build, then read "Importing assets" for good.
                EditorUtility.ClearProgressBar();
            }
        }

        public static void DisableGameMode(string plistPath)
        {
            XDocument document;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse, XmlResolver = null };
                using var reader = XmlReader.Create(plistPath, settings);
                document = XDocument.Load(reader);
            }
            catch (Exception error) when (error is IOException || error is XmlException || error is UnauthorizedAccessException)
            {
                throw new BuildFailedException("Cannot read the macOS Info.plist: " + error.Message);
            }

            var dictionary = document.Root?.Name == "plist" ? document.Root.Element("dict") : null;
            if (dictionary == null)
                throw new BuildFailedException("The macOS Info.plist must contain a root dictionary.");

            SetFalse(dictionary, "GCSupportsGameMode"); // macOS Sonoma / Sequoia
            SetFalse(dictionary, "LSSupportsGameMode"); // current Apple key
            // Apple's plist parser rejects even an empty internal DTD subset ([]) emitted by Mono's XML reader.
            var doctype = document.DocumentType;
            if (doctype != null)
                doctype.ReplaceWith(new XDocumentType(doctype.Name, doctype.PublicId, doctype.SystemId, null));
            // Unity's signer decodes the plist bytes directly into a string; a UTF-8 BOM breaks its XML parser.
            using var writer = XmlWriter.Create(plistPath, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true
            });
            document.Save(writer);
        }

        private static void SetFalse(XElement dictionary, string name)
        {
            var keys = dictionary.Elements("key").Where(key => key.Value == name).ToArray();
            if (keys.Length > 1)
                throw new BuildFailedException("Duplicate macOS Info.plist key: " + name);
            if (keys.Length == 0)
            {
                dictionary.Add(new XElement("key", name), new XElement("false"));
                return;
            }

            var value = keys[0].ElementsAfterSelf().FirstOrDefault();
            if (value == null || value.Name == "key")
                throw new BuildFailedException("Missing macOS Info.plist value: " + name);
            value.ReplaceWith(new XElement("false"));
        }

        private static MethodInfo FindSigner()
        {
            var type = Type.GetType("UnityEditor.OSXStandalone.MacOSCodeSigning, UnityEditor.OSXStandalone.Extensions");
            var method = type?.GetMethod("CodeSignAppBundle", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string) }, null);
            if (method != null)
                return method;
            throw new BuildFailedException("The macOS build module must provide CodeSignAppBundle to sign the updated Info.plist.");
        }
    }
}
