using BooRoute.Core.Localization;

namespace BooRoute.Core;

public static class DemoSeed
{
    public static DemoState Create(DateTimeOffset localNow)
    {
        var text = AppText.Default.Strings;
        string[] names =
        [
            text.DemoName0, text.DemoName1, text.DemoName2, text.DemoName3, text.DemoName4,
            text.DemoName5, text.DemoName6, text.DemoName7, text.DemoName8, text.DemoName9
        ];
        string[] descriptions =
        [
            text.DemoDescription0, text.DemoDescription1, text.DemoDescription2, text.DemoDescription3,
            text.DemoDescription4, text.DemoDescription5, text.DemoDescription6, text.DemoDescription7,
            text.DemoDescription8, text.DemoDescription9
        ];
        var opens = TimeOnly.FromDateTime(localNow.AddHours(-2).DateTime);
        var closes = TimeOnly.FromDateTime(localNow.AddHours(4).DateTime);
        var houses = Enumerable.Range(0, names.Length).Select(index => new House(
            Guid.NewGuid(), names[index], text.DemoAddressFormat(index + 1),
            descriptions[index], opens, closes, (ScareLevel)(index % 3),
            index is 3 or 7 ? HouseStatus.OutOfTreats : index == 9 ? HouseStatus.Closed : HouseStatus.LightsOn,
            index is 0 or 1 or 2 or 4 or 6, index % 3 != 1)).ToList();
        return new(houses, []);
    }
}
