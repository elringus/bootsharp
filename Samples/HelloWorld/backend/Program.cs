using System;
using Bootsharp;

namespace HelloWorld;

public partial class Program
{
    // Entry point is invoked from JavaScript on boot.
    public static void Main ()
    {
        // Invoking 'bootsharp.HelloWorld.getHostName()' JavaScript function.
        var hostName = GetHostName();
        // Writing to JavaScript host console.
        Console.WriteLine($"Hello {hostName}, .NET here!");
    }

    [JSFunction] // The interoperability code is auto-generated.
    public static partial string GetHostName ();

    [JSInvokable] // The method is invoked from JavaScript.
    public static string GetName () => ".NET";
}
