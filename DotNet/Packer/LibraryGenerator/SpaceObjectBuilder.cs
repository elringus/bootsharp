using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class SpaceObjectBuilder
{
    private readonly HashSet<string> declaredObjects = new();
    private readonly List<string> names = new();

    public string EnsureNamespaceObjectsDeclared (string space, string js)
    {
        BuildObjectNamesForNamespace(space);
        foreach (var obj in names)
            if (declaredObjects.Add(obj))
                js = JoinLines($"exports.{obj} = {{}};", js);
        return js;
    }

    public void Reset ()
    {
        declaredObjects.Clear();
    }

    private void BuildObjectNamesForNamespace (string space)
    {
        names.Clear();
        var parts = space.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            var previousParts = i > 0 ? string.Join(".", parts.Take(i)) + "." : "";
            names.Add(previousParts + parts[i]);
        }
        names.Reverse();
    }
}
