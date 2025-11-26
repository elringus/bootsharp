# Interop Interfaces

Instead of manually authoring a binding for each method, let Bootsharp generate them automatically using the `[JSImport]` and `[JSExport]` assembly attributes.

For example, say we have a JavaScript UI (frontend) that needs to be notified when data is mutated in the C# domain layer (backend), so it can render the updated state. Additionally, the frontend may have a setting (e.g., stored in the browser cache) to temporarily mute notifications, which the backend needs to retrieve. You can create the following interface in C# to describe the expected frontend APIs:

```csharp
interface IFrontend
{
    void NotifyDataChanged (Data data);
    bool IsMuted ();
}
```

Now, add the interface type to the JS import list:

```csharp
[assembly: JSImport([
    typeof(IFrontend)
])]
```

Bootsharp will automatically implement the interface in C#, wiring it to JavaScript, while also providing you with a TypeScript spec to implement on the frontend:

```ts
export namespace Frontend {
    export const onDataChanged: Event<[Data]>;
    export let isMuted: () => boolean;
}
```

Now, say we want to provide an API for the frontend to request a mutation of the data:

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

This will generate the following implementation:

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

— which will produce the following spec to be consumed on the JavaScript side:

```ts
export namespace Backend {
    export function addData(data: Data): void;
}
```

To make Bootsharp automatically inject and initialize the generated interop implementations, use the [dependency injection](/guide/extensions/dependency-injection) extension.

::: tip Example
Find an example of using interop interfaces in the [React sample](https://github.com/elringus/bootsharp/tree/main/samples/react).
:::
