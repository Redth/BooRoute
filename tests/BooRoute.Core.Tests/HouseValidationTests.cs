namespace BooRoute.Core.Tests;

public sealed class HouseValidationTests
{
    private static House Sample() => new(Guid.NewGuid(), "Pumpkin Porch", "13 Make-Believe Lane",
        "Friendly pumpkins.", new(17, 0), new(21, 0), ScareLevel.Friendly,
        HouseStatus.LightsOn, true, true);

    [Theory]
    [InlineData(60, null)]
    [InlineData(61, "Keep the nickname to 60 characters.")]
    public void NicknameLengthBoundary(int length, string? expectedError)
    {
        var house = Sample() with { Nickname = new string('N', length) };
        var errors = house.Validate();

        if (expectedError is null)
            Assert.Empty(errors);
        else
            Assert.Equal(expectedError, Assert.Single(errors));
    }

    [Theory]
    [InlineData(120, null)]
    [InlineData(121, "Keep the address to 120 characters.")]
    public void AddressLengthBoundary(int length, string? expectedError)
    {
        var house = Sample() with { Address = new string('A', length) };
        var errors = house.Validate();

        if (expectedError is null)
            Assert.Empty(errors);
        else
            Assert.Equal(expectedError, Assert.Single(errors));
    }

    [Theory]
    [InlineData(500, null)]
    [InlineData(501, "Keep the description to 500 characters.")]
    public void DescriptionLengthBoundary(int length, string? expectedError)
    {
        var house = Sample() with { Description = new string('D', length) };
        var errors = house.Validate();

        if (expectedError is null)
            Assert.Empty(errors);
        else
            Assert.Equal(expectedError, Assert.Single(errors));
    }
}
