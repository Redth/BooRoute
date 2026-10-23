namespace BooRoute.Core.Tests;

public sealed class CanadianTimeZoneTests
{
    [Theory]
    [InlineData("America/St_Johns", 10, 31, -150)]
    [InlineData("America/St_Johns", 11, 2, -210)]
    [InlineData("America/Toronto", 10, 31, -240)]
    [InlineData("America/Toronto", 11, 2, -300)]
    [InlineData("America/Regina", 10, 31, -360)]
    [InlineData("America/Regina", 11, 2, -360)]
    [InlineData("America/Whitehorse", 10, 31, -420)]
    [InlineData("America/Whitehorse", 11, 2, -420)]
    public void WelcomingUsesLocalWallTimeAcrossCanadianZones(string zoneId, int month, int day, int offsetMinutes)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var opening = new DateTime(2026, month, day, 17, 15, 0, DateTimeKind.Unspecified);
        var clock = new ZonedClock(new DateTimeOffset(opening, zone.GetUtcOffset(opening)).ToUniversalTime(), zone);
        var house = Sample(new(17, 15), new(21, 45));

        Assert.Equal(TimeSpan.FromMinutes(offsetMinutes), clock.GetLocalNow().Offset);
        Assert.True(house.IsWelcoming(clock.GetLocalNow()));
        Assert.False(house.IsWelcoming(clock.GetLocalNow().AddTicks(-1)));
        Assert.False(house.IsWelcoming(clock.GetLocalNow().AddHours(4).AddMinutes(30)));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void RepeatedAutumnHourIsWelcomingUntilLocalClosing(int utcHour, bool expected)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");
        var clock = new ZonedClock(new(2026, 11, 1, utcHour, 30, 0, TimeSpan.Zero), zone);
        var house = Sample(new(0, 30), new(2, 30));

        Assert.Equal(expected, house.IsWelcoming(clock.GetLocalNow()));
    }

    [Fact]
    public void NewfoundlandOvernightClosingRemainsExclusive()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/St_Johns");
        var house = Sample(new(22, 0), new(1, 0));
        var beforeClosing = TimeZoneInfo.ConvertTime(new DateTimeOffset(2026, 11, 1, 3, 29, 59, TimeSpan.Zero), zone);
        var atClosing = beforeClosing.AddSeconds(1);

        Assert.Equal(new TimeOnly(0, 59, 59), TimeOnly.FromDateTime(beforeClosing.DateTime));
        Assert.True(house.IsWelcoming(beforeClosing));
        Assert.False(house.IsWelcoming(atClosing));
    }

    private static House Sample(TimeOnly opens, TimeOnly closes) =>
        new(Guid.NewGuid(), "Pumpkin Porch", "13 Make-Believe Lane", "Friendly pumpkins.",
            opens, closes, ScareLevel.Friendly, HouseStatus.LightsOn, true, true);

    private sealed class ZonedClock(DateTimeOffset utcNow, TimeZoneInfo zone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
        public override TimeZoneInfo LocalTimeZone => zone;
    }
}
