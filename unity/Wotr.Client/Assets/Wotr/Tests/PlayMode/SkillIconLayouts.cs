using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    // UI-440: the bare skill icons where they stand, in four layouts (1366×768 and 1920×1080 at 100 and 125 %), the open
    // windows kept as they are; screenshots ui440-<name>-<width>-<scale>.png. Reduced motion holds a usable icon's light
    // at full strength for the picture.
    internal static class SkillIconLayouts
    {
        public static async Task Capture(MatchTestRoom room, MountedMatchClient client, string name, Action check = null)
        {
            var previous = client.Panel.targetTexture;
            var scale = client.Interface.Preferences.Scale;
            var motion = client.Interface.Preferences.ReducedMotion;
            client.Interface.Preferences.SetReducedMotion(true);
            try
            {
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var size in new[] { 1f, 1.25f })
                        await Layout(room, client, name, width, size, check);
            }
            finally
            {
                client.Panel.targetTexture = previous;
                client.Interface.Preferences.SetScale(scale);
                client.Interface.Preferences.SetReducedMotion(motion);
            }
            await room.Settle();
        }

        private static async Task Layout(MatchTestRoom room, MountedMatchClient client, string name, int width, float size, Action check)
        {
            var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
            texture.Create();
            try
            {
                client.Panel.targetTexture = texture;
                client.Interface.Preferences.SetScale(size);
                await room.Settle();
                LayoutTests.Save(texture, $"ui440-{name}-{width}-{size:0.00}.png");
                check?.Invoke();
            }
            finally
            {
                client.Panel.targetTexture = client.Viewport;
                texture.Release();
                Object.Destroy(texture);
            }
        }
    }
}
