using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Wotr.Client
{
    // UI-180: the CoreGraphics calls behind the cursor barrier on macOS (called only in the macOS player). Positions are
    // global display coordinates in points, with the origin at the top left of the main display.
    internal static class MacCursor
    {
        private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private static readonly uint[] Displays = new uint[1];

        [StructLayout(LayoutKind.Sequential)]
        private struct CGPoint
        {
            public double X;
            public double Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CGSize
        {
            public double Width;
            public double Height;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CGRect
        {
            public CGPoint Origin;
            public CGSize Size;
        }

        [DllImport(CoreGraphics)] private static extern IntPtr CGEventCreate(IntPtr source);
        [DllImport(CoreGraphics)] private static extern CGPoint CGEventGetLocation(IntPtr evt);
        [DllImport(CoreFoundation)] private static extern void CFRelease(IntPtr value);
        [DllImport(CoreGraphics)] private static extern int CGGetDisplaysWithPoint(CGPoint point, uint maxDisplays, uint[] displays, out uint count);
        [DllImport(CoreGraphics)] private static extern CGRect CGDisplayBounds(uint display);
        [DllImport(CoreGraphics)] private static extern int CGWarpMouseCursorPosition(CGPoint point);
        [DllImport(CoreGraphics)] private static extern int CGAssociateMouseAndMouseCursorPosition(int connected);

        public static Vector2 Location()
        {
            var current = CGEventCreate(IntPtr.Zero);
            try
            {
                var point = CGEventGetLocation(current);
                return new Vector2((float)point.X, (float)point.Y);
            }
            finally
            {
                CFRelease(current);
            }
        }

        // The bounds of the display under the point, or null when no display holds it.
        public static Rect? ScreenOf(Vector2 point)
        {
            var at = new CGPoint { X = point.x, Y = point.y };
            if (CGGetDisplaysWithPoint(at, 1, Displays, out var count) != 0 || count == 0)
                return null;
            var bounds = CGDisplayBounds(Displays[0]);
            return new Rect((float)bounds.Origin.X, (float)bounds.Origin.Y, (float)bounds.Size.Width, (float)bounds.Size.Height);
        }

        // Moves the cursor without the short pause macOS puts on mouse input after a warp.
        public static void Warp(Vector2 point)
        {
            CGWarpMouseCursorPosition(new CGPoint { X = point.x, Y = point.y });
            CGAssociateMouseAndMouseCursorPosition(1);
        }
    }
}
