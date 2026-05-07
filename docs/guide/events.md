# Events

To expose a C# event to JavaScript consumers, declare a static event and annotate it with `[Export]`:

```csharp
[Export]
public static event Action<string>? OnSomethingChanged;

public static void UpdateSomething (string payload)
{
    OnSomethingChanged?.Invoke(payload);
}
```

— and consume it from JavaScript as follows:

```ts
Program.onSomethingChanged.subscribe(handleSomething);
Program.onSomethingChanged.unsubscribe(handleSomething);

function handleSomething(payload: string) {

}
```

When the event is raised in C#, subscribed JavaScript handlers will be notified. In TypeScript exported events are declared as `EventSubscriber<...>` with argument types inferred from the event delegate signature.

To use a JavaScript event from C#, declare a static event on a partial type and annotate it with `[Import]`:

```csharp
[Import]
public static event Action<string>? OnSomethingChanged;
```

JavaScript will see it as an `EventBroadcaster`:

```ts
Program.onSomethingChanged.broadcast("updated");
```

Bootsharp supports all common event types: `Action`, `EventHandler`, and any custom delegate types without a return type.

Events on [modules](/guide/interop-modules) and [instances](/guide/interop-instances) are picked up automatically, so you don't have to annotate them.

## React Event Hooks

Below are sample React utility hooks, which you may find useful:

```ts
export function useEvent<T extends unknown[]>(
    event: EventSubscriber<T>, handler: (...args: [...T]) => void,
    deps?: DependencyList | undefined, destructor?: () => void) {
    useEffect(() => {
        event.subscribe(handler);
        return () => {
            event.unsubscribe(handler);
            destructor?.();
        };
    }, [event, handler, destructor, ...(deps ?? [])]);
}

export function useEventState<T extends unknown[]>(
    event: EventSubscriber<T>,
    defaultState?: T[0]): T[0] | undefined {
    const initial = event.last === undefined ?
        defaultState : getFirstArg(event.last);
    const [state, setState] = useState<T[0] | undefined>(initial);
    useEvent(event, (...args) => setState(getFirstArg(args)), []);
    return state;

    function getFirstArg(args: T): T[0] | undefined {
        return args[0] === null ? undefined : args[0];
    }
}
```

The `useEventState` hook will take care of both subscribing and unsubscribing from the dotnet event when component unmounts and using last event args as the default state to catch up in case the component missed a broadcast before being mounted.

```tsx
const SomeComponent = () => {
    const payload = useEventState(Program.onSomethingChanged);
    return <>{payload}</>;
};
```
