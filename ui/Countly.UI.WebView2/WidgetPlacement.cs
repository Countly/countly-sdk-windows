using System;
using CountlySDK.CountlyCommon;

namespace CountlySDK.UI
{
    /// <summary>The coordinate space we hand the widget (DIPs, screen-absolute origin).</summary>
    public struct WidgetSurface
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// Pure mapping from a widget resize request (CSS px relative to the surface) to an on-screen
    /// window rect (DIPs, screen-absolute). CSS px == DIPs under per-monitor DPI awareness.
    /// </summary>
    public static class WidgetPlacement
    {
        public static WidgetRect Resolve(WidgetAction action, WidgetSurface surface)
        {
            if (action == null || !action.HasResize) { return null; }

            bool landscape = surface.Width >= surface.Height;
            WidgetRect r = landscape ? (action.Landscape ?? action.Portrait) : (action.Portrait ?? action.Landscape);
            if (r == null) { return null; }

            int w = Math.Min(r.W, surface.Width);
            int h = Math.Min(r.H, surface.Height);
            int x = surface.X + Clamp(r.X, 0, surface.Width - w);
            int y = surface.Y + Clamp(r.Y, 0, surface.Height - h);
            return new WidgetRect { X = x, Y = y, W = w, H = h };
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
