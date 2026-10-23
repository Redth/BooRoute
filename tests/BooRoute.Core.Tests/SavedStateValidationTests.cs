using System.Text;
using System.Text.Json;

namespace BooRoute.Core.Tests;

public sealed class SavedStateValidationTests
{
    private static House Sample() => new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Pumpkin Porch", "13 Make-Believe Lane", "Friendly pumpkins.",
        new(17, 0), new(21, 0), ScareLevel.Friendly, HouseStatus.LightsOn, true, true);

    public static IEnumerable<object[]> InvalidSavedStates()
    {
        var house = Sample();
        yield return ["null"];
        yield return ["""{"Houses":[],"Visited":null}"""];
        yield return ["""{"Houses":[null],"Visited":[]}"""];
        yield return [JsonSerializer.Serialize(new DemoState(
            [house with { Nickname = "" }], []))];
        yield return [JsonSerializer.Serialize(new DemoState(
            [house, house with { Nickname = "Another porch" }], []))];
        yield return [JsonSerializer.Serialize(new DemoState(
            [house], [Guid.Parse("22222222-2222-2222-2222-222222222222")]))];
    }

    [Theory]
    [MemberData(nameof(InvalidSavedStates))]
    public async Task InvalidSavedStateIsRejectedWithoutChangingSavedBytes(string json)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"booroute-test-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "state.json");
        try
        {
            var store = new JsonDemoStateStore(path);
            var bytes = Encoding.UTF8.GetBytes(json);
            await File.WriteAllBytesAsync(path, bytes);

            await Assert.ThrowsAsync<InvalidDataException>(() => store.LoadAsync());

            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ValidSavedStateLoadsWithoutChangingSavedBytes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"booroute-test-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "state.json");
        try
        {
            var store = new JsonDemoStateStore(path);
            var house = Sample();
            var secondHouse = house with
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Nickname = "Another porch"
            };
            var state = new DemoState([house, secondHouse], [house.Id]);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(state);
            await File.WriteAllBytesAsync(path, bytes);

            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal(state.Houses, loaded.Houses);
            Assert.Equal(house.Id, Assert.Single(loaded.Visited));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavedStateMatchesExistingIndentedJsonFormat()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"booroute-test-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "state.json");
        const string json = """
            {
              "Houses": [
                {
                  "Id": "11111111-1111-1111-1111-111111111111",
                  "Nickname": "Pumpkin Porch",
                  "Address": "13 Make-Believe Lane",
                  "Description": "Friendly pumpkins.",
                  "OpensAt": "17:00:00",
                  "ClosesAt": "21:00:00",
                  "ScareLevel": 0,
                  "Status": 0,
                  "QuietVisits": true,
                  "StepFreeAccess": true
                }
              ],
              "Visited": [
                "11111111-1111-1111-1111-111111111111"
              ]
            }
            """;
        try
        {
            var house = Sample();
            var store = new JsonDemoStateStore(path);
            await store.SaveAsync(new DemoState([house], [house.Id]));

            Assert.Equal(json.ReplaceLineEndings("\n"),
                (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n"));
            Assert.False(File.Exists(path + ".tmp"));

            await File.WriteAllTextAsync(path, json);
            var loaded = await new JsonDemoStateStore(path).LoadAsync();

            Assert.NotNull(loaded);
            Assert.Equal(house, Assert.Single(loaded.Houses));
            Assert.Equal(house.Id, Assert.Single(loaded.Visited));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
