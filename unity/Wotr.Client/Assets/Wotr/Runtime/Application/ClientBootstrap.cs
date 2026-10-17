using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class ClientBootstrap : MonoBehaviour
    {
        public BoardScreen BoardScreen
        {
            get; private set;
        }
        private PanelSettings panel;
        private ClientFlow flow;
        private InterfaceController ui;
        private UpdateController updates;
        private Updates.SingleInstanceLock instance;
        private VisualElement root;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartClient()
        {
            if (FindAnyObjectByType<ClientBootstrap>() == null)
                new GameObject("WoTR Client").AddComponent<ClientBootstrap>();
        }

        private void OnEnable()
        {
            if (Updates.InstallationSelfCheck.TryRun())
                return;
            Application.targetFrameRate = 60;
            var document = gameObject.AddComponent<UIDocument>();
            var template = Resources.Load<PanelSettings>("WotrPanel");
            if (template == null)
                throw new System.InvalidOperationException("Missing WotrPanel resource. Run BuildTasks.PrepareProject in the editor.");
            panel = Instantiate(template);
            document.panelSettings = panel;
            root = document.rootVisualElement;
            root.style.flexGrow = 1;
            root.styleSheets.Add(Resources.Load<StyleSheet>("Wotr"));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Diagnostics.MapEdgeProbe.TryStart(gameObject, root, panel))
                return;
#endif
            ui = new InterfaceController(root, panel);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Diagnostics.MapRenderProfile.TryStart(gameObject, this, panel))
                return;
            if (Diagnostics.DesktopProfile.TryStart(gameObject, ui, panel))
                return;
#endif
            // DIST-07: Velopack closes every process of the installation before it replaces files, so a second copy
            // of a release build says the game already runs instead of being closed by an update later. Development
            // builds may run side by side (two test accounts on one machine).
            if (!Application.isEditor && !ClientIdentity.Current.Development && !LockInstance())
            {
                ui.Content.Add(AlreadyRunning());
                return;
            }
            flow = new ClientFlow(ui.Content, windows: ui.Windows, sounds: ui.Sounds);
            flow.InterfaceRequested += ui.Open;
            updates = UpdateController.Start(ui, flow);
        }

        private bool LockInstance()
        {
            try
            {
                instance = Updates.SingleInstanceLock.TryAcquire(UpdateController.Folder);
                return instance != null;
            }
            catch (System.Exception error) when (error is System.IO.IOException || error is System.UnauthorizedAccessException)
            {
                Debug.LogWarning("[Updates] The single-instance lock is unavailable: " + error.Message);
                return true;
            }
        }

        private static VisualElement AlreadyRunning()
        {
            var screen = new VisualElement { name = "already-running" };
            screen.AddToClassList("settings-scrim");
            var window = new VisualElement();
            window.AddToClassList("quit-window");
            window.Add(BoardScreen.Title("War of the Ring is already running"));
            var note = new Label("Switch to the open game window. Only one copy of the game can run at a time.");
            note.AddToClassList("quit-note");
            window.Add(note);
            var actions = BoardScreen.Row("quit-actions");
            var quit = BoardScreen.Button("Quit", ApplicationQuit.Exit);
            quit.name = "already-running-quit";
            actions.Add(quit);
            window.Add(actions);
            screen.Add(window);
            screen.schedule.Execute(() => quit.Focus());
            return screen;
        }
        private void Update()
        {
            updates?.Tick();
            ui?.Barrier.Update();
            ui?.Sounds.Update();
            MapScreenPointer.Update(flow?.Board?.Map);
        }

        public void ShowFixture()
        {
            updates?.Dispose();
            updates = null;
            flow?.Dispose();
            flow = null;
            ui?.Dispose();
            ui = null;
            root.Clear();
            BoardScreen = new BoardScreen();
            root.Add(BoardScreen.Root);
            BoardScreen.ShowFixture();
        }
        private void OnDestroy()
        {
            updates?.Dispose();
            flow?.Dispose();
            ui?.Dispose();
            instance?.Dispose();
            if (panel != null)
                Destroy(panel);
        }
    }
}
