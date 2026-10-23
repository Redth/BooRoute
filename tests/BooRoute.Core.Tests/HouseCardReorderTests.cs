using System.Collections.Specialized;
using BooRoute.Core;
using BooRoute.ViewModels;

namespace BooRoute.Core.Tests;

public sealed class HouseCardReorderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<Guid> NoVisits = [];

    [Fact]
    public void ReorderingOnlyPreservesEveryCardAndEmitsOnlyMatchingMoves()
    {
        var houses = CreateHouses();
        var cards = new HouseCardCollection();
        cards.Update(houses, NoVisits, Now);
        var original = cards.ToArray();
        var changes = Observe(cards);
        House[] ordered = [houses[2] with { }, houses[0] with { }, houses[3] with { }, houses[1] with { }];

        cards.Update(ordered, NoVisits, Now);

        Assert.Equal(ordered, cards.Select(card => card.House));
        AssertCards([original[2], original[0], original[3], original[1]], cards);
        Assert.Collection(changes,
            change =>
            {
                AssertMove(change.Args, original[2], 2, 0);
                AssertCards([original[2], original[0], original[1], original[3]], change.Cards);
            },
            change =>
            {
                AssertMove(change.Args, original[3], 3, 2);
                AssertCards([original[2], original[0], original[3], original[1]], change.Cards);
            });
    }

    [Fact]
    public void ReorderingAndEditingMovesTheOldCardThenReplacesItAtTheFinalIndex()
    {
        var houses = CreateHouses();
        var cards = new HouseCardCollection();
        cards.Update(houses, NoVisits, Now);
        var original = cards.ToArray();
        var edited = houses[2] with { Nickname = "New porch name", Address = "456 Fictional Lane" };
        var changes = Observe(cards);
        House[] ordered = [houses[3], edited, houses[0], houses[1]];

        cards.Update(ordered, NoVisits, Now);

        Assert.Equal(ordered, cards.Select(card => card.House));
        var replacement = cards[1];
        Assert.NotSame(original[2], replacement);
        Assert.Same(edited, replacement.House);
        Assert.Equal(edited.Id, replacement.House.Id);
        Assert.Equal(edited.Nickname, replacement.Nickname);
        Assert.Equal(edited.Address, replacement.Address);
        AssertCards([original[3], replacement, original[0], original[1]], cards);
        Assert.Collection(changes,
            change =>
            {
                AssertMove(change.Args, original[3], 3, 0);
                AssertCards([original[3], original[0], original[1], original[2]], change.Cards);
            },
            change =>
            {
                AssertMove(change.Args, original[2], 3, 1);
                AssertCards([original[3], original[2], original[0], original[1]], change.Cards);
            },
            change =>
            {
                Assert.Equal(NotifyCollectionChangedAction.Replace, change.Args.Action);
                Assert.Equal(1, change.Args.OldStartingIndex);
                Assert.Equal(1, change.Args.NewStartingIndex);
                Assert.NotNull(change.Args.OldItems);
                Assert.NotNull(change.Args.NewItems);
                Assert.Same(original[2], Assert.Single(change.Args.OldItems));
                Assert.Same(replacement, Assert.Single(change.Args.NewItems));
                AssertCards([original[3], replacement, original[0], original[1]], change.Cards);
            });
    }

    private static List<(NotifyCollectionChangedEventArgs Args, HouseCard[] Cards)> Observe(
        HouseCardCollection cards)
    {
        var changes = new List<(NotifyCollectionChangedEventArgs Args, HouseCard[] Cards)>();
        cards.CollectionChanged += (_, args) => changes.Add((args, cards.ToArray()));
        return changes;
    }

    private static void AssertMove(NotifyCollectionChangedEventArgs args, HouseCard card, int from, int to)
    {
        Assert.Equal(NotifyCollectionChangedAction.Move, args.Action);
        Assert.Equal(from, args.OldStartingIndex);
        Assert.Equal(to, args.NewStartingIndex);
        Assert.NotNull(args.OldItems);
        Assert.NotNull(args.NewItems);
        Assert.Same(card, Assert.Single(args.OldItems));
        Assert.Same(card, Assert.Single(args.NewItems));
    }

    private static void AssertCards(HouseCard[] expected, IEnumerable<HouseCard> actual)
    {
        var cards = actual.ToArray();
        Assert.Equal(expected.Length, cards.Length);
        for (var index = 0; index < expected.Length; index++)
            Assert.Same(expected[index], cards[index]);
    }

    private static House[] CreateHouses() =>
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Willow", "123 Fictional Lane",
            "A fictional porch.", new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true),
        new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Maple", "123 Fictional Lane",
            "A fictional porch.", new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true),
        new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Cedar", "123 Fictional Lane",
            "A fictional porch.", new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true),
        new(Guid.Parse("00000000-0000-0000-0000-000000000004"), "Oak", "123 Fictional Lane",
            "A fictional porch.", new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true)
    ];
}
