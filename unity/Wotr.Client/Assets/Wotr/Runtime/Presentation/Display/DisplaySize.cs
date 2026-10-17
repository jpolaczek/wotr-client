using System;

namespace Wotr.Client
{
    // A resolution in pixels; zero by zero stands for the screen's own (native) resolution.
    public readonly struct DisplaySize : IEquatable<DisplaySize>
    {
        public static readonly DisplaySize Native = new DisplaySize(0, 0);
        public int Width { get; }
        public int Height { get; }
        public bool IsNative => Width == 0 && Height == 0;

        public DisplaySize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public string Key => IsNative ? "native" : Width + "x" + Height;

        public bool Fits(DisplaySize screen) => Width <= screen.Width && Height <= screen.Height;

        public bool Equals(DisplaySize other) => Width == other.Width && Height == other.Height;

        public override bool Equals(object value) => value is DisplaySize other && Equals(other);

        public override int GetHashCode() => Width * 100003 + Height;

        public override string ToString() => IsNative ? "Native" : Width + "×" + Height;
    }
}
