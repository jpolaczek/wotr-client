using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    internal static class HuntRoleScreenshots
    {
        public static async Task Capture(UI10TestRoom room, string stage, bool fellowship = false)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
                await CaptureClient(room, client, stage, fellowship);
            await room.Settle();
        }

        private static async Task CaptureClient(UI10TestRoom room, MountedMatchClient client, string stage, bool fellowship)
        {
            var wasOpen = fellowship ? client.Board.Fellowship.IsOpen : client.Board.Hunt.IsOpen;
            if (fellowship)
                client.Board.Fellowship.Open();
            else
                client.Board.Hunt.Open();
            try
            {
                await CaptureCurrent(client, room.Settle, stage);
            }
            finally
            {
                if (!wasOpen && fellowship)
                    client.Board.Fellowship.Close();
                else if (!wasOpen)
                    client.Board.Hunt.Close();
            }
        }

        public static async Task CaptureCurrent(MountedMatchClient client, Func<Task> settle, string stage)
        {
            var previousScale = client.Panel.scale;
            var previousTarget = client.Panel.targetTexture;
            try
            {
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        await CaptureLayout(settle, client, stage, width, scale);
            }
            finally
            {
                client.Panel.targetTexture = previousTarget;
                client.Panel.scale = previousScale;
            }
            await settle();
        }

        private static async Task CaptureLayout(Func<Task> settle, MountedMatchClient client,
            string stage, int width, float scale)
        {
            var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            target.Create();
            try
            {
                client.Panel.targetTexture = target;
                client.Panel.scale = scale;
                await settle();
                LayoutTests.Save(target, $"ui54-{stage}-{client.Board.View.You}-{width}-{scale:0.00}.png");
            }
            finally
            {
                client.Panel.targetTexture = client.Viewport;
                target.Release();
                Object.Destroy(target);
            }
        }
    }
}
