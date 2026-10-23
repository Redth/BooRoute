using System.Globalization;

namespace BooRoute.Core.Localization;

public interface ILocaleStore
{
    string Read();
    void Write(string locale);
}

public sealed class LocaleSettings
{
    private readonly ILocaleStore store;
    private readonly CultureInfo deviceCulture;
    private readonly CultureInfo deviceUICulture;

    public static IReadOnlyList<string> SupportedLocales { get; } =
        Array.AsReadOnly(new[] { "", "en-CA", "fr-CA" });

    public string SelectedLocale { get; private set; }
    public CultureInfo Culture => SelectedLocale.Length == 0
        ? deviceCulture : CultureInfo.GetCultureInfo(SelectedLocale);
    public CultureInfo UICulture => SelectedLocale.Length == 0
        ? deviceUICulture : CultureInfo.GetCultureInfo(SelectedLocale);

    public LocaleSettings(ILocaleStore store, CultureInfo deviceCulture, CultureInfo deviceUICulture)
    {
        this.store = store;
        this.deviceCulture = deviceCulture;
        this.deviceUICulture = deviceUICulture;
        SelectedLocale = store.Read();
        if (!SupportedLocales.Contains(SelectedLocale))
            throw new InvalidDataException("The saved locale is not supported.");
    }

    public void Select(string locale)
    {
        if (!SupportedLocales.Contains(locale))
            throw new ArgumentException("Choose a supported locale.", nameof(locale));
        store.Write(locale);
        SelectedLocale = locale;
    }
}
