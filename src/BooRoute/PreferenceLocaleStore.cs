using BooRoute.Core.Localization;

namespace BooRoute;

internal sealed class PreferenceLocaleStore : ILocaleStore
{
    private const string Key = "locale";

    public string Read() => Preferences.Default.Get(Key, "");
    public void Write(string locale) => Preferences.Default.Set(Key, locale);
}
