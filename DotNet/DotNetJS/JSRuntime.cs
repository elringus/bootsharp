using System;
using System.Reflection;
using System.Text.Json;
using Microsoft.JSInterop.WebAssembly;

namespace DotNetJS;

/// <inheritdoc cref="WebAssemblyJSRuntime"/>
public class JSRuntime : WebAssemblyJSRuntime, IJSRuntime
{
    /// <summary>
    /// JSON serializer options of the runtime used for calls from .NET to JavaScript.
    /// </summary>
    public JsonSerializerOptions OutboundJsonOptions => JsonSerializerOptions;
    /// <summary>
    /// JSON serializer options of the runtime used for calls from JavaScript to .NET.
    /// </summary>
    public JsonSerializerOptions InboundJsonOptions => GetInboundJsonOptions();

    /// <inheritdoc/>
    public void ConfigureJson (Action<JsonSerializerOptions> action)
    {
        action.Invoke(OutboundJsonOptions);
        action.Invoke(InboundJsonOptions);
    }

    private static JsonSerializerOptions GetInboundJsonOptions ()
    {
        var inboundRuntime = GetInboundRuntime();
        return GetJsonSerializerOptions(inboundRuntime);
    }

    private static WebAssemblyJSRuntime GetInboundRuntime ()
    {
        var assembly = Assembly.Load("Microsoft.AspNetCore.Components.WebAssembly");
        var type = assembly.GetType("Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime");
        return (WebAssemblyJSRuntime)type!.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions (WebAssemblyJSRuntime runtime)
    {
        return (JsonSerializerOptions)typeof(Microsoft.JSInterop.JSRuntime).GetProperty(nameof(JsonSerializerOptions),
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
    }
}
