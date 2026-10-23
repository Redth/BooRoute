using BooRoute.Core.Localization;
using BooRoute.ViewModels;
using Microsoft.Extensions.Logging;

namespace BooRoute.Pages;

public sealed class SettingsPage : ContentPage
{
    public SettingsPage(LocaleSettings settings, AppText text, ILoggerFactory logging, Action applyLocale)
    {
        var viewModel = new SettingsViewModel(settings, text, logging, applyLocale);
        var strings = text.Strings;
        BindingContext = viewModel;
        Title = strings.Settings;
        BackgroundColor = Ui.Midnight;
        SafeAreaEdges = SafeAreaEdges.Container;
        var heading = Ui.Text(strings.Settings, 29, bold: true);
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        var preview = Ui.Text(strings.LocalePreviewFormat(text.FormatTime(new TimeOnly(17, 15))), 16, Ui.Muted);
        preview.AutomationId = "LocalePreviewLabel";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 18,
                Children =
                {
                    heading,
                    Ui.Text(strings.Locale, 18, bold: true),
                    Ui.Picker(strings.Locale,
                        Binding.Create(static (SettingsViewModel model) => model.Options),
                        Binding.Create(static (SettingsViewModel model) => model.SelectedIndex), "LocalePicker"),
                    preview,
                    Ui.Text(strings.LocaleNotice, 15, Ui.Muted),
                    Ui.Errors(),
                    Ui.Button(strings.ApplyLocale, "ApplyLocaleButton", viewModel.ApplyCommand)
                }
            }
        };
    }

    internal sealed class SettingsViewModel : ViewModelBase
    {
        private int selectedIndex;
        public string[] Options { get; }
        public int SelectedIndex { get => selectedIndex; set => Set(ref selectedIndex, value); }
        public System.Windows.Input.ICommand ApplyCommand { get; }

        public SettingsViewModel(LocaleSettings settings, AppText text, ILoggerFactory logging, Action applyLocale)
            : base(logging, text)
        {
            Options = [text.Strings.SystemLocale, "English (Canada)", "Français (Canada)"];
            SelectedIndex = LocaleSettings.SupportedLocales.ToList().IndexOf(settings.SelectedLocale);
            ApplyCommand = new AsyncCommand(_ => RunAsync(() =>
            {
                if (SelectedIndex < 0 || SelectedIndex >= LocaleSettings.SupportedLocales.Count)
                    throw new ArgumentException("Choose a supported locale.");
                settings.Select(LocaleSettings.SupportedLocales[SelectedIndex]);
                applyLocale();
                return Task.CompletedTask;
            }));
        }
    }
}
