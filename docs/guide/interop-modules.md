# Interop Modules

Instead of manually authoring a binding for each member, let Bootsharp generate them automatically using the `[Import]` and `[Export]` assembly attributes. The type listed under each attribute defines an *interop module*.

For example, say we have a JavaScript UI (frontend) with a setting stored on the JS side, and a C# domain layer (backend) that wants to expose state changes back to JavaScript. You can describe the imported frontend module like this:

```csharp
interface IFrontend
{
    bool IsMuted { get; set; }
}
```

Now, add the module type to the JS import list:

```csharp
[assembly: Import(typeof(IFrontend))]
```

Bootsharp will automatically implement the interface in C#, wiring it to JavaScript, while also providing you with a TypeScript spec to implement on the frontend:

```ts
export namespace Frontend {
    export let isMuted: boolean;
}
```

Imported modules must be interfaces, since Bootsharp generates the C# implementation that calls into JavaScript.

Now, define the backend contract to expose to JavaScript. An exported module can be either an interface or a non-static class — pick whichever fits your backend best:

```csharp
public interface IBackend
{
    event Action<Data> OnDataChanged;
    Data? Current { get; set; }
    void AddData (Data data);
}
```

```csharp
public class Backend
{
    public event Action<Data>? OnDataChanged;
    public Data? Current { get; set; }
    public void AddData (Data data) { /* ... */ }
}
```

Export the module to JavaScript:

```csharp
[assembly: Export(typeof(IBackend))]
// or
[assembly: Export(typeof(Backend))]
```

Either form produces the following spec to be consumed on the JavaScript side:

```ts
export namespace Backend {
    export const onDataChanged: EventSubscriber<[data: Data]>;
    export let current: Data | undefined;
    export function addData(data: Data): void;
}
```

Imported module events work the other way around: declare a real C# event on the interface, and Bootsharp will generate a JavaScript `EventBroadcaster` plus a regular subscribable event on the generated C# implementation.

To make Bootsharp automatically inject and initialize the generated interop implementations, use the [dependency injection](/guide/extensions/dependency-injection) extension.

::: tip Example
Find an example of using modules in the [React sample](https://github.com/elringus/bootsharp/tree/main/samples/react).
:::
