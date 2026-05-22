global using static Test.Library.Assertions;
using System;
using System.Runtime.CompilerServices;

namespace Test.Library;

public static class Assertions
{
    public static void Assert (bool condition, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (!condition) throw new Exception($"C# assertion failed at {file}:{line}.");
    }
}
