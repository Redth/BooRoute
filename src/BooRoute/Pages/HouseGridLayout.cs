namespace BooRoute.Pages;

internal static class HouseGridLayout
{
    public const double PageInset = 22;
    public const double ColumnSpacing = 14;
    public const double MinimumCardWidth = 320;

    public static int GetSpan(double collectionWidth)
    {
        if (!double.IsFinite(collectionWidth) || collectionWidth <= 0)
            return 1;

        return Math.Max(1, (int)Math.Floor(
            (collectionWidth + ColumnSpacing) / (MinimumCardWidth + ColumnSpacing)));
    }
}
