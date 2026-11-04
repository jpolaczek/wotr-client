using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-486 (owner, 2026-11-04): the signed counts a watching client's map showed while it presented, in order, for a test
    // whose scripted gestures wait out the whole presentation. A frame of each new count, its glow faded in, goes to
    // .artifacts/screenshots.
    internal sealed class ObservedDeltaWatch
    {
        private readonly MountedMatchClient client;
        private readonly string frames;
        private readonly Task loop;
        private bool stopping;
        private DateTime? frameDue;
        public List<string> Counts { get; } = new List<string>();

        public ObservedDeltaWatch(MountedMatchClient client, string frames)
        {
            this.client = client;
            this.frames = frames;
            loop = Watch();
        }

        // Ends once the client's presentation has played out.
        public async Task Finish()
        {
            stopping = true;
            await loop;
        }

        private async Task Watch()
        {
            while (!stopping || client.Playback.Holding)
            {
                Sample();
                await Task.Delay(20);
            }
        }

        private void Sample()
        {
            foreach (var label in client.Board.Map.Query<Label>("observed-delta-count").ToList())
            {
                if (Counts.LastOrDefault() == label.text)
                    continue;
                Counts.Add(label.text);
                frameDue = DateTime.UtcNow.AddMilliseconds(250);
            }
            if (!frameDue.HasValue || DateTime.UtcNow < frameDue.Value)
                return;
            frameDue = null;
            LayoutTests.Save(client.Viewport, frames + "-" + Counts.Count + ".png");
        }
    }
}
