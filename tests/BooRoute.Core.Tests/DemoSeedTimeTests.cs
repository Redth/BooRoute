namespace BooRoute.Core.Tests;

public sealed class DemoSeedTimeTests
{
    public static TheoryData<DateTimeOffset, TimeOnly, TimeOnly> LocalTimeWindows => new()
    {
        { new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero), new(16, 0), new(22, 0) },
        { new(2026, 10, 31, 1, 0, 0, TimeSpan.Zero), new(23, 0), new(5, 0) },
        { new(2026, 10, 31, 22, 0, 0, TimeSpan.Zero), new(20, 0), new(2, 0) },
        { new(2026, 10, 31, 18, 37, 49, TimeSpan.Zero), new(16, 37, 49), new(22, 37, 49) },
        { new(2026, 10, 31, 1, 23, 45, TimeSpan.FromMinutes(330)), new(23, 23, 45), new(5, 23, 45) },
        { new(2026, 10, 31, 22, 17, 31, TimeSpan.FromHours(-7)), new(20, 17, 31), new(2, 17, 31) }
    };

    [Theory]
    [MemberData(nameof(LocalTimeWindows))]
    public void EveryHouseOpensTwoHoursBeforeAndClosesFourHoursAfterSuppliedLocalTime(
        DateTimeOffset localNow, TimeOnly expectedOpening, TimeOnly expectedClosing)
    {
        var state = DemoSeed.Create(localNow);

        Assert.NotEmpty(state.Houses);
        Assert.All(state.Houses, house =>
        {
            Assert.Equal(expectedOpening, house.OpensAt);
            Assert.Equal(expectedClosing, house.ClosesAt);
        });
    }
}
