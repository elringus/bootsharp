using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Test.Main;

public static class Stream
{
    [JSInvokable]
    public static async Task StreamFromJSAsync (IJSStreamReference streamRef)
    {
        await using var stream = await streamRef.OpenReadStreamAsync();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var buffer = memoryStream.ToArray();
        for (var i = 0; i < buffer.Length; i++)
            if (buffer[i] != i % 256)
                throw new Exception($"Failure at index {i}.");
        if (buffer.Length != 100_000)
            throw new Exception($"Got a stream of length {buffer.Length}, expected a length of 100,000.");
    }

    [JSInvokable]
    public static DotNetStreamReference StreamFromDotNet ()
    {
        var data = new byte[100000];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);
        return new DotNetStreamReference(new MemoryStream(data));
    }
}
