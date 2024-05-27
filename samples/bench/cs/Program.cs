using Bootsharp;

namespace Bench;

public static class Program
{
    public static void Main () { }

    [JSInvokable]
    public static RecordA Echo (RecordA record) => record;
}
