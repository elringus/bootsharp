# Namespaces

Bootsharp projects each C# namespace into its own ES module. The full namespace becomes the import path; individual classes, enums and interface bindings inside that namespace become flat top-level exports of that module.

The slug rule is: PascalCase → kebab-case, dot → directory separator. `Foo.Bar` → `foo/bar`. `MyRootSpace.MyOtherSpace` → `my-root-space/my-other-space`.

## Static Members

The C# namespace of the declaring type maps to a sub-path under the Bootsharp module; the declaring class becomes a flat `export const`:

```csharp
class Class { [Export] static void Method() {} }
namespace Foo { class Class { [Export] static void Method() {} } }
namespace Foo.Bar { class Class { [Export] static void Method() {} } }
```

```ts
import { Class as Root } from "bootsharp"; // root-namespace members re-exported from the entry
import { Class as FooClass } from "bootsharp/foo";
import { Class as FooBarClass } from "bootsharp/foo/bar";

Root.method();
FooClass.method();
FooBarClass.method();
```

Bindings declared without any C# namespace live in `bootsharp/index` and are re-exported from the package entry, so root-namespace types remain importable directly from `"bootsharp"`.

Methods inside nested classes are emitted under the containing class's binding inside the namespace's module file:

```csharp
namespace Foo;

public class Class
{
    public class Nested { [Export] public static void Method() {} }
}
```

```ts
import { Class } from "bootsharp/foo";

Class.Nested.method();
```

## Interop Modules

When generating bindings for [modules](/guide/interop-modules), the JS export uses the C# type name as-is. The C# namespace maps to the import path the same way it does for static members:

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
import { IExported as Root } from "bootsharp";
import { IExported as FooExported } from "bootsharp/foo";
import { Exported as FooBarExported } from "bootsharp/foo/bar";

Root.method();
FooExported.method();
FooBarExported.method();
```

## Types

Custom types referenced in API signatures (records, classes, interfaces, etc) are declared as top-level exports of their respective namespace module:

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
import { Class, type Record } from "bootsharp";
import type { Record as FooRecord } from "bootsharp/foo";

Class.method = methodImpl;

function methodImpl(r: Record): FooRecord {
    // ...
}
```

## Configuring Namespaces

You can control how the C#-side namespace path resolves to the generated module path with the `Space` option in [preferences](/guide/preferences). A pref that rewrites `Foo.Bar.SomeClass` to `Bar.NewClass` will emit the binding into `bootsharp/bar` under the name `NewClass`.
