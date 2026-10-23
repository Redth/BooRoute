using BooRoute.Pages;

namespace BooRoute.Core.Tests;

public sealed class HouseGridBoundaryTests
{
    [Theory]
    [InlineData(653.75, 1)]
    [InlineData(654, 2)]
    [InlineData(654.25, 2)]
    [InlineData(987.75, 2)]
    [InlineData(988, 3)]
    [InlineData(988.25, 3)]
    [InlineData(1655.75, 4)]
    [InlineData(1656, 5)]
    [InlineData(1656.25, 5)]
    [InlineData(3840.5, 11)]
    public void AvailableWidthFitsExpectedColumnsButNotOneMore(double width, int expectedColumns)
    {
        var columns = HouseGridLayout.GetSpan(width);

        Assert.Equal(expectedColumns, columns);
        Assert.True(columns * 320 + (columns - 1) * 14 <= width);
        Assert.True((columns + 1) * 320 + columns * 14 > width);
    }

    [Fact]
    public void NegativeInfinityFallsBackToOneColumn()
    {
        Assert.Equal(1, HouseGridLayout.GetSpan(double.NegativeInfinity));
    }
}
