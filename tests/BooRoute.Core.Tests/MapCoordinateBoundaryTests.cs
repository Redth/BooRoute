namespace BooRoute.Core.Tests;

public sealed class MapCoordinateBoundaryTests
{
    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 52.5104, 13.3434)]
    [InlineData("ffffffff-ffff-ffff-0000-000000000000", 52.5176, 13.3566)]
    [InlineData("ffffffff-0000-0000-0000-000000000000", 52.5104, 13.3566)]
    [InlineData("00000000-ffff-ffff-0000-000000000000", 52.5176, 13.3434)]
    public void GuidExtremaMapToExpectedCorners(
        string id, double expectedLatitude, double expectedLongitude)
    {
        var (latitude, longitude) = DemoMap.LocationFor(Guid.Parse(id));

        Assert.Equal(expectedLatitude, latitude, precision: 10);
        Assert.Equal(expectedLongitude, longitude, precision: 10);
    }
}
