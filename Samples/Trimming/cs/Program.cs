using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bootsharp;

[assembly: JSImport(typeof(IFrontend))]
[assembly: JSExport(typeof(IBackend))]

Serializer.Options = new(JsonSerializerDefaults.Web) {
    TypeInfoResolver = SerializerContext.Default
};

_ = new Backend.JSBackend(new SharpBackend());
var info = Frontend.JSFrontend.GetInfo();
Frontend.JSFrontend.Log($"Frontend: {info.Environment}");

public record Info(string Environment);

public interface IFrontend
{
    void Log (string message);
    Info GetInfo ();
}

public interface IBackend
{
    Info GetInfo ();
}

internal class SharpBackend : IBackend
{
    public Info GetInfo () => new($".NET {Environment.Version}");
}

// Until .NET support source generators compositing (https://github.com/dotnet/roslyn/issues/57239)
// JSON generator won't pick type hints emitted by other generators; hence, with aggressive trimming enabled,
// (which strips reflection-based JSON serialization), we have to manually hint the serialized types.
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
[JsonSerializable(typeof(Info))]
internal partial class SerializerContext : JsonSerializerContext;
