using System.Collections.Generic;

namespace Test.Types;

public interface IRegistryProvider
{
    Registry GetRegistry ();
    IReadOnlyList<Registry> GetRegistries ();
    IReadOnlyDictionary<string, Registry> GetRegistryMap ();
}
