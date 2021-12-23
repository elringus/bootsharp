using System;
using System.Text.Json.Serialization;
using DotNetJS;
using Microsoft.JSInterop;
using Test.Types;

namespace Test.Main;

public static class Program
{
    private static bool mainInvoked;

    static Program ()
    {
        JS.Runtime.ConfigureJson(options =>
            options.Converters.Add(new JsonStringEnumConverter())
        );
    }

    public static void Main ()
    {
        mainInvoked = true;
        ForceLoadTypesAssembly();
    }

    [JSInvokable]
    public static bool IsMainInvoked () => mainInvoked;

    // https://github.com/Elringus/DotNetJS/issues/23
    private static void ForceLoadTypesAssembly () => Console.Write(typeof(Registry));
}
