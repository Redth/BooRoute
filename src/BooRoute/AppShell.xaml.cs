using BooRoute.Core;
using BooRoute.Pages;
using BooRoute.ViewModels;
using Microsoft.Extensions.Logging;

namespace BooRoute;

public partial class AppShell : Shell
{
    private readonly List<NeighborhoodViewModel> viewModels = [];
    private readonly MapViewModel? mapViewModel;

    public AppShell(INeighborhoodBackend backend, TimeProvider clock, ILoggerFactory logging)
    {
        InitializeComponent();
        var tabs = new TabBar();
        foreach (var mode in Enum.GetValues<NeighborhoodMode>())
        {
            var viewModel = new NeighborhoodViewModel(backend, clock, logging, mode);
            viewModels.Add(viewModel);
            var page = new NeighborhoodPage(viewModel);
            var tab = new Tab { Title = page.Title, Route = mode.ToString().ToLowerInvariant() };
            tab.Items.Add(new ShellContent { Title = page.Title, Content = page });
            tabs.Items.Add(tab);
        }
#if !WINDOWS
        // .NET MAUI Maps has no Windows implementation.
        mapViewModel = new MapViewModel(backend, clock, logging);
        var mapPage = new MapPage(mapViewModel);
        var mapTab = new Tab { Title = mapPage.Title, Route = "map" };
        mapTab.Items.Add(new ShellContent { Title = mapPage.Title, Content = mapPage });
        tabs.Items.Insert(1, mapTab);
#endif
        Items.Add(tabs);
    }

    public async Task RefreshAsync()
    {
        foreach (var viewModel in viewModels)
            await viewModel.RefreshAsync();
        if (mapViewModel is not null)
            await mapViewModel.RefreshAsync();
    }
}
