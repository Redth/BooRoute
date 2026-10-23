namespace BooRoute.Core.Tests;

public sealed class MapHouseIdentityTests
{
    private static House Sample() => new(
        Guid.Parse("12345678-1234-5678-9abc-def012345678"),
        "Pumpkin Porch", "13 Make-Believe Lane", "Friendly pumpkins.",
        new(17, 0), new(21, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true);

    [Fact]
    public void HouseLocationMatchesIdentifierLocation()
    {
        var house = Sample();

        Assert.Equal(DemoMap.LocationFor(house.Id), DemoMap.LocationFor(house));
    }

    [Fact]
    public void ChangingHouseDetailsDoesNotChangeLocation()
    {
        var house = Sample();
        House[] changedHouses =
        [
            house with { Nickname = "Ghost Gate" },
            house with { Address = "42 Imaginary Street" },
            house with { Description = "Spooky ghosts." },
            house with { OpensAt = new(18, 0) },
            house with { ClosesAt = new(22, 0) },
            house with { ScareLevel = ScareLevel.Scary },
            house with { Status = HouseStatus.Closed },
            house with { QuietVisits = false },
            house with { StepFreeAccess = false },
            house with
            {
                Nickname = "Ghost Gate",
                Address = "42 Imaginary Street",
                Description = "Spooky ghosts.",
                OpensAt = new(18, 0),
                ClosesAt = new(22, 0),
                ScareLevel = ScareLevel.Scary,
                Status = HouseStatus.Closed,
                QuietVisits = false,
                StepFreeAccess = false
            }
        ];
        var expected = DemoMap.LocationFor(house.Id);

        foreach (var changedHouse in changedHouses)
            Assert.Equal(expected, DemoMap.LocationFor(changedHouse));
    }

    [Fact]
    public void ChangingIdentifierChangesLocation()
    {
        var house = Sample();
        var changedHouse = house with { Id = Guid.Parse("87654321-4321-8765-9abc-def012345678") };

        var originalLocation = DemoMap.LocationFor(house);
        var changedLocation = DemoMap.LocationFor(changedHouse);

        Assert.Equal(DemoMap.LocationFor(changedHouse.Id), changedLocation);
        Assert.NotEqual(originalLocation.Latitude, changedLocation.Latitude);
        Assert.NotEqual(originalLocation.Longitude, changedLocation.Longitude);
    }
}
