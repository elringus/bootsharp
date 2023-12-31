namespace Bootsharp.Inject.Test;

public interface IBackend;
public interface IFrontend;
public class Backend : IBackend;
public class Frontend : IFrontend;
public class JSFrontend : IFrontend;

public class JSBackend
{
    public static IBackend Handler;

    public JSBackend (IBackend handler)
    {
        Handler = handler;
    }
}
