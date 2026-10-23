namespace BooRoute.Core.Tests;

public sealed class BackendCancellationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledSnapshotDoesNotReadOrChangeStateAndLaterSnapshotSucceeds(bool initialized)
    {
        var house = Sample();
        var original = new DemoState([house], []);
        var store = new CountingStore(original);
        var backend = new MockNeighborhoodBackend(store, TimeProvider.System);
        if (initialized)
            await backend.GetSnapshotAsync();
        var changed = 0;
        backend.Changed += (_, _) => changed++;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => backend.GetSnapshotAsync(cancellation.Token));

        Assert.Equal(initialized ? 1 : 0, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Same(original, store.State);
        Assert.Equal(house, Assert.Single(store.State.Houses));
        Assert.Empty(store.State.Visited);
        Assert.Equal(0, changed);

        var snapshot = await backend.GetSnapshotAsync();

        Assert.Equal(house, Assert.Single(snapshot.Houses));
        Assert.Empty(snapshot.Visited);
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Same(original, store.State);
        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task CancelledHouseSaveDoesNotChangeStateAndLaterSaveSucceeds()
    {
        var house = Sample();
        var original = new DemoState([house], [house.Id]);
        var store = new CountingStore(original);
        var backend = new MockNeighborhoodBackend(store, TimeProvider.System);
        await backend.GetSnapshotAsync();
        var updated = house with { Nickname = "Updated porch" };
        Assert.Empty(updated.Validate());
        var changed = 0;
        backend.Changed += (_, _) => changed++;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => backend.SaveHouseAsync(updated, cancellation.Token));

        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Same(original, store.State);
        Assert.Equal(house, Assert.Single(store.State.Houses));
        Assert.Equal(house.Id, Assert.Single(store.State.Visited));
        Assert.Equal(0, changed);
        var unchanged = await backend.GetSnapshotAsync();
        Assert.Equal(house, Assert.Single(unchanged.Houses));
        Assert.Equal(house.Id, Assert.Single(unchanged.Visited));

        await backend.SaveHouseAsync(updated);
        var snapshot = await backend.GetSnapshotAsync();

        Assert.Equal(updated, Assert.Single(snapshot.Houses));
        Assert.Equal(house.Id, Assert.Single(snapshot.Visited));
        Assert.Equal(updated, Assert.Single(store.State.Houses));
        Assert.Equal(house.Id, Assert.Single(store.State.Visited));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, changed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelledVisitChangeDoesNotChangeStateAndLaterChangeSucceeds(bool visited)
    {
        var house = Sample();
        var original = new DemoState([house], visited ? [] : [house.Id]);
        var store = new CountingStore(original);
        var backend = new MockNeighborhoodBackend(store, TimeProvider.System);
        await backend.GetSnapshotAsync();
        var changed = 0;
        backend.Changed += (_, _) => changed++;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => backend.SetVisitedAsync(house.Id, visited, cancellation.Token));

        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Same(original, store.State);
        Assert.Equal(house, Assert.Single(store.State.Houses));
        Assert.Equal(visited ? 0 : 1, store.State.Visited.Count);
        Assert.Equal(!visited, store.State.Visited.Contains(house.Id));
        Assert.Equal(0, changed);
        var unchanged = await backend.GetSnapshotAsync();
        Assert.Equal(house, Assert.Single(unchanged.Houses));
        Assert.Equal(visited ? 0 : 1, unchanged.Visited.Count);
        Assert.Equal(!visited, unchanged.Visited.Contains(house.Id));

        await backend.SetVisitedAsync(house.Id, visited);
        var snapshot = await backend.GetSnapshotAsync();

        Assert.Equal(house, Assert.Single(snapshot.Houses));
        Assert.Equal(visited ? 1 : 0, snapshot.Visited.Count);
        Assert.Equal(visited, snapshot.Visited.Contains(house.Id));
        Assert.Equal(house, Assert.Single(store.State.Houses));
        Assert.Equal(visited ? 1 : 0, store.State.Visited.Count);
        Assert.Equal(visited, store.State.Visited.Contains(house.Id));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, changed);
    }

    private static House Sample() => new(Guid.Parse("82193cb3-876d-4e05-89a1-323ca47d9170"),
        "Pumpkin Porch", "13 Make-Believe Lane", "Friendly pumpkins.",
        new(17, 0), new(21, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true);

    private sealed class CountingStore(DemoState state) : IDemoStateStore
    {
        public DemoState State { get; private set; } = state;
        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<DemoState?> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult<DemoState?>(State);
        }

        public Task SaveAsync(DemoState value, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            State = value;
            return Task.CompletedTask;
        }
    }
}
