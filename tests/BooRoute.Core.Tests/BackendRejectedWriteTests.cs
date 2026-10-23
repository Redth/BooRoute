namespace BooRoute.Core.Tests;

public sealed class BackendRejectedWriteTests
{
    [Fact]
    public async Task InvalidHouseIsRejectedBeforeStoreAccessAndLaterSaveSucceeds()
    {
        var house = Sample();
        Assert.Empty(house.Validate());
        var original = new DemoState([house], [house.Id]);
        var store = new CountingStore(original);
        var backend = new MockNeighborhoodBackend(store, new FixedClock());
        var changed = 0;
        backend.Changed += (_, _) => changed++;

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => backend.SaveHouseAsync(house with { Nickname = "" }));

        Assert.Equal("house", error.ParamName);
        Assert.Equal(0, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, changed);
        Assert.Same(original, store.State);
        Assert.Equal(house, Assert.Single(store.State.Houses));
        Assert.Equal(house.Id, Assert.Single(store.State.Visited));

        var unchanged = await backend.GetSnapshotAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(house, Assert.Single(unchanged.Houses));
        Assert.Equal(house.Id, Assert.Single(unchanged.Visited));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, changed);

        var updated = house with { Nickname = "Updated porch" };
        await backend.SaveHouseAsync(updated).WaitAsync(TimeSpan.FromSeconds(5));
        var afterSave = await backend.GetSnapshotAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(updated, Assert.Single(afterSave.Houses));
        Assert.Equal(house.Id, Assert.Single(afterSave.Visited));
        Assert.Equal(updated, Assert.Single(store.State.Houses));
        Assert.Equal(house.Id, Assert.Single(store.State.Visited));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, changed);
        Assert.Equal(house, Assert.Single(unchanged.Houses));
        Assert.Equal(house.Id, Assert.Single(unchanged.Visited));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnknownHouseVisitIsRejectedWithoutChangesAndLaterVisitChangeSucceeds(bool visited)
    {
        var house = Sample();
        var other = house with
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Nickname = "Ghost Garden",
            Address = "15 Make-Believe Lane"
        };
        House[] houses = [house, other];
        var initialVisits = new HashSet<Guid> { other.Id };
        if (!visited)
            initialVisits.Add(house.Id);
        var original = new DemoState(houses.ToList(), new(initialVisits));
        var store = new CountingStore(original);
        var backend = new MockNeighborhoodBackend(store, new FixedClock());
        var before = await backend.GetSnapshotAsync();
        var changed = 0;
        backend.Changed += (_, _) => changed++;
        var unknownId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => backend.SetVisitedAsync(unknownId, visited));

        Assert.Equal("houseId", error.ParamName);
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, changed);
        Assert.Same(original, store.State);
        Assert.Equal(houses, store.State.Houses);
        Assert.True(initialVisits.SetEquals(store.State.Visited));
        var unchanged = await backend.GetSnapshotAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(houses, unchanged.Houses);
        Assert.True(initialVisits.SetEquals(unchanged.Visited));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(0, store.SaveCount);
        Assert.Equal(0, changed);

        await backend.SetVisitedAsync(house.Id, visited).WaitAsync(TimeSpan.FromSeconds(5));
        var afterChange = await backend.GetSnapshotAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var expectedVisits = new HashSet<Guid> { other.Id };
        if (visited)
            expectedVisits.Add(house.Id);

        Assert.Equal(houses, afterChange.Houses);
        Assert.True(expectedVisits.SetEquals(afterChange.Visited));
        Assert.Equal(houses, store.State.Houses);
        Assert.True(expectedVisits.SetEquals(store.State.Visited));
        Assert.Equal(1, store.LoadCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, changed);
        Assert.Equal(houses, before.Houses);
        Assert.True(initialVisits.SetEquals(before.Visited));
    }

    private static House Sample() => new(Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Pumpkin Porch", "13 Make-Believe Lane", "Friendly pumpkins.",
        new(17, 0), new(21, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class CountingStore(DemoState state) : IDemoStateStore
    {
        public DemoState State { get; private set; } = state;
        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }

        public Task<DemoState?> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return Task.FromResult<DemoState?>(new(State.Houses.ToList(), new(State.Visited)));
        }

        public Task SaveAsync(DemoState value, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            State = new(value.Houses.ToList(), new(value.Visited));
            return Task.CompletedTask;
        }
    }
}
