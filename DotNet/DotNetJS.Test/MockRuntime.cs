using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetJS.Test;

[ExcludeFromCodeCoverage]
public class MockRuntime : IJSRuntime
{
    public void ConfigureJson (Action<JsonSerializerOptions> action)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TValue> InvokeAsync<TValue> (string identifier, object?[]? args)
    {
        throw new NotImplementedException();
    }

    public ValueTask<TValue> InvokeAsync<TValue> (string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        throw new NotImplementedException();
    }

    public TResult Invoke<TResult> (string identifier, object?[]? args)
    {
        throw new NotImplementedException();
    }

    public TResult InvokeUnmarshalled<TResult> (string identifier)
    {
        throw new NotImplementedException();
    }

    public TResult InvokeUnmarshalled<T0, TResult> (string identifier, T0 arg0)
    {
        throw new NotImplementedException();
    }

    public TResult InvokeUnmarshalled<T0, T1, TResult> (string identifier, T0 arg0, T1 arg1)
    {
        throw new NotImplementedException();
    }

    public TResult InvokeUnmarshalled<T0, T1, T2, TResult> (string identifier, T0 arg0, T1 arg1, T2 arg2)
    {
        throw new NotImplementedException();
    }
}
