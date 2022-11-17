using Backend.Domain;

namespace Backend.WASM;

public static class Program
{
    public static void Main ()
    {
        _ = new JSBackend(new Domain.Backend(new JSFrontend()));
    }
}
