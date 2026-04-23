# Interop Instances

When an interface is supplied as argument or return type of an interop method, instead of serializing it as value, Bootsharp will instead generate an instance binding, eg:

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

Interop instances are subject to the following limitations:
- Can't be args or return values of other interop instance method
- Interfaces from "System" namespace are not qualified
