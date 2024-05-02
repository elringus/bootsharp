global using static Bootsharp.Common.Test.Mocks;

namespace Bootsharp.Common.Test;

public static class Mocks
{
    public interface IBackend;
    public interface IFrontend;
    public class Backend : IBackend;
    public class Frontend : IFrontend;

    public enum MockEnum { Foo, Bar }
    public record MockItem (string Id);
    public record MockItemWithEnum (MockEnum? Enum);
    public record MockRecord (IReadOnlyList<MockItem> Items);
}
