global using static Bootsharp.Common.Test.Mocks;

namespace Bootsharp.Common.Test;

public static class Mocks
{
    public interface IBackend;
    public interface IFrontend;
    public class Backend : IBackend;
    public class Frontend : IFrontend;

    public record MockItem (string Id);
    public record MockRecord (IReadOnlyList<MockItem> Items);
}
