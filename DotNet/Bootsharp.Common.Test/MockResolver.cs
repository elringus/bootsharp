using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Bootsharp.Common.Test;

public class MockResolver : IJsonTypeInfoResolver
{
    public JsonTypeInfo GetTypeInfo (Type type, JsonSerializerOptions options) => null;
}
