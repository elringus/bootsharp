using System.Collections.Generic;

namespace Test.Library;

public interface IRegistry
{
    IReadOnlyList<Wheeled?> Wheeled { get; set; }
    IReadOnlyList<Tracked?> Tracked { get; set; }
}
