namespace BooRoute.Core.Tests;

public sealed class SnapshotIsolationTests
{
    [Fact]
    public async Task UpdatingHouseDoesNotChangeOlderSnapshot()
    {
        var house = Sample();
        var backend = CreateBackend(house);
        var before = await backend.GetSnapshotAsync();
        var updated = house with { Nickname = "Updated porch", Status = HouseStatus.OutOfTreats };

        await backend.SaveHouseAsync(updated);
        var after = await backend.GetSnapshotAsync();

        Assert.Equal(house, Assert.Single(before.Houses));
        Assert.Equal(updated, Assert.Single(after.Houses));
        Assert.Empty(before.Visited);
        Assert.Empty(after.Visited);
    }

    [Fact]
    public async Task AddingHouseDoesNotChangeOlderSnapshot()
    {
        var house = Sample();
        var backend = CreateBackend(house);
        var before = await backend.GetSnapshotAsync();
        var added = Sample();

        await backend.SaveHouseAsync(added);
        var after = await backend.GetSnapshotAsync();

        Assert.Equal(house, Assert.Single(before.Houses));
        Assert.Equal(new[] { house, added }, after.Houses);
        Assert.Empty(before.Visited);
        Assert.Empty(after.Visited);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangingVisitDoesNotChangeOlderSnapshot(bool visited)
    {
        var house = Sample();
        var other = Sample();
        var backend = CreateBackend(house, other);
        await backend.SetVisitedAsync(other.Id, true);
        await backend.SetVisitedAsync(house.Id, !visited);
        var before = await backend.GetSnapshotAsync();

        await backend.SetVisitedAsync(house.Id, visited);
        var after = await backend.GetSnapshotAsync();

        Assert.Equal(!visited, before.Visited.Contains(house.Id));
        Assert.Equal(visited, after.Visited.Contains(house.Id));
        Assert.Equal(visited ? 1 : 2, before.Visited.Count);
        Assert.Equal(visited ? 2 : 1, after.Visited.Count);
        Assert.Contains(other.Id, before.Visited);
        Assert.Contains(other.Id, after.Visited);
        Assert.Equal(new[] { house, other }, before.Houses);
        Assert.Equal(new[] { house, other }, after.Houses);
    }

    [Fact]
    public async Task ChangingReturnedHouseArrayDoesNotChangeBackendSnapshots()
    {
        var house = Sample();
        var backend = CreateBackend(house);
        var snapshot = await backend.GetSnapshotAsync();
        var another = await backend.GetSnapshotAsync();
        var houses = Assert.IsType<House[]>(snapshot.Houses);

        houses[0] = house with { Nickname = "Changed only in snapshot" };
        var later = await backend.GetSnapshotAsync();

        Assert.Equal(house, Assert.Single(another.Houses));
        Assert.Equal(house, Assert.Single(later.Houses));
    }

    [Fact]
    public async Task ChangingReturnedVisitSetDoesNotChangeBackendSnapshots()
    {
        var house = Sample();
        var other = Sample();
        var backend = CreateBackend(house, other);
        await backend.SetVisitedAsync(house.Id, true);
        var snapshot = await backend.GetSnapshotAsync();
        var another = await backend.GetSnapshotAsync();
        var visits = Assert.IsType<HashSet<Guid>>(snapshot.Visited);

        Assert.True(visits.Remove(house.Id));
        Assert.True(visits.Add(other.Id));
        var later = await backend.GetSnapshotAsync();

        Assert.Equal(house.Id, Assert.Single(another.Visited));
        Assert.Equal(house.Id, Assert.Single(later.Visited));
    }

    private static House Sample() => new(Guid.NewGuid(), "Pumpkin Porch", "13 Make-Believe Lane",
        "Friendly pumpkins.", new(17, 0), new(21, 0), ScareLevel.Friendly,
        HouseStatus.LightsOn, true, true);

    private static MockNeighborhoodBackend CreateBackend(params House[] houses) =>
        new(new MemoryStore(new(houses.ToList(), [])), TimeProvider.System);

    private sealed class MemoryStore(DemoState state) : IDemoStateStore
    {
        public Task<DemoState?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<DemoState?>(state);

        public Task SaveAsync(DemoState value, CancellationToken cancellationToken = default)
        {
            state = value;
            return Task.CompletedTask;
        }
    }
}
