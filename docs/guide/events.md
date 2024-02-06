# Events

To make a C# method act as event broadcaster for JavaScript consumers, annotate it with `[JSEvent]` attribute:

```csharp
[JSEvent]
public static partial void OnSomethingChanged (string payload);
```

— and consume it from JavaScript as follows:

```js
Program.onSomethingChanged.subscribe(handleSomething);
Program.onSomethingChanged.unsubscribe(handleSomething);

function handleSomething(payload) {

}
```

When the method in invoked in C#, subscribed JavaScript handlers will be notified. In TypeScript the event will have typed generic declaration corresponding to the event arguments.

## React Event Hooks

Below are some utility hooks, which you can use in React for convenience:

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
