# Interop Instances

When an interface is supplied as argument or return type of an interop method, instead of serializing it as value, Bootsharp will instead generate an instance binding, eg:

```csharp
public interface IExported { string GetFromCSharp (); }
public interface IImported { string GetFromJavaScript (); }

public class Exported : IExported
{
    public string GetFromCSharp () => "cs";
}

public static partial class Factory
{
    [JSInvokable] public static IExported GetExported () => new Exported();
    [JSFunction] public static partial IImported GetImported ();
}

var imported = Factory.GetImported();
imported.GetFromJavaScript(); //returns "js"
```

```ts
import { Factory, IImported } from "bootsharp";

class Imported implements IImported {
    getFromJavaScript() { return "js"; }
}

Factory.getImported = () => new Imported();

const exported = Factory.getExported();
exported.getFromCSharp(); // returns "cs"
```

Interop instances are subject to the following limitations:
- Can't be args or return values of other interop instance method
- Can't be args of events
- Interfaces from "System" namespace are not qualified
