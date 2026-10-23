using System.Collections.ObjectModel;
using BooRoute.Core;

namespace BooRoute.ViewModels;

public sealed class HouseCard
{
    public House House { get; }
    public bool Visited { get; }
    public bool IsWelcoming { get; }
    public string Nickname => House.Nickname;
    public string Address => House.Address;
    public string Emoji { get; }
    public string Hours { get; }
    public string Status { get; }
    public string Features { get; }
    public string VisitStatus { get; }

    public HouseCard(House house, bool visited, DateTimeOffset localNow)
    {
        House = house;
        Visited = visited;
        IsWelcoming = house.IsWelcoming(localNow);
        Emoji = house.ScareLevel == ScareLevel.Scary ? "🎃" : "🏠";
        Hours = $"{house.OpensAt:HH:mm} – {house.ClosesAt:HH:mm}";
        Status = house.Status switch
        {
            HouseStatus.OutOfTreats => "Out of treats",
            HouseStatus.Closed => "Closed",
            _ => IsWelcoming ? "Lights on · Welcome!" : "Lights on · Outside hours"
        };
        Features = string.Join(" · ", new[]
        {
            house.ScareLevel.ToString(),
            house.QuietVisits ? "Quiet visits" : null,
            house.StepFreeAccess ? "Step-free" : null
        }.Where(value => value is not null));
        VisitStatus = visited ? "✓ Visited" : "";
    }
}

public sealed class HouseCardCollection : ObservableCollection<HouseCard>
{
    public void Update(IEnumerable<House> orderedHouses, IReadOnlySet<Guid> visited, DateTimeOffset localNow)
    {
        var houses = orderedHouses.ToArray();
        var incoming = houses.ToDictionary(house => house.Id);
        var existing = this.ToDictionary(card => card.House.Id);

        for (var index = Count - 1; index >= 0; index--)
        {
            if (!incoming.ContainsKey(this[index].House.Id))
                RemoveAt(index);
        }

        for (var index = 0; index < houses.Length; index++)
        {
            var house = houses[index];
            var isVisited = visited.Contains(house.Id);
            existing.TryGetValue(house.Id, out var previous);
            var card = previous is not null && previous.House == house &&
                       previous.Visited == isVisited && previous.IsWelcoming == house.IsWelcoming(localNow)
                ? previous
                : new HouseCard(house, isVisited, localNow);

            if (previous is null)
            {
                Insert(index, card);
                continue;
            }

            if (this[index].House.Id != house.Id)
                Move(IndexOf(previous), index);
            if (!ReferenceEquals(this[index], card))
                this[index] = card;
        }
    }
}
