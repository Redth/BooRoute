using System.Globalization;
using BooRoute.Core.Localization;
using Microsoft.Maui.Handlers;

namespace BooRoute;

internal static class DeviceLocalization
{
    private static string? localeOverride;

    public static LocaleSettings Configure()
    {
#if ANDROID
        var language = Android.App.Application.Context.Resources?.Configuration?.Locales?.Get(0)?.ToLanguageTag();
#elif IOS || MACCATALYST
        var language = Foundation.NSBundle.MainBundle.PreferredLocalizations.FirstOrDefault();
#else
        var language = CultureInfo.CurrentUICulture.Name;
#endif
        var settings = new LocaleSettings(new PreferenceLocaleStore(), CultureInfo.CurrentCulture,
            string.IsNullOrEmpty(language) ? CultureInfo.CurrentUICulture : CultureInfo.GetCultureInfo(language));
        Apply(settings);

        TimePickerHandler.Mapper.AppendToMapping(nameof(ITimePicker.Time), UpdateTimePicker);
        TimePickerHandler.Mapper.AppendToMapping(nameof(ITimePicker.Format), UpdateTimePicker);
        return settings;
    }

    public static void Apply(LocaleSettings settings)
    {
        localeOverride = settings.SelectedLocale.Length == 0 ? null : settings.SelectedLocale;
        AppText.UseCultures(settings.Culture, settings.UICulture);
        CultureInfo.CurrentCulture = settings.Culture;
        CultureInfo.CurrentUICulture = settings.UICulture;
        CultureInfo.DefaultThreadCurrentCulture = settings.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = settings.UICulture;
#if ANDROID
        AndroidX.AppCompat.App.AppCompatDelegate.ApplicationLocales =
            AndroidX.Core.OS.LocaleListCompat.ForLanguageTags(settings.SelectedLocale)
            ?? throw new InvalidOperationException("Android did not provide the selected app locale.");
#endif
    }

    public static string FormatTime(TimeOnly time)
    {
#if ANDROID
        using var locale = Java.Util.Locale.ForLanguageTag(localeOverride ?? CultureInfo.CurrentCulture.Name);
        using var formatter = (localeOverride is null
            ? Android.Text.Format.DateFormat.GetTimeFormat(Android.App.Application.Context)
            : Java.Text.DateFormat.GetTimeInstance(Java.Text.DateFormat.Short, locale))
            ?? throw new InvalidOperationException("Android did not provide a time formatter.");
        using var utc = Java.Util.TimeZone.GetTimeZone("UTC")
            ?? throw new InvalidOperationException("Android did not provide the UTC time zone.");
        formatter.TimeZone = utc;
        using var date = new Java.Util.Date((long)time.ToTimeSpan().TotalMilliseconds);
        return formatter.Format(date);
#elif IOS || MACCATALYST
        using var locale = new Foundation.NSLocale(localeOverride ?? Foundation.NSLocale.CurrentLocale.LocaleIdentifier);
        using var formatter = new Foundation.NSDateFormatter
        {
            DateStyle = Foundation.NSDateFormatterStyle.None,
            TimeStyle = Foundation.NSDateFormatterStyle.Short,
            Locale = locale,
            TimeZone = Foundation.NSTimeZone.FromName("UTC")
        };
        using var date = Foundation.NSDate.FromTimeIntervalSince1970(time.ToTimeSpan().TotalSeconds);
        return formatter.ToString(date);
#else
        return time.ToString("t", CultureInfo.CurrentCulture);
#endif
    }

    private static void UpdateTimePicker(ITimePickerHandler handler, ITimePicker picker)
    {
#if ANDROID || IOS
        // MAUI's field text uses .NET culture; the native dialog can use a user-overridden hour cycle.
        handler.PlatformView.Text = picker.Time is { } time ? FormatTime(TimeOnly.FromTimeSpan(time)) : "";
#endif
#if IOS
        if (handler.PlatformView.InputView is UIKit.UIDatePicker nativePicker)
            nativePicker.Locale = new Foundation.NSLocale(localeOverride ?? Foundation.NSLocale.CurrentLocale.LocaleIdentifier);
#elif MACCATALYST
        handler.PlatformView.Locale = new Foundation.NSLocale(localeOverride ?? Foundation.NSLocale.CurrentLocale.LocaleIdentifier);
#endif
    }
}
