using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bootsharp.Test
{
    public record MockItem(string Id);
    public record MockRecord(IReadOnlyList<MockItem> Items);

    public static class MockClass
    {
        public static void Do () { }
        public static Task DoAsync () => Task.CompletedTask;
        public static MockRecord Echo (MockRecord record) => record;
        public static Task<MockRecord> EchoAsync (MockRecord record) => Task.FromResult(record);
        public static MockRecord Copy (MockRecord record, IReadOnlyList<MockItem> items, string id = null) =>
            record with { Items = id != null ? items.Select(i => i with { Id = id }).ToArray() : items };
    }
}

namespace Bootsharp.Test.Other
{
    public static class MockClassWithNamespaceNotEqualAssemblyName;
}
