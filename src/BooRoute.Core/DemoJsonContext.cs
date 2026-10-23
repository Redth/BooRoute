using System.Text.Json.Serialization;

namespace BooRoute.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DemoState))]
internal partial class DemoJsonContext : JsonSerializerContext
{
}
