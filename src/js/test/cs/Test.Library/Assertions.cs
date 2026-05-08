global using static Test.Library.Assertions;
using System;

namespace Test.Library;

public static class Assertions
{
    public static void Assert (bool condition)
    {
        if (!condition) throw new Exception("C# assertion failed.");
    }
}
