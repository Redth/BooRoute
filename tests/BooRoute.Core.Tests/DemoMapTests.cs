namespace BooRoute.Core.Tests;

public sealed class DemoMapTests
{
    [Fact]
    public void SameHouseAlwaysGetsTheSameLocation()
    {
        var id = Guid.NewGuid();
        Assert.Equal(DemoMap.LocationFor(id), DemoMap.LocationFor(id));
    }

    [Fact]
    public void LocationsStayInsideTheDemoArea()
    {
        for (var i = 0; i < 1000; i++)
        {
            var (latitude, longitude) = DemoMap.LocationFor(Guid.NewGuid());
            Assert.InRange(latitude, DemoMap.CenterLatitude - DemoMap.LatitudeSpread,
                DemoMap.CenterLatitude + DemoMap.LatitudeSpread);
            Assert.InRange(longitude, DemoMap.CenterLongitude - DemoMap.LongitudeSpread,
                DemoMap.CenterLongitude + DemoMap.LongitudeSpread);
        }
    }

    [Fact]
    public void SeededHousesGetDistinctLocations()
    {
        var houses = DemoSeed.Create(new DateTimeOffset(2026, 10, 31, 18, 0, 0, TimeSpan.Zero)).Houses;
        var locations = houses.Select(DemoMap.LocationFor).ToList();
        Assert.Equal(houses.Count, locations.Distinct().Count());
    }
}
