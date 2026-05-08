using System.Collections.Generic;

namespace Test.Library;

public interface IRegistryProvider
{
    IRegistry GetRegistry ();
    IReadOnlyList<IRegistry> GetRegistries ();
    IReadOnlyDictionary<string, IRegistry> GetRegistryMap ();
}
