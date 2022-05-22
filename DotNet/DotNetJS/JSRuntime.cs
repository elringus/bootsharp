using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop.WebAssembly;

namespace DotNetJS;

/// <inheritdoc cref="WebAssemblyJSRuntime"/>
public class JSRuntime : IJSRuntime
{
    /// <summary>
    /// JSON serializer options of the JS interop runtime.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; }

    private readonly WebAssemblyJSRuntime defaultRuntime;

    /// <summary>
    /// Constructs new instance of the JS interop runtime.
    /// </summary>
    public JSRuntime ()
    {
        defaultRuntime = GetDefaultRuntime();
        JsonSerializerOptions = GetJsonSerializerOptions(defaultRuntime);
    }

    /// <inheritdoc/>
    public void ConfigureJson (Action<JsonSerializerOptions> action)
    {
        action.Invoke(JsonSerializerOptions);
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public ValueTask<TValue> InvokeAsync<TValue> (string identifier, object?[]? args)
        => defaultRuntime.InvokeAsync<TValue>(identifier, args);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public ValueTask<TValue> InvokeAsync<TValue> (string identifier, CancellationToken cancellationToken, object?[]? args)
        => defaultRuntime.InvokeAsync<TValue>(identifier, cancellationToken, args);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public TResult Invoke<TResult> (string identifier, params object?[]? args)
        => defaultRuntime.Invoke<TResult>(identifier, args);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public TResult InvokeUnmarshalled<TResult> (string identifier)
        => defaultRuntime.InvokeUnmarshalled<TResult>(identifier);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public TResult InvokeUnmarshalled<T0, TResult> (string identifier, T0 arg0)
        => defaultRuntime.InvokeUnmarshalled<T0, TResult>(identifier, arg0);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public TResult InvokeUnmarshalled<T0, T1, TResult> (string identifier, T0 arg0, T1 arg1)
        => defaultRuntime.InvokeUnmarshalled<T0, T1, TResult>(identifier, arg0, arg1);

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public TResult InvokeUnmarshalled<T0, T1, T2, TResult> (string identifier, T0 arg0, T1 arg1, T2 arg2)
        => defaultRuntime.InvokeUnmarshalled<T0, T1, T2, TResult>(identifier, arg0, arg1, arg2);

    private static WebAssemblyJSRuntime GetDefaultRuntime ()
    {
        var assembly = Assembly.Load("Microsoft.AspNetCore.Components.WebAssembly");
        var type = assembly.GetType("Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime");
        return (WebAssemblyJSRuntime)type!.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    }

    private static JsonSerializerOptions GetJsonSerializerOptions (WebAssemblyJSRuntime runtime)
    {
        return (JsonSerializerOptions)typeof(Microsoft.JSInterop.JSRuntime).GetProperty(nameof(System.Text.Json.JsonSerializerOptions),
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
    }
}
