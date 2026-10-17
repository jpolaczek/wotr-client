#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // Native-input observation only: no ClientFlow, network, saved account or synthetic gestures.
    public sealed class MapEdgeProbe : MonoBehaviour
    {
        private sealed class Request
        {
            public int Reset;
            public bool Quit;
        }

        private BoardScreen board;
        private MapEdgeProbeTrace trace;
        private VisualElement input;
        private string output;
        private double deadline;
        private double nextRequest;
        private double nextSample;
        private double nextFlush;
        private int requestedReset;
        private int acknowledgedReset = -1;
        private int settlingFrames = 60;
        private bool finished;

        public static bool TryStart(GameObject host, VisualElement parent, PanelSettings panel)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "--wotr-edge-probe");
            if (index < 0)
                return false;
            if (index + 2 >= args.Length)
                throw new ArgumentException("--wotr-edge-probe requires a PlayerView file and output directory.");
            var probe = host.AddComponent<MapEdgeProbe>();
            probe.Configure(args, index, parent, panel);
            return true;
        }

        private void Configure(string[] args, int index, VisualElement parent, PanelSettings panel)
        {
            output = Path.GetFullPath(args[index + 2]);
            Directory.CreateDirectory(output);
            var view = JsonConvert.DeserializeObject<PlayerView>(File.ReadAllText(args[index + 1]));
            if (view == null)
                throw new ArgumentException("The edge probe requires a non-null PlayerView.");
            var mode = Argument(args, "--wotr-edge-mode", "window");
            if (mode != "window" && mode != "fullscreen")
                throw new ArgumentException("--wotr-edge-mode must be window or fullscreen.");
            var width = (int)Number(args, "--wotr-edge-width", 1366, 640, 7680);
            var height = (int)Number(args, "--wotr-edge-height", 768, 480, 4320);
            var seconds = Number(args, "--wotr-edge-seconds", 180, 10, 300);
            panel.scale = Number(args, "--wotr-edge-scale", 1, 1, 2);
            Screen.SetResolution(width, height, mode == "fullscreen" ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            board = new BoardScreen();
            parent.Add(board.Root);
            board.Apply(view);
            board.Connection.text = "Offline input probe";
            trace = new MapEdgeProbeTrace(Path.Combine(output, "trace.jsonl"), board.Map);
            Attach(parent.panel.visualTree);
            deadline = Time.realtimeSinceStartupAsDouble + seconds;
            WriteStatus("starting");
        }

        private void Update()
        {
            if (finished || trace == null)
                return;
            try
            {
                ObserveFrame();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish("error", 1);
            }
        }

        private void ObserveFrame()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            if (now >= deadline)
            {
                Finish("timeout", 0);
                return;
            }
            if (now >= nextRequest)
            {
                nextRequest = now + .1;
                ReadRequest();
                if (finished)
                    return;
            }
            SettleReset();
            MapScreenPointer.Update(board.Map);
            if (now >= nextSample)
            {
                nextSample = now + .05;
                trace.Record("sample", requestedReset);
            }
            if (now >= nextFlush)
            {
                nextFlush = now + 1;
                trace.Flush();
                WriteStatus(settlingFrames == 0 ? "ready" : "settling");
            }
        }

        private void SettleReset()
        {
            var map = board.Map;
            if (settlingFrames == 0 || map.contentRect.width <= 0 || map.contentRect.height <= 0)
                return;
            settlingFrames--;
            if (settlingFrames == 2)
            {
                map.EdgePan.Stop();
                map.Camera.Zoom = Mathf.Max(1.6f, map.FitZoom * 1.5f);
                map.Camera.Pan = map.contentRect.center - map.TerrainBounds.center * map.Camera.Zoom;
                map.Refresh();
            }
            if (settlingFrames != 0)
                return;
            acknowledgedReset = requestedReset;
            trace.Record("reset-ready", requestedReset);
            trace.Flush();
            WriteStatus("ready");
        }

        private void ReadRequest()
        {
            var path = Path.Combine(output, "request.json");
            if (!File.Exists(path))
                return;
            Request request;
            try
            {
                request = JsonConvert.DeserializeObject<Request>(File.ReadAllText(path));
            }
            catch (IOException) { return; }
            catch (JsonException) { return; }
            if (request == null)
                return;
            if (request.Quit)
            {
                Finish("requested", 0);
                return;
            }
            if (request.Reset <= requestedReset)
                return;
            requestedReset = request.Reset;
            settlingFrames = 4;
            board.Map.EdgePan.Stop();
            WriteStatus("settling");
        }

        private void Attach(VisualElement root)
        {
            input = root;
            root.RegisterCallback<PointerMoveEvent>(ObservePointer, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerEnterEvent>(ObservePointer, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(ObservePointer, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(ObservePointer, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(ObservePointer, TrickleDown.TrickleDown);
            root.RegisterCallback<MouseLeaveWindowEvent>(ObserveWindowLeave, TrickleDown.TrickleDown);
        }

        private void ObservePointer<T>(T evt) where T : EventBase<T>, IPointerEvent, new()
        {
            if (!finished)
                trace.Record(typeof(T).Name, requestedReset, evt.position, (evt.target as VisualElement)?.name);
        }

        private void ObserveWindowLeave(MouseLeaveWindowEvent evt)
        {
            if (!finished)
                trace.Record(nameof(MouseLeaveWindowEvent), requestedReset, evt.mousePosition, (evt.target as VisualElement)?.name);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!finished)
                trace?.Record(focused ? "focus-gained" : "focus-lost", requestedReset);
        }

        private void WriteStatus(string state)
        {
            var path = Path.Combine(output, "status.json");
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonConvert.SerializeObject(new
            {
                State = state,
                RequestedReset = requestedReset,
                AcknowledgedReset = acknowledgedReset,
                Unity = Application.unityVersion,
                Development = Debug.isDebugBuild,
                Editor = Application.isEditor,
                Platform = Application.platform.ToString(),
                Snapshot = trace.Snapshot(),
                trace.Records,
                trace.Dropped
            }, Formatting.Indented));
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }

        private void Finish(string state, int exitCode)
        {
            if (finished)
                return;
            finished = true;
            board.Map.EdgePan.Stop();
            trace.Record(state, requestedReset);
            WriteStatus(state);
            trace.Dispose();
            Application.Quit(exitCode);
        }

        private void OnApplicationQuit()
        {
            if (trace != null && !finished)
                Finish("closed", 0);
        }

        private void OnDestroy()
        {
            finished = true;
            if (input != null)
            {
                input.UnregisterCallback<PointerMoveEvent>(ObservePointer, TrickleDown.TrickleDown);
                input.UnregisterCallback<PointerEnterEvent>(ObservePointer, TrickleDown.TrickleDown);
                input.UnregisterCallback<PointerLeaveEvent>(ObservePointer, TrickleDown.TrickleDown);
                input.UnregisterCallback<PointerDownEvent>(ObservePointer, TrickleDown.TrickleDown);
                input.UnregisterCallback<PointerUpEvent>(ObservePointer, TrickleDown.TrickleDown);
                input.UnregisterCallback<MouseLeaveWindowEvent>(ObserveWindowLeave, TrickleDown.TrickleDown);
            }
            trace?.Dispose();
        }

        private static string Argument(string[] args, string name, string fallback)
        {
            var index = Array.IndexOf(args, name);
            if (index < 0)
                return fallback;
            if (index + 1 >= args.Length)
                throw new ArgumentException(name + " requires a value.");
            return args[index + 1];
        }

        private static float Number(string[] args, string name, float fallback, float min, float max)
        {
            var value = float.Parse(Argument(args, name, fallback.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
            if (float.IsNaN(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }
}
#endif
