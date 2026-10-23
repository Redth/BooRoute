using BooRoute.ViewModels;
using BooRoute.Core.Localization;

namespace BooRoute.Pages;

public sealed class HostEditorPage : ContentPage
{
    public HostEditorPage(HostEditorViewModel viewModel)
    {
        BindingContext = viewModel;
        var text = viewModel.Text.Strings;
        Title = text.DemoHouse;
        BackgroundColor = Ui.Midnight;
        HideSoftInputOnTapped = true;
        var fields = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 14,
            Children =
            {
                Ui.Heading("heading_host.png", viewModel.Heading),
                Ui.DemoBanner(text.HostDemoBanner),
                Ui.Text(text.HouseNickname, 15, bold: true),
                Entry("Nickname", text.NicknamePlaceholder, text.HouseNickname, "NicknameEntry", 60),
                Ui.Text(text.FictionalAddress, 15, bold: true),
                Entry("Address", text.AddressPlaceholder, text.FictionalAddress, "AddressEntry", 120),
                Ui.Text(text.DescriptionHeading, 15, bold: true),
                DescriptionEditor(text),
                Ui.Text(text.DailyHours, 15, bold: true),
                TimeFields(text),
                Ui.Text(text.ScareLevel, 15, bold: true),
                Ui.Picker(text.ScareLevel, nameof(viewModel.ScareOptions), nameof(viewModel.ScareIndex), "HouseScarePicker"),
                Ui.Text(text.PorchStatus, 15, bold: true),
                Ui.Picker(text.PorchStatus, nameof(viewModel.StatusOptions), nameof(viewModel.StatusIndex), "HouseStatusPicker"),
                Ui.Toggle(text.QuietVisitsAvailable, nameof(viewModel.QuietVisits), "QuietVisitsSwitch"),
                Ui.Toggle(text.StepFreeAccess, nameof(viewModel.StepFreeAccess), "StepFreeSwitch"),
                Ui.Errors(),
                Ui.Button(text.SaveHouse, "SaveHouseButton", viewModel.SaveCommand),
                Ui.Text(text.StorageNotice, 13, Ui.Muted)
            }
        };
        Content = new ScrollView { Content = fields };
    }

    private static Entry Entry(string property, string placeholder, string description, string id, int maximumLength)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            AutomationId = id,
            MaxLength = maximumLength,
            TextColor = Ui.Cream,
            PlaceholderColor = Ui.Muted,
            BackgroundColor = Ui.Panel,
            MinimumHeightRequest = 50,
            FontSize = 16,
            ReturnType = ReturnType.Next
        };
        entry.SetBinding(Microsoft.Maui.Controls.Entry.TextProperty, property);
        SemanticProperties.SetDescription(entry, description);
        return entry;
    }

    private static Editor DescriptionEditor(AppTextLocalized text)
    {
        var editor = new Editor
        {
            Placeholder = text.DescriptionPlaceholder,
            AutomationId = "DescriptionEditor",
            MaxLength = 500,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 105,
            TextColor = Ui.Cream,
            PlaceholderColor = Ui.Muted,
            BackgroundColor = Ui.Panel,
            FontSize = 16
        };
        editor.SetBinding(Editor.TextProperty, nameof(HostEditorViewModel.Description));
        SemanticProperties.SetDescription(editor, text.DescriptionAccessibility);
        return editor;
    }

    private static View TimeFields(AppTextLocalized text)
    {
        var opens = new TimePicker { AutomationId = "OpeningTimePicker", TextColor = Ui.Cream, Format = "t" };
        opens.SetBinding(TimePicker.TimeProperty, nameof(HostEditorViewModel.OpensAt));
        SemanticProperties.SetDescription(opens, text.OpeningTime);
        var closes = new TimePicker { AutomationId = "ClosingTimePicker", TextColor = Ui.Cream, Format = "t" };
        closes.SetBinding(TimePicker.TimeProperty, nameof(HostEditorViewModel.ClosesAt));
        SemanticProperties.SetDescription(closes, text.ClosingTime);
        var grid = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 18 };
        grid.Add(new VerticalStackLayout { Children = { Ui.Text(text.Opens, 13, Ui.Muted), opens } });
        grid.Add(new VerticalStackLayout { Children = { Ui.Text(text.Closes, 13, Ui.Muted), closes } }, 1);
        return grid;
    }
}
