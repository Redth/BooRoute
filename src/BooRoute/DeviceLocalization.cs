using System.Globalization;
using Microsoft.Maui.Handlers;

namespace BooRoute;

internal static class DeviceLocalization
{
    public static void Configure()
    {
#if ANDROID
        var language = Android.App.Application.Context.Resources?.Configuration?.Locales?.Get(0)?.ToLanguageTag();
#elif IOS || MACCATALYST
        var language = Foundation.NSBundle.MainBundle.PreferredLocalizations.FirstOrDefault();
#else
        var language = CultureInfo.CurrentUICulture.Name;
#endif
        if (!string.IsNullOrEmpty(language))
        {
            var culture = CultureInfo.GetCultureInfo(language);
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        TimePickerHandler.Mapper.AppendToMapping(nameof(ITimePicker.Time), UpdateTimePicker);
        TimePickerHandler.Mapper.AppendToMapping(nameof(ITimePicker.Format), UpdateTimePicker);
    }

    public static string FormatTime(TimeOnly time)
    {
#if ANDROID
        using var formatter = Android.Text.Format.DateFormat.GetTimeFormat(Android.App.Application.Context)
            ?? throw new InvalidOperationException("Android did not provide a time formatter.");
        using var utc = Java.Util.TimeZone.GetTimeZone("UTC")
            ?? throw new InvalidOperationException("Android did not provide the UTC time zone.");
        formatter.TimeZone = utc;
        using var date = new Java.Util.Date((long)time.ToTimeSpan().TotalMilliseconds);
        return formatter.Format(date);
#elif IOS || MACCATALYST
        using var formatter = new Foundation.NSDateFormatter
        {
            DateStyle = Foundation.NSDateFormatterStyle.None,
            TimeStyle = Foundation.NSDateFormatterStyle.Short,
            Locale = Foundation.NSLocale.CurrentLocale,
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
            nativePicker.Locale = Foundation.NSLocale.CurrentLocale;
#elif MACCATALYST
        handler.PlatformView.Locale = Foundation.NSLocale.CurrentLocale;
#endif
    }
}
