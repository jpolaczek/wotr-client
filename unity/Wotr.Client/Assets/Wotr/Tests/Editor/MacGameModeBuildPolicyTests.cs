using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using Wotr.Editor;

namespace Wotr.EditorTests
{
    public sealed class MacGameModeBuildPolicyTests
    {
        private string directory;
        private string plist;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "WoTR audio policy " + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            plist = Path.Combine(directory, "Info.plist");
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        [TestCase("")]
        [TestCase("<key>GCSupportsGameMode</key><true/><key>LSSupportsGameMode</key><true/>")]
        [TestCase("<key>GCSupportsGameMode</key><string>false</string><key>LSSupportsGameMode</key><false/>")]
        public void DisablesBothGenerationsWithoutChangingIdentityOrOtherMetadata(string flags)
        {
            var document = Fixture(flags);
            var before = new XElement(document.Root.Element("dict"));
            document.Save(plist);

            MacGameModeBuildPolicy.DisableGameMode(plist);
            var dictionary = XDocument.Load(plist).Root.Element("dict");
            foreach (var name in new[] { "GCSupportsGameMode", "LSSupportsGameMode" })
            {
                var key = dictionary.Elements("key").Single(element => element.Value == name);
                var value = key.ElementsAfterSelf().First();
                Assert.AreEqual("false", value.Name.LocalName, name + " must be a boolean, not a string.");
                value.Remove();
                key.Remove();
                var oldKey = before.Elements("key").SingleOrDefault(element => element.Value == name);
                oldKey?.ElementsAfterSelf().First().Remove();
                oldKey?.Remove();
            }
            Assert.IsTrue(XNode.DeepEquals(before, dictionary), "Keep bundle ID, version, category, ATS and nested keys.");

            var first = File.ReadAllText(plist);
            MacGameModeBuildPolicy.DisableGameMode(plist);
            Assert.AreEqual(first, File.ReadAllText(plist), "Repeated processing must not add duplicate keys.");
        }

        [TestCase("<plist><array/></plist>")]
        [TestCase("<plist><dict><key>GCSupportsGameMode</key></dict></plist>")]
        [TestCase("<plist><dict><key>GCSupportsGameMode</key><true/><key>GCSupportsGameMode</key><false/></dict></plist>")]
        [TestCase("<plist><dict>")]
        public void InvalidPlistFailsWithoutOverwritingTheInput(string content)
        {
            File.WriteAllText(plist, content);
            Assert.Throws<BuildFailedException>(() => MacGameModeBuildPolicy.DisableGameMode(plist));
            Assert.AreEqual(content, File.ReadAllText(plist));
        }

        [Test]
        public void MissingPlistFailsAndWindowsDoesNotTouchMacFiles()
        {
            Assert.Throws<BuildFailedException>(() => MacGameModeBuildPolicy.DisableGameMode(plist));
            MacGameModeBuildPolicy.Apply(BuildTarget.StandaloneWindows64, directory);
            Assert.IsEmpty(Directory.GetFileSystemEntries(directory));
        }

#if UNITY_EDITOR_OSX
        [TestCase("x64")]
        [TestCase("arm64")]
        public void ProcessedMacBundleHasAValidSignatureAfterThePlistChanges(string architecture)
        {
            var app = Path.Combine(directory, "Audio Fixture.app");
            var contents = Path.Combine(app, "Contents");
            var executable = Path.Combine(contents, "MacOS", "Fixture");
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            // Use the actual Unity player stub: Apple's /usr/bin tools contain arm64e and system signatures,
            // whereas the build module signs Unity's x64/arm64 layout. No player build or launch is needed.
            var module = Type.GetType("UnityEditor.OSXStandalone.MacOSCodeSigning, UnityEditor.OSXStandalone.Extensions");
            var player = Path.Combine(Path.GetDirectoryName(module.Assembly.Location), "Variations",
                "macos_" + architecture + "_player_nondevelopment_mono", "UnityPlayer.app", "Contents", "MacOS", "UnityPlayer");
            File.Copy(player, executable);
            Fixture("<key>GCSupportsGameMode</key><true/>").Save(Path.Combine(contents, "Info.plist"));

            MacGameModeBuildPolicy.Apply(BuildTarget.StandaloneOSX, app);
            VerifySignature(app);
            var first = File.ReadAllText(Path.Combine(contents, "Info.plist"));
            MacGameModeBuildPolicy.Apply(BuildTarget.StandaloneOSX, app);
            VerifySignature(app);
            Assert.AreEqual(first, File.ReadAllText(Path.Combine(contents, "Info.plist")));
        }

        private static void VerifySignature(string app)
        {
            VerifyMacTool("/usr/bin/plutil", "-lint \"" + Path.Combine(app, "Contents", "Info.plist") + "\"");
            VerifyMacTool("/usr/bin/codesign", "--verify --deep --strict \"" + app + "\"");
        }

        private static void VerifyMacTool(string tool, string arguments)
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tool,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.AreEqual(0, process.ExitCode, output + error);
        }
#endif

        private static XDocument Fixture(string flags) => XDocument.Parse(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">" +
            "<plist version=\"1.0\"><dict>" +
            "<key>CFBundleIdentifier</key><string>org.wotr.audio-fixture</string>" +
            "<key>CFBundleExecutable</key><string>Fixture</string>" +
            "<key>CFBundlePackageType</key><string>APPL</string>" +
            "<key>CFBundleVersion</key><string>123</string>" +
            "<key>LSApplicationCategoryType</key><string>public.app-category.games</string>" +
            "<key>NSAppTransportSecurity</key><dict><key>NSAllowsArbitraryLoads</key><true/></dict>" +
            "<key>NestedMetadata</key><dict><key>GCSupportsGameMode</key><true/></dict>" +
            flags + "</dict></plist>");
    }
}
