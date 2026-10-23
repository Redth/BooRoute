using BooRoute.ViewModels;

namespace BooRoute.Pages;

public sealed class HouseDetailsPage : ContentPage
{
    private readonly HouseDetailsViewModel viewModel;

    public HouseDetailsPage(HouseDetailsViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        Title = "House details";
        BackgroundColor = Ui.Midnight;
        SafeAreaEdges = SafeAreaEdges.Container;
        var house = viewModel.House;
        var card = viewModel.Card;
        var visit = Ui.Button("", "ToggleVisitedButton", viewModel.ToggleVisitedCommand);
        visit.SetBinding(Button.TextProperty, nameof(viewModel.VisitButtonText));
        var status = Ui.Text(card.Status, 17, card.IsWelcoming ? Ui.Mint : Ui.Muted, true);
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
                    Ui.DemoBanner("FICTIONAL DESTINATION · Demo neighborhood only"),
                    status, Ui.Text($"Daily welcoming hours: {card.Hours}", 17),
                    Ui.Text(house.Description, 18),
                    Ui.Card(new VerticalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            Ui.Text($"Scare level: {house.ScareLevel}", 17, bold: true),
                            Ui.Text(house.QuietVisits ? "✓ Quiet visits available" : "Quiet visits not indicated", 16),
                            Ui.Text(house.StepFreeAccess ? "✓ Step-free access indicated" : "Step-free access not indicated", 16),
                            Ui.Text("Accessibility details are supplied by the demo host, not independently verified.", 13, Ui.Muted)
                        }
                    }),
                    Ui.Errors(), visit,
                    Ui.Text("Your visit history stays on this device. No accounts. No tracking.", 13, Ui.Muted)
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
