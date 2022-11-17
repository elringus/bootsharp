namespace Backend.WASM;

public static class Program
{
    public static void Main ()
    {
        _ = new JSBackend(new Backend(new JSFrontend()));
    }
}
