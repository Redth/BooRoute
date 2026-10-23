namespace BooRoute.Pages;

public sealed class CandyConfettiView : GraphicsView
{
    private const string AnimationName = "CandyConfetti";
    private readonly CandyDrawable candy = new();

    public CandyConfettiView()
    {
        Drawable = candy;
        InputTransparent = true;
        // Keep the overlay laid out so the first burst has valid bounds.
        Opacity = 0;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        Unloaded += (_, _) => Stop();
    }

    public void Burst()
    {
        Stop();
        if (!MotionEnabled || Width <= 0 || Height <= 0)
            return;

        candy.Reset();
        Opacity = 1;
        new Animation(progress =>
        {
            if (!MotionEnabled)
            {
                Stop();
                return;
            }
            candy.Elapsed = (float)progress * 1.6f;
            Invalidate();
        }).Commit(this, AnimationName, rate: 16, length: 1600,
            finished: (_, _) => Opacity = 0);
    }

    public void Stop()
    {
        this.AbortAnimation(AnimationName);
        Opacity = 0;
    }

    private static bool MotionEnabled
    {
        get
        {
#if IOS || MACCATALYST
            return !UIKit.UIAccessibility.IsReduceMotionEnabled;
#elif ANDROID
            return Android.Provider.Settings.Global.GetFloat(
                Android.App.Application.Context.ContentResolver,
                Android.Provider.Settings.Global.AnimatorDurationScale, 1) > 0;
#elif WINDOWS
            return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
#else
            return false;
#endif
        }
    }

    private sealed class CandyDrawable : IDrawable
    {
        private readonly record struct Sweet(float Horizontal, float Upward, float Size,
            float Angle, float Spin, float Delay, int Kind, Color Color);

        private static readonly Color[] Palette = [Ui.Orange, Ui.Mint, Ui.Muted, Ui.Cream];
        private static readonly PathF Wrappers = CreateWrappers();
        private static readonly PathF CandyCorn = CreateCandyCorn();
        private readonly Sweet[] sweets = new Sweet[32];
        public float Elapsed { get; set; }

        public void Reset()
        {
            Elapsed = 0;
            for (var i = 0; i < sweets.Length; i++)
            {
                sweets[i] = new(
                    (Random.Shared.NextSingle() - 0.5f) * 1.2f,
                    0.8f + Random.Shared.NextSingle() * 0.5f,
                    0.75f + Random.Shared.NextSingle() * 0.5f,
                    Random.Shared.NextSingle() * 360,
                    (Random.Shared.NextSingle() - 0.5f) * 480,
                    Random.Shared.NextSingle() * 0.08f,
                    i % 3, Palette[i % Palette.Length]);
            }
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            foreach (var sweet in sweets)
            {
                var time = Elapsed - sweet.Delay;
                if (time < 0)
                    continue;
                var x = dirtyRect.Width * (0.5f + sweet.Horizontal * time);
                var y = dirtyRect.Height * (0.85f - sweet.Upward * time + 0.85f * time * time);
                canvas.SaveState();
                canvas.Alpha = Math.Clamp((1.6f - Elapsed) / 0.35f, 0, 1);
                canvas.Translate(x, y);
                canvas.Rotate(sweet.Angle + sweet.Spin * time);
                canvas.Scale(sweet.Size, sweet.Size);
                DrawSweet(canvas, sweet);
                canvas.RestoreState();
            }
        }

        private static void DrawSweet(ICanvas canvas, Sweet sweet)
        {
            canvas.FillColor = sweet.Color;
            if (sweet.Kind == 0)
            {
                canvas.FillPath(Wrappers);
                canvas.FillRoundedRectangle(-8, -6, 16, 12, 5);
                canvas.FillColor = Ui.Cream;
                canvas.FillRoundedRectangle(-4, -4, 3, 8, 1);
            }
            else if (sweet.Kind == 1)
            {
                canvas.ClipPath(CandyCorn);
                canvas.FillColor = Ui.Cream;
                canvas.FillRectangle(-9, -11, 18, 7);
                canvas.FillColor = Ui.Orange;
                canvas.FillRectangle(-9, -4, 18, 8);
                canvas.FillColor = Ui.Mint;
                canvas.FillRectangle(-9, 4, 18, 7);
            }
            else
            {
                canvas.FillColor = Ui.Cream;
                canvas.FillRoundedRectangle(-1.5f, 0, 3, 19, 1.5f);
                canvas.FillColor = sweet.Color;
                canvas.FillCircle(0, -4, 8);
                canvas.StrokeColor = Ui.Midnight;
                canvas.StrokeSize = 2;
                canvas.DrawCircle(0, -4, 4);
                canvas.FillColor = Ui.Cream;
                canvas.FillCircle(-2, -6, 2);
            }
        }

        private static PathF CreateWrappers()
        {
            var path = new PathF();
            path.MoveTo(-6, 0);
            path.LineTo(-15, -6);
            path.LineTo(-15, 6);
            path.Close();
            path.MoveTo(6, 0);
            path.LineTo(15, -6);
            path.LineTo(15, 6);
            path.Close();
            return path;
        }

        private static PathF CreateCandyCorn()
        {
            var path = new PathF();
            path.MoveTo(-2, -9);
            path.QuadTo(0, -12, 2, -9);
            path.LineTo(9, 7);
            path.QuadTo(10, 11, 6, 11);
            path.LineTo(-6, 11);
            path.QuadTo(-10, 11, -9, 7);
            path.Close();
            return path;
        }
    }
}
