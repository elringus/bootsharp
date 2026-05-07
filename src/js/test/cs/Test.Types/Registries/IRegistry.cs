using System.Collections.Generic;

namespace Test.Types;

public interface IRegistry
{
    IReadOnlyList<Wheeled?> Wheeled { get; set; }
    IReadOnlyList<Tracked?> Tracked { get; set; }
}
