using CountlySDK.CountlyCommon;
using CountlySDK.UI;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class WidgetPlacementTests
    {
        private static WidgetAction Resize(int px,int py,int pw,int ph,int lx,int ly,int lw,int lh) => new WidgetAction {
            IsActionEvent = true, HasResize = true,
            Portrait = new WidgetRect { X=px,Y=py,W=pw,H=ph },
            Landscape = new WidgetRect { X=lx,Y=ly,W=lw,H=lh },
        };

        [Fact]
        public void LandscapeSurface_PicksLandscape_AndOffsetsByOrigin()
        {
            var surface = new WidgetSurface { X = 100, Y = 200, Width = 1920, Height = 1080 };
            var rect = WidgetPlacement.Resolve(Resize(0,0,300,400, 1420,460,500,620), surface);
            Assert.NotNull(rect);
            Assert.Equal(100 + 1420, rect.X);
            Assert.Equal(200 + 460, rect.Y);
            Assert.Equal(500, rect.W);
            Assert.Equal(620, rect.H);
        }

        [Fact]
        public void PortraitSurface_PicksPortrait()
        {
            var surface = new WidgetSurface { X = 0, Y = 0, Width = 800, Height = 1200 };
            var rect = WidgetPlacement.Resolve(Resize(10,20,300,400, 0,0,700,350), surface);
            Assert.Equal(300, rect.W);
            Assert.Equal(10, rect.X);
        }

        [Fact]
        public void ClampsWithinSurface()
        {
            var surface = new WidgetSurface { X = 0, Y = 0, Width = 500, Height = 500 };
            var rect = WidgetPlacement.Resolve(Resize(400,400,300,300, 400,400,300,300), surface);
            Assert.True(rect.X + rect.W <= 500);
            Assert.True(rect.Y + rect.H <= 500);
        }

        [Fact]
        public void NoUsableResize_ReturnsNull()
        {
            Assert.Null(WidgetPlacement.Resolve(new WidgetAction { IsActionEvent = true, HasResize = false }, new WidgetSurface { Width = 100, Height = 100 }));
        }
    }
}
