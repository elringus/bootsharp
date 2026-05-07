# Interop Instances

When a type with a mutable semantic (a class or an interface) appears on the interop boundary or under a [serialized type](/guide/serialization), instead of serializing and copying it by value, Bootsharp will instead generate an instance binding and pass it by reference, eg:

```csharp
public interface IExported
{
    string Value { get; set; }
    string GetFromCSharp ();
}

public interface IImported
{
    string Value { get; set; }
    string GetFromJavaScript ();
}

public class Exported : IExported
{
    public string Value { get; set; } = "cs";
    public string GetFromCSharp () => "cs";
}

public static partial class Factory
{
    [Export] public static IExported GetExported () => new Exported();
    [Import] public static partial IImported GetImported ();
}

var imported = Factory.GetImported();
imported.GetFromJavaScript(); // returns "js"
imported.value = "updated"; // invokes the JS setter
_ = imported.value; // invokes the JS getter
```

```ts
import { Factory, IImported } from "bootsharp";

class Imported implements IImported {
    value = "js";
    getFromJavaScript() { return "js"; }
}

Factory.getImported = () => new Imported();

const exported = Factory.getExported();
exported.getFromCSharp(); // returns "cs"
exported.value = "updated"; // invokes the C# setter
_ = exported.value; // invokes the C# getter
```

::: info NOTE
Only user types are subject to instance binding. BCL types are ignored to prevent leaking the entire .NET runtime into the generated interop layer.
:::
