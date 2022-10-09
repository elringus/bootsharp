import { DependencyList, useEffect } from "react";
import { EventSubscriber } from "backend";

export function useEvent<T extends any[]>(event: EventSubscriber<T>,
    handler: (...args: [...T]) => void, deps?: DependencyList | undefined) {
    useEffect(() => {
        const subscribePromise = event.subscribe(handler);
        return () => {
            (async () => {
                await subscribePromise;
                await event.unsubscribe(handler);
            })();
        };
    }, deps);
}
