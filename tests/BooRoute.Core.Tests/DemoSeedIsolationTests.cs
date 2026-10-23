namespace BooRoute.Core.Tests;

public sealed class DemoSeedIsolationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HouseIdsAreNonemptyAndUniqueWithinNeighborhood()
    {
        var state = DemoSeed.Create(Now);

        Assert.NotEmpty(state.Houses);
        Assert.All(state.Houses, house => Assert.NotEqual(Guid.Empty, house.Id));
        Assert.Equal(state.Houses.Count, state.Houses.Select(house => house.Id).Distinct().Count());
    }

    [Fact]
    public void EditingRemovingAndAddingHousesDoesNotChangeAnotherNeighborhood()
    {
        var first = DemoSeed.Create(Now);
        var second = DemoSeed.Create(Now);
        var originalFirst = first.Houses.ToArray();
        var originalSecond = second.Houses.ToArray();
        var edited = originalFirst[0] with { Nickname = "Updated porch", Status = HouseStatus.Closed };
        var added = originalFirst[1] with
        {
            Id = Guid.Parse("fc0c35c4-f163-4319-836a-9b536a712c31"),
            Nickname = "New porch"
        };
        Assert.NotSame(first.Houses, second.Houses);

        first.Houses[0] = edited;
        first.Houses.RemoveAt(1);
        first.Houses.Add(added);

        Assert.Equal(new[] { edited }.Concat(originalFirst.Skip(2)).Append(added), first.Houses);
        Assert.Equal(originalSecond, second.Houses);
        Assert.Empty(first.Visited);
        Assert.Empty(second.Visited);
    }

    [Fact]
    public void AddingAndRemovingVisitsDoesNotChangeAnotherNeighborhood()
    {
        var first = DemoSeed.Create(Now);
        var second = DemoSeed.Create(Now);
        var originalFirst = first.Houses.ToArray();
        var originalSecond = second.Houses.ToArray();
        Assert.NotSame(first.Visited, second.Visited);

        Assert.True(second.Visited.Add(second.Houses[0].Id));
        Assert.Empty(first.Visited);

        Assert.True(first.Visited.Add(first.Houses[0].Id));
        Assert.True(first.Visited.Add(first.Houses[1].Id));
        Assert.True(first.Visited.SetEquals(first.Houses.Take(2).Select(house => house.Id)));
        Assert.Equal(second.Houses[0].Id, Assert.Single(second.Visited));

        Assert.True(first.Visited.Remove(first.Houses[0].Id));

        Assert.Equal(first.Houses[1].Id, Assert.Single(first.Visited));
        Assert.Equal(second.Houses[0].Id, Assert.Single(second.Visited));
        Assert.Equal(originalFirst, first.Houses);
        Assert.Equal(originalSecond, second.Houses);
    }
}
