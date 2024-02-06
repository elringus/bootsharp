# Interop Interfaces

Instead of writing a binding for each method, make DotNetJS generate them automatically with `[JSImport]` and `[JSExport]` assembly attributes.

For example, let's say we have a JS frontend, which needs to be notified when a data is mutated on our C# backend, so it can render the updated state; additionally, our frontend may have a setting (eg, stored in browser cache) to temporary mute notifications, which needs to be retrieved by the backend. Create the following interface in C# to describe the expected frontend APIs:

```csharp
interface IFrontend
{
    void NotifyDataChanged (Data data);
    bool IsMuted ();
}
```

Now add the interface type to the JS import list:

```csharp
[assembly: JSImport(new[] { typeof(IFrontend) })]
```

DotNetJS will generate the following C# implementation:

```csharp
public class JSFrontend : IFrontend
{
    [JSFunction] public static void NotifyDataChanged (Data data) => JS.Invoke("dotnet.Frontend.notifyDataChanged.broadcast", new object[] { data });
    [JSFunction] public static bool IsMuted () => JS.Invoke<bool>("dotnet.Frontend.isMuted");

    void IFrontend.NotifyDataChanged (Data data) => NotifyDataChanged(data);
    bool IFrontend.IsMuted () => IsMuted();

}
```

— which you can use in C# to interop with the frontend and following TypeScript spec to be implemented on the frontend:

```ts
export namespace Frontend {
    export const notifyDataChanged: Event<[Data]>;
    export let isMuted: () => boolean;
}
```

Now let's say we want to provide an API for frontend to request mutation of the data:

```csharp
interface IBackend
{
    void AddData (Data data);
}
```

Export the interface to JS:

```csharp
[assembly: JSExport(new[] { typeof(IBackend) })]
```

Get the following implementation auto-generated:

```csharp
public class JSBackend
{
    private static IBackend handler = null!;

    public JSBackend (IBackend handler)
    {
        JSBackend.handler = handler;
    }

    [JSInvokable] public static void AddData (Data data) => handler.AddData(data);
}
```

— which will as well produce following spec to be consumed on JS side:

```ts
export namespace Backend {
    export function addData(data: Data): void;
}
```

Find example on using the attributes in the [React sample](https://github.com/Elringus/DotNetJS/blob/main/Samples/React).
