using System;

namespace Wotr.Client
{
    // ART-01/ART-02: a figure picture by its own key — the nation and kind of a unit ("gondor-elite",
    // "sauron-nazgul") or the character ("gandalf-grey") — with the UI-76 silhouette of its side and kind as
    // the fallback when no atlas holds the key.
    public readonly struct FigureArt : IEquatable<FigureArt>
    {
        public FigureArt(string key, FigureAppearance fallback)
        {
            Key = key ?? "";
            Fallback = fallback;
        }

        public string Key { get; }

        public FigureAppearance Fallback { get; }

        public static implicit operator FigureArt(FigureAppearance appearance) => new FigureArt("", appearance);

        public bool Equals(FigureArt other) => Key == other.Key && Fallback == other.Fallback;

        public override bool Equals(object obj) => obj is FigureArt other && Equals(other);

        public override int GetHashCode() => (Key.GetHashCode() * 397) ^ (int)Fallback;

        public override string ToString() => Key.Length > 0 ? Key : Fallback.ToString();
    }
}
