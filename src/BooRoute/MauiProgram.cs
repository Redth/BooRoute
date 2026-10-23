using Microsoft.Extensions.Logging;
using BooRoute.Core;
using BooRoute.Core.Localization;

namespace BooRoute;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        DeviceLocalization.Configure();
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddStronglyTypedLocalizations();
        builder.Services.AddSingleton(provider => new AppText(provider.GetRequiredService<AppTextLocalized>())
        {
            FormatTime = DeviceLocalization.FormatTime
        });
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddSingleton<IDemoStateStore>(_ =>
            new JsonDemoStateStore(Path.Combine(FileSystem.AppDataDirectory, "neighborhood.json")));
        builder.Services.AddSingleton<INeighborhoodBackend, MockNeighborhoodBackend>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
