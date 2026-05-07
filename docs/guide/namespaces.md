# Namespaces

Bootsharp maps binding APIs based on the fully qualified name of the C# types.

## Static Members

Full type name (including namespace) of the declaring type of the static member is mapped into JavaScript object name:

```csharp
class Class { [Export] static void Method() {} }
namespace Foo { class Class { [Export] static void Method() {} } }
namespace Foo.Bar { class Class { [Export] static void Method() {} } }
```

```ts
import { Class, Foo } from "bootsharp";

Class.method();
Foo.Class.method();
Foo.Bar.Class.method();
```

Methods inside nested classes are treated as if they were declared under namespace:

```csharp
namespace Foo;

public class Class
{
    public class Nested { [Export] public static void Method() {} }
}
```

```ts
import { Foo } from "bootsharp";

Foo.Class.Nested.method();
```

## Interop Modules

When generating bindings for [modules](/guide/interop-modules), an interface name is assumed to have an "I" prefix, so the associated JavaScript name will have the first character removed. Class modules keep their name as-is. In either case, if the type is declared under a namespace, it'll be mirrored in JavaScript.

```csharp
[Export(
    typeof(IExported),
    typeof(Foo.IExported),
    typeof(Foo.Bar.Exported)
)]

interface IExported { void Method(); }
namespace Foo { interface IExported { void Method(); } }
namespace Foo.Bar { class Exported { public void Method() {} } }
```

```ts
import { Exported, Foo } from "bootsharp";

Exported.method();
Foo.Exported.method();
Foo.Bar.Exported.method();
```

## Types

Custom types referenced in API signatures (records, classes, interfaces, etc) are declared under their respective namespace when they have one, or under root otherwise.

```csharp
public record Record;
namespace Foo { public record Record; }

partial class Class
{
    [Import]
    public static partial Record Method(Foo.Record r);
}
```

```ts
import { Class, Record, Foo } from "bootsharp";

Class.method = methodImpl;

function methodImpl(r: Record): Foo.Record {

}
```

## Configuring Namespaces

You can control how namespaces are generated with `Space` option in [preferences](/guide/preferences).
