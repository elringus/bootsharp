# Namespaces

Bootsharp maps generated binding APIs based on the name of the associated C# types. The rules are a bit different for static interop methods, interop interfaces and types.

## Static Methods

Full type name (including namespace) of the declaring type of the static interop method is mapped into JavaScript object name:

```csharp
class Class { [JSInvokable] static void Method() {} }
namespace Foo { class Class { [JSInvokable] static void Method() {} } }
namespace Foo.Bar { class Class { [JSInvokable] static void Method() {} } }
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
    public class Nested { [JSInvokable] public static void Method() {} }
}
```

```ts
import { Foo } from "bootsharp";

Foo.Class.Nested.method();
```

## Interop Interfaces

When generating bindings for [interop interfaces](/guide/interop-interfaces), it's assumed the interface name has "I" prefix, so the associated implementation name will have first character removed. In case interface is declared under namespace, it'll be mirrored in JavaScript.

```csharp
[JSExport([
    typeof(IExported),
    typeof(Foo.IExported),
    typeof(Foo.Bar.IExported),
])]

interface IExported { void Method(); }
namespace Foo { interface IExported { void Method(); } }
namespace Foo.Bar { interface IExported { void Method(); } }
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
    [JSFunction]
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

You can control how namespaces are generated via `Space` patterns of [emit preferences](/guide/emit-prefs).
