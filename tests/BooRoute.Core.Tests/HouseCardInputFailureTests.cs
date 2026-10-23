using BooRoute.Core;
using BooRoute.ViewModels;

namespace BooRoute.Core.Tests;

public sealed class HouseCardInputFailureTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<Guid> NoVisits = [];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceFailureAfterFirstItemPreservesPublishedCards(bool addHouse)
    {
        var first = House("First");
        var second = House("Second");
        var cards = new HouseCardCollection();
        cards.Update([first, second], NoVisits, Now);
        var firstCard = cards[0];
        var secondCard = cards[1];
        var incoming = addHouse ? House("Added") : first with { Address = "456 Fictional Lane" };
        var failure = new InvalidOperationException("The house source failed.");
        var notifications = 0;
        cards.CollectionChanged += (_, _) => notifications++;

        var thrown = Assert.Throws<InvalidOperationException>(() => cards.Update(Source(), NoVisits, Now));

        Assert.Same(failure, thrown);
        Assert.Equal([first, second], cards.Select(card => card.House));
        Assert.Same(firstCard, cards[0]);
        Assert.Same(secondCard, cards[1]);
        Assert.Equal(0, notifications);

        IEnumerable<House> Source()
        {
            yield return incoming;
            throw failure;
        }
    }

    [Fact]
    public void SingleUseSourceIsEnumeratedOnceAndApplied()
    {
        var first = House("First");
        var second = House("Second");
        var changed = second with { Address = "456 Fictional Lane" };
        var added = House("Added");
        var cards = new HouseCardCollection();
        cards.Update([first, second], NoVisits, Now);
        var firstCard = cards[0];
        var secondCard = cards[1];
        var enumerations = 0;

        cards.Update(Source(), NoVisits, Now);

        Assert.Equal(1, enumerations);
        Assert.Equal([first, changed, added], cards.Select(card => card.House));
        Assert.Same(firstCard, cards[0]);
        Assert.NotSame(secondCard, cards[1]);
        Assert.Same(changed, cards[1].House);
        Assert.Same(added, cards[2].House);

        IEnumerable<House> Source()
        {
            if (++enumerations != 1)
                throw new InvalidOperationException("The house source can only be enumerated once.");

            yield return first;
            yield return changed;
            yield return added;
        }
    }

    private static House House(string name) => new(Guid.NewGuid(), name, "123 Fictional Lane",
        "A fictional porch.", new(17, 0), new(20, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true);
}
