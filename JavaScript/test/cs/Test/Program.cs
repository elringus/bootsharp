using System;
using System.Text.Json.Serialization;
using Bootsharp;
using Test.Types;

namespace Test;

public static partial class Program
{
    private static bool mainInvoked;

    static Program ()
    {
        Serializer.Options.Converters.Add(new JsonStringEnumConverter());
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
