using System;
using System.IO;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Wotr.Verification
{
    public static class TestDiscovery
    {
        private static TestRunnerApi api;
        private static double deadline;

        public static void Export()
        {
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += CheckTimeout;
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var mode = Environment.GetEnvironmentVariable("WOTR_TEST_DISCOVERY_MODE") == "EditMode"
                ? TestMode.EditMode : TestMode.PlayMode;
            api.RetrieveTestList(mode, root => Write(root, mode));
        }

        private static void Write(ITestAdaptor root, TestMode mode)
        {
            try
            {
                var path = Environment.GetEnvironmentVariable("WOTR_TEST_DISCOVERY_OUTPUT");
                if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Discovery output is required.");
                var document = new XElement("test-discovery",
                    new XAttribute("mode", mode), new XAttribute("unity", Application.unityVersion));
                Append(document, root);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                new XDocument(document).Save(path);
                EditorApplication.update -= CheckTimeout;
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        private static void Append(XElement document, ITestAdaptor node)
        {
            if (node.IsSuite)
            {
                foreach (var child in node.Children) Append(document, child);
                return;
            }
            document.Add(new XElement("test-case", new XAttribute("fullname", node.FullName),
                new XAttribute("classname", node.TypeInfo?.FullName ?? node.ParentFullName),
                new XAttribute("runstate", node.RunState)));
        }

        private static void CheckTimeout()
        {
            if (EditorApplication.timeSinceStartup <= deadline) return;
            Debug.LogError("Test discovery timed out.");
            EditorApplication.Exit(1);
        }
    }
}
