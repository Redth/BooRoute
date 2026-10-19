using BooRoute.ViewModels;

namespace BooRoute.Pages;

public sealed class NeighborhoodPage : ContentPage
{
    private readonly NeighborhoodViewModel viewModel;
    private readonly IDispatcherTimer timer;

    public NeighborhoodPage(NeighborhoodViewModel viewModel)
    {
        this.viewModel = viewModel;
        BindingContext = viewModel;
        BackgroundColor = Ui.Midnight;
        SafeAreaEdges = SafeAreaEdges.Container;
        Title = viewModel.Mode switch
        {
            NeighborhoodMode.Host => "Demo host",
            NeighborhoodMode.Visited => "My visits",
            _ => "Explore"
        };
        var heading = viewModel.Mode switch
        {
            NeighborhoodMode.Host => "🏠 Your porch, your rules.",
            NeighborhoodMode.Visited => "🎃 A trail of good treats.",
            _ => "🎃 BooRoute"
        };
        var subtitle = viewModel.Mode switch
        {
            NeighborhoodMode.Host => "Demo hosting only. No accounts or verified ownership.",
            NeighborhoodMode.Visited => "Your visits stay on this device. Nobody else sees them.",
            _ => "Find the doors worth knocking on."
        };
        var header = new VerticalStackLayout
        {
            Spacing = 10,
            Margin = new Thickness(22, 12, 22, 10),
            Children = { Ui.Text(heading, 29, bold: true), Ui.Text(subtitle, 14, Ui.Muted), Ui.DemoBanner() }
        };
        if (viewModel.Mode == NeighborhoodMode.Explore)
        {
            header.Add(Ui.Toggle("Welcoming visitors now", nameof(viewModel.WelcomingOnly), "WelcomingOnlySwitch"));
            header.Add(Ui.Toggle("Quiet visits only", nameof(viewModel.QuietOnly), "QuietOnlySwitch"));
            header.Add(Ui.Picker("Scare level", nameof(viewModel.ScareOptions), nameof(viewModel.ScareIndex), "ScareFilterPicker"));
        }
        if (viewModel.Mode == NeighborhoodMode.Host)
            header.Add(Ui.Button("+ Register a demo house", "AddHouseButton", viewModel.AddHouseCommand));
        var summary = Ui.Bound(nameof(viewModel.Summary), 14, Ui.Muted);
        summary.AutomationId = "HouseCountLabel";
        header.Add(summary);
        header.Add(Ui.Errors());
        header.Add(Ui.Button("Refresh neighborhood", "RefreshButton", viewModel.RefreshCommand, secondary: true));

        var collection = new CollectionView
        {
            AutomationId = "HousesCollection",
            SelectionMode = SelectionMode.None,
            Header = header,
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            EmptyView = new VerticalStackLayout
            {
                Padding = 26,
                Spacing = 12,
                Children =
                {
                    Ui.Text("No houses here… yet.", 24, bold: true),
                    Ui.Text(viewModel.Mode == NeighborhoodMode.Visited
                        ? "Open a house in Explore and mark it as visited."
                        : "Try a different filter or register a demo house.", 16, Ui.Muted)
                }
            },
            ItemTemplate = new DataTemplate(() => BuildHouseCard(viewModel))
        };
        collection.SetBinding(ItemsView.ItemsSourceProperty, nameof(viewModel.Houses));
        Content = collection;
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(30);
        timer.Tick += async (_, _) => await viewModel.RefreshAsync();
    }

    private static View BuildHouseCard(NeighborhoodViewModel viewModel)
    {
        var status = Ui.Bound(nameof(HouseCard.Status), 14, bold: true);
        status.SetBinding(Label.TextColorProperty, nameof(HouseCard.StatusColor));
        var top = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        var nickname = Ui.Bound(nameof(HouseCard.Nickname), 22, bold: true);
        SemanticProperties.SetHeadingLevel(nickname, SemanticHeadingLevel.Level2);
        top.Add(nickname);
        top.Add(Ui.Bound(nameof(HouseCard.Emoji), 32), 1);
        var button = Ui.Button(viewModel.Mode == NeighborhoodMode.Host ? "Edit house & status" : "Visit details",
            "OpenHouseButton", viewModel.OpenHouseCommand, secondary: true);
        button.BackgroundColor = Color.FromArgb("#3A2B4F");
        button.SetBinding(Button.CommandParameterProperty, ".");
        button.SetBinding(SemanticProperties.DescriptionProperty,
            new Binding(nameof(HouseCard.Nickname), stringFormat: viewModel.Mode == NeighborhoodMode.Host ? "Edit {0}" : "View {0}"));
        var content = new VerticalStackLayout
        {
            Spacing = 9,
            Children =
            {
                top, Ui.Bound(nameof(HouseCard.Address), 14, Ui.Muted), status,
                Ui.Bound(nameof(HouseCard.Hours), 15),
                Ui.Bound(nameof(HouseCard.Features), 13, Ui.Muted),
                Ui.Bound(nameof(HouseCard.VisitStatus), 14, Ui.Mint), button
            }
        };
        var card = Ui.Card(content);
        card.Margin = new Thickness(22, 0, 22, 14);
        return card;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        timer.Start();
        await viewModel.RefreshAsync();
    }

    protected override void OnDisappearing()
    {
        timer.Stop();
        base.OnDisappearing();
    }
}
