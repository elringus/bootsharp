using System.Collections.Generic;

namespace Test.Types;

public interface IRegistryProvider
{
    IRegistry GetRegistry ();
    IReadOnlyList<IRegistry> GetRegistries ();
    IReadOnlyDictionary<string, IRegistry> GetRegistryMap ();
}
