namespace BooRoute.Core;

/// <summary>
/// Places the fictional demo houses on the map.
/// Addresses in BooRoute are made up and cannot be geocoded, so every house gets a
/// stable, made-up position derived from its id. All positions fall inside a public
/// park (Berlin's Tiergarten), so no pin ever points at a real home.
/// </summary>
public static class DemoMap
{
    public const double CenterLatitude = 52.5140;
    public const double CenterLongitude = 13.3500;

    // Roughly ±400 m north/south and ±450 m east/west around the center.
    public const double LatitudeSpread = 0.0036;
    public const double LongitudeSpread = 0.0066;

    public static (double Latitude, double Longitude) LocationFor(House house) => LocationFor(house.Id);

    public static (double Latitude, double Longitude) LocationFor(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes);
        var x = BitConverter.ToUInt32(bytes[..4]) / (double)uint.MaxValue;
        var y = BitConverter.ToUInt32(bytes[4..8]) / (double)uint.MaxValue;
        return (CenterLatitude + (y * 2 - 1) * LatitudeSpread,
                CenterLongitude + (x * 2 - 1) * LongitudeSpread);
    }
}
