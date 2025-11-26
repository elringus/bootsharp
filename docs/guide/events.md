# Events

To make a C# method act as event broadcaster for JavaScript consumers, annotate it with `[JSEvent]` attribute:

```csharp
[JSEvent]
public static partial void OnSomethingChanged (string payload);
```

— and consume it from JavaScript as follows:

```ts
Program.onSomethingChanged.subscribe(handleSomething);
Program.onSomethingChanged.unsubscribe(handleSomething);

function handleSomething(payload: string) {

}
```

When the method in invoked in C#, subscribed JavaScript handlers will be notified. In TypeScript the event will have typed generic declaration corresponding to the event arguments.

## Events in Interop Interfaces

To make a method in an [interop interface](/guide/interop-interfaces) act as event broadcaster, make its name start with "Notify". Such methods will be detected by Bootsharp and exposed to JavaScript as events with "Notify" changed to "On". For example, `NotifyUserUpdated` C# method will be exposed as `OnUserUpdated` JavaScript event.

Which interface methods are considered events and the way they are named in JavaScript can be customized with [emit preferences](/guide/emit-prefs).

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
