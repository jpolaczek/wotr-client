using UnityEngine;

namespace Wotr.Client
{
    // AUDIO-02: plays the catalogue through two 2D audio sources: the new clip on one while the other fades out over
    // 40 ms, so a replaced or silenced clip never ends with a click. Owns its game object for the application. The
    // client builds its scene in code, so the output also provides the one audio listener when there is none.
    public sealed class UnitySoundOutput : ISoundOutput, System.IDisposable
    {
        private const float FadeSeconds = 0.04f;
        private readonly SoundCatalog catalog;
        private readonly GameObject host;
        private readonly AudioSource[] sources = new AudioSource[2];
        private readonly float[] fading = new float[2];
        private static AudioListener listener;
        private int active;

        public UnitySoundOutput(SoundCatalog catalog)
        {
            this.catalog = catalog;
            host = new GameObject("WoTR Sounds");
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(host);
            EnsureListener();
            for (var i = 0; i < sources.Length; i++)
            {
                sources[i] = host.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0;
                sources[i].ignoreListenerPause = true;
            }
        }

        public AudioSource Current => sources[active];

        public void Play(SoundCue cue, int variant, float volume)
        {
            var clip = catalog.Clip(cue, variant);
            if (clip == null)
                return;
            EnsureListener();
            FadeOut(active);
            active = 1 - active;
            var source = sources[active];
            fading[active] = 0;
            source.Stop();
            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        public void Stop() => FadeOut(active);

        public void Update(float seconds)
        {
            for (var i = 0; i < sources.Length; i++)
            {
                if (fading[i] <= 0)
                    continue;
                var source = sources[i];
                source.volume = Mathf.Max(0, source.volume - fading[i] * seconds / FadeSeconds);
                if (source.volume > 0)
                    continue;
                source.Stop();
                fading[i] = 0;
            }
        }

        // A listener closed with another interface is replaced by this one.
        private void EnsureListener()
        {
            if (listener != null)
                return;
            listener = Object.FindAnyObjectByType<AudioListener>();
            if (listener == null)
                listener = host.AddComponent<AudioListener>();
        }

        private void FadeOut(int index)
        {
            if (sources[index].isPlaying)
                fading[index] = Mathf.Max(sources[index].volume, 0.0001f);
        }

        public void Dispose()
        {
            if (host == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(host);
            else
                Object.DestroyImmediate(host);
        }
    }
}
