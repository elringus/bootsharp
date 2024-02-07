# Interop Interfaces

Instead of manually authoring a binding for each method, make Bootsharp generate them automatically with `[JSImport]` and `[JSExport]` assembly attributes.

For example, say we have a JavaScript UI (frontend), which needs to be notified when a data is mutated on the C# domain layer (backend), so it can render the updated state; additionally, our frontend may have a setting (eg, stored in browser cache) to temporary mute notifications, which needs to be retrieved by the backend. Create the following interface in C# to describe the expected frontend APIs:

```csharp
interface IFrontend
{
    void NotifyDataChanged (Data data);
    bool IsMuted ();
}
```

Now add the interface type to the JS import list:

```csharp
[assembly: JSImport([
    typeof(IFrontend)
])]
```

Bootsharp will generate following C# implementation:

```csharp
public static partial class JSFrontend : IFrontend
{
    [JSFunction] public static partial void NotifyDataChanged (Data data);
    [JSFunction] public static partial bool IsMuted ();

    void IFrontend.NotifyDataChanged (Data data) => NotifyDataChanged(data);
    bool IFrontend.IsMuted () => IsMuted();
}
```

— which you can use in C# to interop with the frontend and following TypeScript spec to be implemented on the frontend:

```ts
export namespace Frontend {
    export const onDataChanged: Event<[Data]>;
    export let isMuted: () => boolean;
}
```

Now say we want to provide an API for frontend to request mutation of the data:

```csharp
interface IBackend
{
    void AddData (Data data);
}
```

Export the interface to JavaScript:

```csharp
[assembly: JSExport([
    typeof(IBackend)
])]
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

    [JSInvokable]
    public static void AddData (Data data) => handler.AddData(data);
}
```

— which will produce following spec to be consumed on JavaScript side:

```ts
export namespace Backend {
    export function addData(data: Data): void;
}
```

To make Bootsharp automatically inject and inititliaize generate interop implementations, use [dependency injection](/guide/extensions/dependency-injection) extension.

::: tip Example
Find example on using interop interfaces in the [React sample](https://github.com/elringus/bootsharp/tree/main/samples/react).
:::
