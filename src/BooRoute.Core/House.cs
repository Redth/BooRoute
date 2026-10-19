namespace BooRoute.Core;

public enum HouseStatus { LightsOn, OutOfTreats, Closed }
public enum ScareLevel { Friendly, Spooky, Scary }

public sealed record House(
    Guid Id,
    string Nickname,
    string Address,
    string Description,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    ScareLevel ScareLevel,
    HouseStatus Status,
    bool QuietVisits,
    bool StepFreeAccess)
{
    public bool IsWelcoming(DateTimeOffset localNow)
    {
        if (Status != HouseStatus.LightsOn || OpensAt == ClosesAt)
            return false;

        var time = TimeOnly.FromDateTime(localNow.DateTime);
        return OpensAt < ClosesAt
            ? time >= OpensAt && time < ClosesAt
            : time >= OpensAt || time < ClosesAt;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Nickname))
            errors.Add("Give your house a nickname.");
        else if (Nickname.Length > 60)
            errors.Add("Keep the nickname to 60 characters.");
        if (string.IsNullOrWhiteSpace(Address))
            errors.Add("Enter a fictional address.");
        else if (Address.Length > 120)
            errors.Add("Keep the address to 120 characters.");
        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Add a short description for visiting families.");
        else if (Description.Length > 500)
            errors.Add("Keep the description to 500 characters.");
        if (OpensAt == ClosesAt)
            errors.Add("Opening and closing times must be different.");
        if (!Enum.IsDefined(ScareLevel) || !Enum.IsDefined(Status))
            errors.Add("Choose a valid scare level and house status.");
        if (Id == Guid.Empty)
            errors.Add("The house needs a valid identifier.");
        return errors;
    }
}

public static class HouseFilters
{
    public static IEnumerable<House> Apply(
        IEnumerable<House> houses,
        DateTimeOffset localNow,
        bool welcomingOnly = false,
        ScareLevel? scareLevel = null,
        bool quietOnly = false,
        IReadOnlySet<Guid>? visitedOnly = null) =>
        houses.Where(house =>
            (!welcomingOnly || house.IsWelcoming(localNow)) &&
            (scareLevel is null || house.ScareLevel == scareLevel) &&
            (!quietOnly || house.QuietVisits) &&
            (visitedOnly is null || visitedOnly.Contains(house.Id)));
}
