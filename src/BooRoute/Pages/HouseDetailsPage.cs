using BooRoute.ViewModels;

namespace BooRoute.Pages;

public sealed class HouseDetailsPage : ContentPage
{
    private readonly HouseDetailsViewModel viewModel;

    public HouseDetailsPage(HouseDetailsViewModel viewModel)
    {
        this.viewModel = viewModel;
        var text = viewModel.Text.Strings;
        BindingContext = viewModel;
        Title = text.HouseDetails;
        BackgroundColor = Ui.Midnight;
        SafeAreaEdges = SafeAreaEdges.Container;
        var house = viewModel.House;
        var card = viewModel.Card;
        var visit = Ui.Button("", "ToggleVisitedButton", viewModel.ToggleVisitedCommand);
        visit.SetBinding(Button.TextProperty, nameof(viewModel.VisitButtonText));
        var status = Ui.Text(card.Status, 17, card.StatusColor, true);
        status.AutomationId = "HouseStatusLabel";
        var name = Ui.Text(house.Nickname, 32, bold: true);
        name.AutomationId = "HouseNicknameLabel";
        SemanticProperties.SetHeadingLevel(name, SemanticHeadingLevel.Level1);
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 18,
                Children =
                {
                    Ui.Text(card.Emoji, 72), name, Ui.Text(house.Address, 16, Ui.Muted),
                    Ui.DemoBanner(text.FictionalDestination),
                    status, Ui.Text(text.DailyHoursDescriptionFormat(card.Hours), 17),
                    Ui.Text(house.Description, 18),
                    Ui.Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            Ui.Text(text.ScareDescriptionFormat(viewModel.Text.ScareName(house.ScareLevel)), 17, bold: true),
                            Ui.Text(house.QuietVisits ? text.QuietAvailable : text.QuietNotIndicated, 16),
                            Ui.Text(house.StepFreeAccess ? text.StepFreeIndicated : text.StepFreeNotIndicated, 16),
                            Ui.Text(text.AccessibilityDisclaimer, 13, Ui.Muted)
                        }
                    }),
                    Ui.Errors(), visit,
                    Ui.Text(text.VisitPrivacy, 13, Ui.Muted)
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
