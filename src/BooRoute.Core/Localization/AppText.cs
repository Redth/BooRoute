using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BooRoute.Core.Localization;

public sealed class AppText(AppTextLocalized strings)
{
    private static readonly ResourceManagerStringLocalizerFactory DefaultFactory =
        new(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);

    // Domain validation also runs outside DI (including when loading saved data).
    public static AppText Default { get; } = new(new(new StringLocalizer<AppText>(DefaultFactory)));

    public AppTextLocalized Strings { get; } = strings;
    public Func<TimeOnly, string> FormatTime { get; init; } =
        static time => time.ToString("t", CultureInfo.CurrentCulture);

    public string Hours(TimeOnly opens, TimeOnly closes) =>
        Strings.HoursFormat(FormatTime(opens), FormatTime(closes));

    public string Summary(int houses, int welcoming) => Strings.SummaryFormat(
        houses switch
        {
            0 => Strings.HouseCountNone,
            1 => Strings.HouseCountOne,
            _ => Strings.HouseCountManyFormat(houses)
        },
        welcoming switch
        {
            0 => Strings.WelcomingCountNone,
            1 => Strings.WelcomingCountOne,
            _ => Strings.WelcomingCountManyFormat(welcoming)
        });

    public string ScareName(ScareLevel level) => level switch
    {
        ScareLevel.Friendly => Strings.ScareFriendly,
        ScareLevel.Spooky => Strings.ScareSpooky,
        ScareLevel.Scary => Strings.ScareScary,
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };

    public string StatusName(HouseStatus status) => status switch
    {
        HouseStatus.LightsOn => Strings.LightsOn,
        HouseStatus.OutOfTreats => Strings.OutOfTreats,
        HouseStatus.Closed => Strings.Closed,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
