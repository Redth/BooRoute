using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using BooRoute.Core;
using BooRoute.Core.Localization;
using BooRoute.Pages;
using Microsoft.Extensions.Logging;

namespace BooRoute.ViewModels;

public sealed class AsyncCommand(Func<object?, Task> execute) : ICommand
{
    private bool running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !running;

    public async void Execute(object? parameter)
    {
        if (running)
            return;
        running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute(parameter);
        }
        finally
        {
            running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

public abstract class ViewModelBase(ILoggerFactory logging, AppText text) : INotifyPropertyChanged
{
    private string errorMessage = "";
    private bool isBusy;
    public string ErrorMessage { get => errorMessage; protected set => Set(ref errorMessage, value); }
    public bool IsBusy { get => isBusy; private set => Set(ref isBusy, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public AppText Text { get; } = text;

    protected void Changed([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        Changed(name);
        return true;
    }

    protected async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
                                      JsonException or ArgumentException or InvalidOperationException)
        {
            logging.CreateLogger(GetType()).LogError(error, "BooRoute action failed");
            ErrorMessage = error switch
            {
                JsonException or InvalidDataException => Text.Strings.SavedDataInvalid,
                IOException or UnauthorizedAccessException => Text.Strings.StorageFailed,
                _ => Text.Strings.ActionFailed
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class HouseCard(House house, bool visited, DateTimeOffset localNow, AppText text)
{
    public House House { get; } = house;
    public string Nickname => House.Nickname;
    public string Address => House.Address;
    public string Emoji => House.ScareLevel == ScareLevel.Scary ? "🎃" : "🏠";
    public string Hours => text.Hours(House.OpensAt, House.ClosesAt);
    public string Status => House.Status switch
    {
        HouseStatus.OutOfTreats => text.Strings.OutOfTreats,
        HouseStatus.Closed => text.Strings.Closed,
        _ => House.IsWelcoming(localNow) ? text.Strings.LightsOnWelcome : text.Strings.LightsOnOutsideHours
    };
    public Color StatusColor => House.IsWelcoming(localNow) ? Ui.Mint : Ui.Muted;
    public string Features => string.Join(" · ", new[]
    {
        text.ScareName(House.ScareLevel),
        House.QuietVisits ? text.Strings.QuietVisits : null,
        House.StepFreeAccess ? text.Strings.StepFree : null
    }.Where(value => value is not null));
    public string VisitStatus => visited ? text.Strings.Visited : "";
    public string EditDescription => text.Strings.EditHouseDescriptionFormat(Nickname);
    public string ViewDescription => text.Strings.ViewHouseDescriptionFormat(Nickname);
}

public enum NeighborhoodMode { Explore, Visited, Host }

public sealed class NeighborhoodViewModel : ViewModelBase
{
    private readonly INeighborhoodBackend backend;
    private readonly TimeProvider clock;
    private readonly ILoggerFactory logging;
    private NeighborhoodSnapshot? snapshot;
    private bool welcomingOnly;
    private bool quietOnly;
    private int scareIndex;
    private string summary;

    public NeighborhoodMode Mode { get; }
    public ObservableCollection<HouseCard> Houses { get; } = [];
    public ICommand OpenHouseCommand { get; }
    public ICommand AddHouseCommand { get; }
    public ICommand RefreshCommand { get; }
    public string[] ScareOptions =>
        [Text.Strings.EveryScareLevel, .. Enum.GetValues<ScareLevel>().Select(Text.ScareName)];
    public string Summary { get => summary; private set => Set(ref summary, value); }
    public bool WelcomingOnly
    {
        get => welcomingOnly;
        set { if (Set(ref welcomingOnly, value)) ApplyFilters(); }
    }
    public bool QuietOnly
    {
        get => quietOnly;
        set { if (Set(ref quietOnly, value)) ApplyFilters(); }
    }
    public int ScareIndex
    {
        get => scareIndex;
        set { if (Set(ref scareIndex, value)) ApplyFilters(); }
    }

    public NeighborhoodViewModel(INeighborhoodBackend backend, TimeProvider clock,
        ILoggerFactory logging, AppText text, NeighborhoodMode mode) : base(logging, text)
    {
        this.backend = backend;
        this.clock = clock;
        this.logging = logging;
        Mode = mode;
        summary = Text.Strings.InitialSummary;
        RefreshCommand = new AsyncCommand(_ => RefreshAsync());
        OpenHouseCommand = new AsyncCommand(parameter => RunAsync(async () =>
        {
            if (parameter is not HouseCard card)
                throw new ArgumentException(Text.Strings.SelectHouse);
            Page page = Mode == NeighborhoodMode.Host
                ? new HostEditorPage(new(backend, clock, logging, Text, card.House))
                : new HouseDetailsPage(new(backend, clock, logging, Text, card.House));
            await Shell.Current.Navigation.PushAsync(page);
        }));
        AddHouseCommand = new AsyncCommand(_ => RunAsync(() =>
            Shell.Current.Navigation.PushAsync(new HostEditorPage(new(backend, clock, logging, Text)))));
        backend.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(() => _ = RefreshAsync());
    }

    public Task RefreshAsync() => RunAsync(async () =>
    {
        snapshot = await backend.GetSnapshotAsync();
        ApplyFilters();
    });

    private void ApplyFilters()
    {
        if (snapshot is null)
            return;
        var now = clock.GetLocalNow();
        ScareLevel? scare = scareIndex is >= 1 and <= 3 ? (ScareLevel)(scareIndex - 1) : null;
        var filtered = HouseFilters.Apply(snapshot.Houses, now,
            Mode == NeighborhoodMode.Explore && WelcomingOnly,
            Mode == NeighborhoodMode.Explore ? scare : null,
            Mode == NeighborhoodMode.Explore && QuietOnly,
            Mode == NeighborhoodMode.Visited ? snapshot.Visited : null);
        Houses.Clear();
        foreach (var house in filtered.OrderByDescending(house => house.IsWelcoming(now)).ThenBy(house => house.Nickname))
            Houses.Add(new(house, snapshot.Visited.Contains(house.Id), now, Text));
        Summary = Text.Summary(Houses.Count, Houses.Count(card => card.House.IsWelcoming(now)));
    }
}

public sealed class HouseDetailsViewModel : ViewModelBase
{
    private bool visited;
    public House House { get; }
    public HouseCard Card { get; }
    public string VisitButtonText => visited ? Text.Strings.UndoVisited : Text.Strings.MarkVisited;
    public ICommand ToggleVisitedCommand { get; }

    public HouseDetailsViewModel(INeighborhoodBackend backend, TimeProvider clock,
        ILoggerFactory logging, AppText text, House house) : base(logging, text)
    {
        House = house;
        Card = new(house, false, clock.GetLocalNow(), Text);
        ToggleVisitedCommand = new AsyncCommand(_ => RunAsync(async () =>
        {
            await backend.SetVisitedAsync(House.Id, !visited);
            visited = !visited;
            Changed(nameof(VisitButtonText));
        }));
        Backend = backend;
    }

    private INeighborhoodBackend Backend { get; }
    public Task LoadAsync() => RunAsync(async () =>
    {
        visited = (await Backend.GetSnapshotAsync()).Visited.Contains(House.Id);
        Changed(nameof(VisitButtonText));
    });
}

public sealed class HostEditorViewModel : ViewModelBase
{
    private readonly Guid id;
    private string nickname = "";
    private string address = "";
    private string description = "";
    private TimeSpan opensAt;
    private TimeSpan closesAt;
    private int scareIndex;
    private int statusIndex;
    private bool quietVisits;
    private bool stepFreeAccess;

    public string Heading { get; }
    public string Nickname { get => nickname; set => Set(ref nickname, value); }
    public string Address { get => address; set => Set(ref address, value); }
    public string Description { get => description; set => Set(ref description, value); }
    public TimeSpan OpensAt { get => opensAt; set => Set(ref opensAt, value); }
    public TimeSpan ClosesAt { get => closesAt; set => Set(ref closesAt, value); }
    public int ScareIndex { get => scareIndex; set => Set(ref scareIndex, value); }
    public int StatusIndex { get => statusIndex; set => Set(ref statusIndex, value); }
    public bool QuietVisits { get => quietVisits; set => Set(ref quietVisits, value); }
    public bool StepFreeAccess { get => stepFreeAccess; set => Set(ref stepFreeAccess, value); }
    public string[] ScareOptions => Enum.GetValues<ScareLevel>().Select(Text.ScareName).ToArray();
    public string[] StatusOptions => Enum.GetValues<HouseStatus>().Select(Text.StatusName).ToArray();
    public ICommand SaveCommand { get; }

    public HostEditorViewModel(INeighborhoodBackend backend, TimeProvider clock,
        ILoggerFactory logging, AppText text, House? house = null) : base(logging, text)
    {
        id = house?.Id ?? Guid.NewGuid();
        Heading = house is null ? Text.Strings.NewHouseHeading : Text.Strings.EditHouseHeading;
        Nickname = house?.Nickname ?? "";
        Address = house?.Address ?? "";
        Description = house?.Description ?? "";
        OpensAt = house?.OpensAt.ToTimeSpan() ?? TimeOnly.FromDateTime(clock.GetLocalNow().AddHours(-1).DateTime).ToTimeSpan();
        ClosesAt = house?.ClosesAt.ToTimeSpan() ?? TimeOnly.FromDateTime(clock.GetLocalNow().AddHours(3).DateTime).ToTimeSpan();
        ScareIndex = (int)(house?.ScareLevel ?? ScareLevel.Friendly);
        StatusIndex = (int)(house?.Status ?? HouseStatus.LightsOn);
        QuietVisits = house?.QuietVisits ?? false;
        StepFreeAccess = house?.StepFreeAccess ?? false;
        SaveCommand = new AsyncCommand(_ => RunAsync(async () =>
        {
            var updated = new House(id, Nickname.Trim(), Address.Trim(), Description.Trim(),
                TimeOnly.FromTimeSpan(OpensAt), TimeOnly.FromTimeSpan(ClosesAt),
                (ScareLevel)ScareIndex, (HouseStatus)StatusIndex, QuietVisits, StepFreeAccess);
            var errors = updated.Validate();
            if (errors.Count != 0)
            {
                ErrorMessage = string.Join(Environment.NewLine, errors);
                return;
            }
            await backend.SaveHouseAsync(updated);
            await Shell.Current.Navigation.PopAsync();
        }));
    }
}
