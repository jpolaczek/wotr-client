using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    internal sealed class MountedFlowClient : IDisposable
    {
        private readonly GameObject host;
        private readonly RenderTexture viewport;
        public PanelSettings Panel { get; }
        public VisualElement Root { get; }
        public ClientFlow Flow { get; }
        public BoardScreen Board => Flow.Board;
        public Task LocalWrites => Flow.LocalWrites;
        public InterfaceController Interface { get; }
        // MENU-14: a confirmed Quit game is counted here instead of leaving Play mode.
        public int Quits { get; private set; }
        private readonly string preferencesKey = "test.interface." + Guid.NewGuid().ToString("N");

        public MountedFlowClient(GameApi api, string storageRoot,
            Func<string, string, string, ILocalMatchStorage> localStorage = null)
        {
            Panel = Object.Instantiate(Resources.Load<PanelSettings>("WotrPanel"));
            viewport = new RenderTexture(1366, 768, 24);
            viewport.Create();
            Panel.targetTexture = viewport;
            host = new GameObject("Saved games integration client");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = Panel;
            Root = document.rootVisualElement;
            Root.style.flexGrow = 1;
            Root.styleSheets.Add(Resources.Load<StyleSheet>("Wotr"));
            Interface = new InterfaceController(Root, Panel, new InterfacePreferences(preferencesKey));
            Flow = new ClientFlow(Interface.Content, api, storageRoot, Interface.Windows, localStorage, () => Quits++, Interface.Sounds);
            Flow.InterfaceRequested += Interface.Open;
        }

        public void Dispose()
        {
            Flow.Dispose();
            Interface.Dispose();
            PlayerPrefs.DeleteKey(preferencesKey);
            Panel.targetTexture = null;
            viewport.Release();
            Object.Destroy(host);
            Object.Destroy(Panel);
            Object.Destroy(viewport);
        }
    }
}
