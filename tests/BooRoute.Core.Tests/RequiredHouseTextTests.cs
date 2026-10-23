namespace BooRoute.Core.Tests;

public sealed class RequiredHouseTextTests
{
    private static House Sample() => new(Guid.NewGuid(), "Pumpkin Porch", "13 Make-Believe Lane",
        "Friendly pumpkins.", new(17, 0), new(21, 0), ScareLevel.Friendly,
        HouseStatus.LightsOn, true, true);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void NicknameRequiresText(string nickname)
    {
        var house = Sample() with { Nickname = nickname };

        Assert.Equal("Give your house a nickname.", Assert.Single(house.Validate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void AddressRequiresText(string address)
    {
        var house = Sample() with { Address = address };

        Assert.Equal("Enter a fictional address.", Assert.Single(house.Validate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void DescriptionRequiresText(string description)
    {
        var house = Sample() with { Description = description };

        Assert.Equal("Add a short description for visiting families.", Assert.Single(house.Validate()));
    }

    [Fact]
    public void NonemptyTextIsValid() =>
        Assert.Empty(Sample().Validate());
}
