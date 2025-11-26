global using static Bootsharp.Inject.Test.Mocks;

namespace Bootsharp.Inject.Test;

public static class Mocks
{
    public interface IBackend;
    public interface IFrontend;
    public class Backend : IBackend;
    public class Frontend : IFrontend;
    public class JSFrontend : IFrontend;

    public class JSBackend
    {
        public static IBackend Handler { get; private set; }

        public JSBackend (IBackend handler)
        {
            Handler = handler;
        }
    }
}
