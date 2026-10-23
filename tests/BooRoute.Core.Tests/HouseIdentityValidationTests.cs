namespace BooRoute.Core.Tests;

public sealed class HouseIdentityValidationTests
{
    private static House Sample() => new(Guid.NewGuid(), "Pumpkin Porch", "13 Make-Believe Lane",
        "Friendly pumpkins.", new(17, 0), new(21, 0), ScareLevel.Friendly,
        HouseStatus.LightsOn, true, true);

    [Fact]
    public void EmptyIdentifierIsRejected()
    {
        var house = Sample() with { Id = Guid.Empty };

        Assert.Equal("The house needs a valid identifier.", Assert.Single(house.Validate()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void UndefinedStatusIsRejected(int value)
    {
        var house = Sample() with { Status = (HouseStatus)value };

        Assert.Equal("Choose a valid scare level and house status.", Assert.Single(house.Validate()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void UndefinedScareLevelIsRejected(int value)
    {
        var house = Sample() with { ScareLevel = (ScareLevel)value };

        Assert.Equal("Choose a valid scare level and house status.", Assert.Single(house.Validate()));
    }

    public static IEnumerable<object[]> DefinedStatusesAndScareLevels()
    {
        foreach (var status in Enum.GetValues<HouseStatus>())
        foreach (var scareLevel in Enum.GetValues<ScareLevel>())
            yield return [status, scareLevel];
    }

    [Theory]
    [MemberData(nameof(DefinedStatusesAndScareLevels))]
    public void DefinedStatusAndScareLevelAreAccepted(HouseStatus status, ScareLevel scareLevel)
    {
        var house = Sample() with { Status = status, ScareLevel = scareLevel };

        Assert.Empty(house.Validate());
    }
}
