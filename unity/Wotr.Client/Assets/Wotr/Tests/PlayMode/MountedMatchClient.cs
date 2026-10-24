using System;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    internal sealed class MountedMatchClient : IDisposable
    {
        public GameApi Api { get; }
        public BoardScreen Board { get; }
        public MatchConnection Connection { get; }
        public int Connected { get; private set; }
        private readonly GameObject host;
        private readonly PanelSettings panel;
        private readonly RenderTexture viewport;
        private TurnInteraction interaction;
        private readonly ActionLogController history;
        private readonly BattleReportController reports;
        public ActionNoticeController Activity { get; }
        private readonly PlaybackBinding playback;
        // UI-305: the production presenters, as MatchSession registers them.
        public OpponentPresenters Presenters { get; }
        // UI-304: the same playback coordinator MatchSession binds; tests register presenters here.
        public PresentationPlayback Playback => playback.Playback;
        public TurnInteraction Interaction => interaction;
        private readonly bool ownsApi;
        private readonly LocalMatchPersistence persistence;
        private bool fixtureOnline;
        public PanelSettings Panel => panel;
        public RenderTexture Viewport => viewport;
        public InterfaceController Interface { get; }
        private readonly string preferencesKey = "test.interface." + Guid.NewGuid().ToString("N");
        public string PreferencesKey => preferencesKey;
        // AUDIO-04: the open game's sounds, as MatchSession binds them, renewed with the interaction.
        public GameSounds Sounds { get; private set; }
        // Layout fixtures may reuse revision zero for unrelated scenes. Production updates
        // below keep one controller and always pass through MatchConnection's version gate.
        public void Apply(PlayerView view)
        {
            interaction.Dispose();
            interaction = new TurnInteraction(Api, Connection, Board, persistence);
            BindSounds();
            Refresh(view);
        }

        // Same path as a received snapshot, preserving the current interaction/draft.
        public void Refresh(PlayerView view) => playback.Receive(view);

        // As MatchSession presents a view the playback released.
        private void Present(PlayerView view)
        {
            Board.Apply(view);
            interaction.Update(view);
            Sounds?.Update(view);
            history.Update(view);
            if (fixtureOnline)
                interaction.SetConnection("Connected");
        }

        public void SetOnline()
        {
            fixtureOnline = true;
            interaction.SetConnection("Connected");
        }

        public MountedMatchClient(string address, string match, HttpMessageHandler handler = null, GameApi api = null,
            LocalMatchPersistence persistence = null, NotificationReadStore reads = null)
        {
            this.persistence = persistence;
            ownsApi = api == null;
            Api = api ?? new GameApi(address, handler);
            Connection = new MatchConnection(Api, match);
            panel = Object.Instantiate(Resources.Load<PanelSettings>("WotrPanel"));
            viewport = new RenderTexture(1366, 768, 24);
            viewport.Create();
            panel.targetTexture = viewport;
            host = new GameObject("Network integration client");
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.style.flexGrow = 1;
            document.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Wotr"));
            Interface = new InterfaceController(document.rootVisualElement, panel, new InterfacePreferences(preferencesKey));
            Board = new BoardScreen(Interface.Windows);
            Interface.Content.Add(Board.Root);
            Board.InterfaceRequested += Interface.Open;
            interaction = new TurnInteraction(Api, Connection, Board, persistence);
            BindSounds();
            history = new ActionLogController(Api, match, Board.Log);
            reports = new BattleReportController(Api, match, Board.BattleReport, reads ?? persistence?.Notifications);
            Activity = new ActionNoticeController(Api, match, Board, reads ?? persistence?.Notifications);
            playback = new PlaybackBinding(Board, Activity, Present, held => interaction.Hold(held));
            Presenters = new OpponentPresenters(Board, playback);
            Presenters.Sounds.Target = Sounds;
            Connection.Updated += Refresh;
            Connection.StatusChanged += status =>
            {
                if (status == "Connected")
                    Connected++;
                Board.SetConnection(status);
                Sounds?.SetConnection(status);
                interaction.SetConnection(status);
                history.SetConnection(status);
                Activity.SetConnection(status);
                Playback.SetConnection(status);
            };
        }

        private void BindSounds()
        {
            Sounds?.Dispose();
            Sounds = new GameSounds(Interface.Sounds, Board, interaction);
            if (Presenters != null)
                Presenters.Sounds.Target = Sounds;
        }

        public void Dispose()
        {
            Presenters.Dispose();
            playback.Dispose();
            Sounds?.Dispose();
            interaction.Dispose();
            history.Dispose();
            reports.Dispose();
            Activity.Dispose();
            Interface.Dispose();
            PlayerPrefs.DeleteKey(preferencesKey);
            Connection.Dispose();
            if (ownsApi)
                Api.Dispose();
            panel.targetTexture = null;
            viewport.Release();
            Object.Destroy(host);
            Object.Destroy(panel);
            Object.Destroy(viewport);
        }
    }

}
