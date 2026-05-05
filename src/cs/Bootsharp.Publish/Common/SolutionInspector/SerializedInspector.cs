using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bootsharp.Publish;

/// <remarks>
/// Remember that the serialization is only required for the values that cross the interop boundary
/// and whose types are not natively supported by System.Runtime.InteropServices.JavaScript.
/// The types that are referenced by these top-level interop types are crawled by this inspector.
/// </remarks>
internal sealed class SerializedInspector
{
    private record Discard (Type Type) : SerializedMeta(Type);

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    private static readonly FrozenSet<string> native = new[] {
        typeof(string).FullName!, typeof(bool).FullName!, typeof(byte).FullName!,
        typeof(char).FullName!, typeof(short).FullName!, typeof(long).FullName!,
        typeof(int).FullName!, typeof(float).FullName!, typeof(double).FullName!,
        typeof(nint).FullName!, typeof(Task).FullName!, typeof(DateTime).FullName!,
        typeof(DateTimeOffset).FullName!, typeof(Exception).FullName!
    }.ToFrozenSet();

    private readonly Dictionary<string, SerializedMeta> byId = [];
    private readonly HashSet<Type> cycle = [];

    public SerializedMeta? Inspect (Type type)
    {
        return ShouldSerialize(type) ? Build(type) : null;
    }

    public IReadOnlyList<SerializedMeta> Collect ()
    {
        return OrderByDependencyGraph(byId.Values); // initialization order matters for JavaScript
    }

    private static bool ShouldSerialize (Type type)
    {
        if (IsVoid(type)) return false;
        if (IsNullable(type, out var value)) return ShouldSerialize(value);
        if (IsTaskWithResult(type, out var result)) return ShouldSerialize(result);
        if (IsInstancedType(type)) return false;
        return !native.Contains(type.FullName!);
    }

    private SerializedMeta Build (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result; // tasks are natively supported - ignore them
        var id = BuildSerializedId(type);
        if (byId.TryGetValue(id, out var existing)) return existing;
        if (!cycle.Add(type)) return new Discard(type); // break self-ref cycle
        var meta = byId[id] =
            IsNullable(type, out var value) ? new SerializedNullableMeta(type, Build(value)) :
            type.IsEnum ? new SerializedEnumMeta(type) :
            IsPrimitive(type) ? new SerializedPrimitiveMeta(type) :
            type.IsArray ? new SerializedArrayMeta(type, Build(type.GetElementType()!)) :
            IsList(type, out var element) ? new SerializedListMeta(type, Build(element)) :
            IsDictionary(type, out var k, out var v) ? new SerializedDictionaryMeta(type, Build(k), Build(v)) :
            BuildObject(type);
        cycle.Remove(type);
        return meta;
    }

    private static bool IsPrimitive (Type type) =>
        Type.GetTypeCode(type) != TypeCode.Object ||
        type.FullName == typeof(DateTimeOffset).FullName ||
        type.FullName == typeof(nint).FullName;

    private SerializedObjectMeta BuildObject (Type type)
    {
        var ctor = ResolveConstructor(type);
        var ctorParams = ctor?.GetParameters() ?? [];
        var paramOrders = ctorParams
            .Select((p, i) => (p.Name!, i))
            .ToDictionary(p => p.Item1, p => p.i, StringComparer.OrdinalIgnoreCase);
        var properties = GetSerializableProperties(type)
            .OrderBy(p => paramOrders.GetValueOrDefault(p.Name, int.MaxValue))
            .Select(p => BuildProperty(p, paramOrders.ContainsKey(p.Name)))
            .ToArray();
        return new(type, properties);
    }

    private SerializedPropertyMeta BuildProperty (PropertyInfo prop, bool ctor)
    {
        var value = Build(prop.PropertyType);
        var getter = prop.GetMethod!;
        var setter = prop.SetMethod;
        var initOnly = setter?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(m => m.FullName == typeof(IsExternalInit).FullName) == true;
        var canSet = setter != null && setter.IsPublic && !initOnly;
        var canInit = setter != null && setter.IsPublic && initOnly;
        var canSetField = !canInit && !canSet && IsAutoProperty(prop) && getter.IsPublic;
        return new(value.Clr) {
            Info = prop,
            Name = prop.Name,
            JSName = BuildJSName(prop.Name),
            OmitWhenNull = !prop.PropertyType.IsValueType || IsNullable(prop.PropertyType),
            Required = prop.CustomAttributes
                .Any(a => a.AttributeType.FullName == typeof(RequiredMemberAttribute).FullName),
            ConstructorParameter = ctor,
            Kind = canInit ? SerializedPropertyKind.Init : canSet ? SerializedPropertyKind.Set :
                canSetField ? SerializedPropertyKind.Field : SerializedPropertyKind.None,
            FieldAccessorName = canSetField ? $"Access_{BuildSerializedId(prop.DeclaringType!)}_{prop.Name}" : null
        };
    }

    private static ConstructorInfo? ResolveConstructor (Type type)
    {
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (constructors.Length == 0) return null;
        var parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        if (parameterless != null) return parameterless;
        var matching = constructors.Where(c => HasMatchingParameters(c, type)).ToArray();
        if (matching.Length == 1) return matching[0];
        return null;

        static bool HasMatchingParameters (ConstructorInfo ctor, Type declaringType)
        {
            var props = declaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var param in ctor.GetParameters())
                if (!props.TryGetValue(param.Name!, out var prop)) return false;
                else if (prop.PropertyType != param.ParameterType) return false;
            return true;
        }
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties (Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetMethod != null && p.GetIndexParameters().Length == 0);
    }

    private static IReadOnlyList<SerializedMeta> OrderByDependencyGraph (IEnumerable<SerializedMeta> types)
    {
        var metas = types.DistinctBy(m => m.Id).ToDictionary(m => m.Id);
        var pending = metas.ToDictionary(
            m => m.Key,
            m => GetInitDependencies(m.Value).Where(metas.ContainsKey).ToHashSet(StringComparer.Ordinal));
        var dependents = metas.Keys.ToDictionary(k => k, _ => new List<string>(), StringComparer.Ordinal);
        foreach (var (id, dependencies) in pending)
        foreach (var dependency in dependencies)
            dependents[dependency].Add(id);

        var queue = new PriorityQueue<SerializedMeta, (int, string)>();
        foreach (var meta in metas.Values.Where(m => pending[m.Id].Count == 0))
            queue.Enqueue(meta, (GetInitOrder(meta), meta.Id));

        var ordered = new List<SerializedMeta>(metas.Count);
        while (queue.TryDequeue(out var meta, out _))
        {
            if (!pending.Remove(meta.Id)) continue;
            ordered.Add(meta);
            foreach (var dependent in dependents[meta.Id])
                if (pending.TryGetValue(dependent, out var deps))
                {
                    deps.Remove(meta.Id);
                    if (deps.Count == 0)
                        queue.Enqueue(metas[dependent], (GetInitOrder(metas[dependent]), dependent));
                }
        }

        return ordered;

        static int GetInitOrder (SerializedMeta meta) => meta switch {
            SerializedPrimitiveMeta or SerializedEnumMeta => 0,
            SerializedObjectMeta => 2,
            _ => 3
        };

        static IEnumerable<string> GetInitDependencies (SerializedMeta meta)
        {
            switch (meta)
            {
                case SerializedNullableMeta nullable:
                    yield return nullable.Value.Id;
                    yield break;
                case SerializedArrayMeta array:
                    yield return array.Element.Id;
                    yield break;
                case SerializedListMeta list:
                    yield return list.Element.Id;
                    yield break;
                case SerializedDictionaryMeta dic:
                    yield return dic.Key.Id;
                    yield return dic.Value.Id;
                    yield break;
            }
        }
    }
}
