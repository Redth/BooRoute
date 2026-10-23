using BooRoute.Core;
using BooRoute.Core.Localization;
using BooRoute.ViewModels;
using System.Globalization;

namespace BooRoute.Core.Tests;

public sealed class HouseCardFormattingTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly House TestHouse = new(
        Guid.Parse("782428c5-2f57-42d4-bb31-504b2865417d"),
        "Friendly porch", "123 Fictional Lane", "A fictional porch.",
        new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, false, false);

    [Theory]
    [InlineData(ScareLevel.Friendly, false, false, "Friendly")]
    [InlineData(ScareLevel.Friendly, true, false, "Friendly \u00b7 Quiet visits")]
    [InlineData(ScareLevel.Friendly, false, true, "Friendly \u00b7 Step-free")]
    [InlineData(ScareLevel.Friendly, true, true, "Friendly \u00b7 Quiet visits \u00b7 Step-free")]
    [InlineData(ScareLevel.Spooky, false, false, "Spooky")]
    [InlineData(ScareLevel.Spooky, true, false, "Spooky \u00b7 Quiet visits")]
    [InlineData(ScareLevel.Spooky, false, true, "Spooky \u00b7 Step-free")]
    [InlineData(ScareLevel.Spooky, true, true, "Spooky \u00b7 Quiet visits \u00b7 Step-free")]
    [InlineData(ScareLevel.Scary, false, false, "Scary")]
    [InlineData(ScareLevel.Scary, true, false, "Scary \u00b7 Quiet visits")]
    [InlineData(ScareLevel.Scary, false, true, "Scary \u00b7 Step-free")]
    [InlineData(ScareLevel.Scary, true, true, "Scary \u00b7 Quiet visits \u00b7 Step-free")]
    public void FeaturesIncludesOnlyEnabledFeaturesWithSingleSeparators(
        ScareLevel scareLevel, bool quietVisits, bool stepFreeAccess, string expected)
    {
        var house = TestHouse with
        {
            ScareLevel = scareLevel,
            QuietVisits = quietVisits,
            StepFreeAccess = stepFreeAccess
        };

        var card = new HouseCard(house, false, Now);

        Assert.Equal(expected, card.Features);
    }

    [Theory]
    [InlineData(7, 5, 9, 9, "07:05 \u2013 09:09")]
    [InlineData(17, 7, 21, 43, "17:07 \u2013 21:43")]
    [InlineData(23, 8, 1, 4, "23:08 \u2013 01:04")]
    public void HoursUsesTheConfiguredFormatterInOpeningThenClosingOrder(
        int openingHour, int openingMinute, int closingHour, int closingMinute, string expected)
    {
        var house = TestHouse with
        {
            OpensAt = new(openingHour, openingMinute),
            ClosesAt = new(closingHour, closingMinute)
        };

        var text = new AppText(AppText.Default.Strings)
        {
            FormatTime = time => time.ToString("HH:mm", CultureInfo.InvariantCulture)
        };
        var card = new HouseCard(house, false, Now, text);

        Assert.Equal(expected, card.Hours);
    }
}
