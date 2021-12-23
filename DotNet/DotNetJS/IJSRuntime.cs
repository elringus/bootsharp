using System;
using System.Text.Json;
using Microsoft.JSInterop;

namespace DotNetJS;

/// <inheritdoc cref="IJSInProcessRuntime"/>
public interface IJSRuntime : IJSInProcessRuntime, IJSUnmarshalledRuntime
{
    /// <summary>
    /// Overrides default JSON serializer options used for marshalling the interop data.
    /// </summary>
    /// <param name="action">An action to invoke over the serializer options.</param>
    void ConfigureJson (Action<JsonSerializerOptions> action);
}
