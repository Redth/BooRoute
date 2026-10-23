namespace BooRoute.Core.Tests;

public sealed class WelcomingFilterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WelcomingOnlyIncludesLightsOnHousesDuringHoursInInputOrder()
    {
        House[] houses =
        [
            CreateHouse(6, new(19, 0), new(21, 0)),
            CreateHouse(4, new(17, 0), new(21, 0)),
            CreateHouse(2, new(17, 0), new(21, 0), HouseStatus.Closed),
            CreateHouse(1, new(18, 0), new(20, 0)),
            CreateHouse(5, new(17, 0), new(18, 0)),
            CreateHouse(7, new(17, 0), new(21, 0), HouseStatus.OutOfTreats),
            CreateHouse(3, new(16, 0), new(19, 0))
        ];

        Assert.Equal(new[] { houses[1], houses[3], houses[6] },
            HouseFilters.Apply(houses, Now, welcomingOnly: true));
    }

    [Theory]
    [InlineData(31, 23)]
    [InlineData(1, 1)]
    public void WelcomingOnlyIncludesOvernightHousesOnBothSidesOfMidnightInInputOrder(int day, int hour)
    {
        var now = new DateTimeOffset(2026, day == 31 ? 10 : 11, day, hour, 0, 0, TimeSpan.Zero);
        House[] houses =
        [
            CreateHouse(6, new(17, 0), new(21, 0)),
            CreateHouse(4, new(22, 0), new(2, 0)),
            CreateHouse(2, new(22, 0), new(2, 0), HouseStatus.Closed),
            CreateHouse(1, new(21, 0), new(3, 0)),
            CreateHouse(5, new(23, 30), new(0, 30)),
            CreateHouse(3, new(22, 0), new(2, 0), HouseStatus.OutOfTreats)
        ];

        Assert.Equal(new[] { houses[1], houses[3] },
            HouseFilters.Apply(houses, now, welcomingOnly: true));
    }

    [Fact]
    public void WelcomingOnlyReturnsEmptyWhenNoHousesMatch()
    {
        House[] houses =
        [
            CreateHouse(1, new(19, 0), new(21, 0)),
            CreateHouse(2, new(17, 0), new(18, 0)),
            CreateHouse(3, new(17, 0), new(21, 0), HouseStatus.Closed),
            CreateHouse(4, new(17, 0), new(21, 0), HouseStatus.OutOfTreats)
        ];

        Assert.Empty(HouseFilters.Apply(houses, Now, welcomingOnly: true));
    }

    [Fact]
    public void WelcomingOnlyReturnsEmptyForEmptyInput()
    {
        Assert.Empty(HouseFilters.Apply(Array.Empty<House>(), Now, welcomingOnly: true));
    }

    private static House CreateHouse(int id, TimeOnly opensAt, TimeOnly closesAt,
        HouseStatus status = HouseStatus.LightsOn) =>
        new(Guid.Parse($"00000000-0000-0000-0000-{id:D12}"), $"Pumpkin Porch {id}",
            $"{id} Make-Believe Lane", "Friendly pumpkins.", opensAt, closesAt,
            ScareLevel.Friendly, status, true, true);
}
