using System.Collections.Generic;

namespace Test.Library;

public class Registry : IRegistry
{
    public IReadOnlyList<Wheeled?> Wheeled { get; set; } = [];
    public IReadOnlyList<Tracked?> Tracked { get; set; } = [];
}
