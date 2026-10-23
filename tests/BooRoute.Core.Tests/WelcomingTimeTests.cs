namespace BooRoute.Core.Tests;

public sealed class WelcomingTimeTests
{
    [Theory]
    [InlineData(21, 46, 51, false)]
    [InlineData(2, 46, 51, true)]
    [InlineData(0, 0, 0, true)]
    public void OpeningIsInclusiveAndClosingIsExclusiveToTheTick(
        int closingHour, int closingMinute, int closingSecond, bool closesNextDay)
    {
        var opensAt = new TimeOnly(17, 13, 27).Add(TimeSpan.FromTicks(3));
        var closesAt = new TimeOnly(closingHour, closingMinute, closingSecond);
        var house = Sample(opensAt, closesAt);
        var date = new DateOnly(2026, 10, 31);
        var opening = new DateTimeOffset(date.ToDateTime(opensAt), TimeSpan.Zero);
        var closingDate = closesNextDay ? date.AddDays(1) : date;
        var closing = new DateTimeOffset(closingDate.ToDateTime(closesAt), TimeSpan.Zero);

        Assert.False(house.IsWelcoming(opening.AddTicks(-1)));
        Assert.True(house.IsWelcoming(opening));
        Assert.True(house.IsWelcoming(closing.AddTicks(-1)));
        Assert.False(house.IsWelcoming(closing));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(-6)]
    public void SuppliedLocalTimeControlsAvailabilityForTheSameInstant(int offsetHours)
    {
        var house = Sample(new(17, 13, 27), new(21, 46, 51));
        var offset = TimeSpan.FromHours(offsetHours);
        var welcomingLocalTime = new DateTimeOffset(2026, 10, 31, 18, 0, 0, offset);
        var closedLocalTime = welcomingLocalTime.ToOffset(-offset);

        Assert.Equal(welcomingLocalTime.UtcDateTime, closedLocalTime.UtcDateTime);
        Assert.True(house.IsWelcoming(welcomingLocalTime));
        Assert.False(house.IsWelcoming(closedLocalTime));
    }

    private static House Sample(TimeOnly opensAt, TimeOnly closesAt) =>
        new(new Guid("7bde24b0-4b64-4ff1-b404-ab9e041c76e9"), "Pumpkin Porch", "13 Make-Believe Lane",
            "Friendly pumpkins.", opensAt, closesAt, ScareLevel.Friendly,
            HouseStatus.LightsOn, true, true);
}
