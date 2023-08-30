export class Event<T extends unknown[]> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers = new Map<string, (...args: [...T]) => void>();
    private readonly warn: ((message: string) => void) | null;
    private lastArgs: T | undefined;

    constructor(warn?: ((message: string) => void) | null) {
        this.warn = warn === undefined ? console.warn : warn;
    }

    public broadcast(...args: [...T]) {
        this.lastArgs = args;
        for (const handler of this.handlers.values())
            handler(...args);
    }

    public subscribe(handler: (...args: [...T]) => void): string {
        const id = this.getOrDefineId(handler);
        this.subscribeById(id, handler);
        return id;
    }

    public unsubscribe(handler: (...args: [...T]) => void) {
        if (handler == null) return;
        const id = this.getOrDefineId(handler);
        this.unsubscribeById(id);
    }

    public subscribeById(id: string, handler: (...args: [...T]) => void): void {
        if (this.handlers.has(id))
            this.warn?.(`Failed to subscribe event handler with ID '${id}': handler is already subscribed.`);
        else this.handlers.set(id, handler);
    }

    public unsubscribeById(id: string) {
        if (this.handlers.has(id))
            this.handlers.delete(id);
        else this.warn?.(`Failed to unsubscribe event handler with ID '${id}': handler is not subscribed.`);
    }

    public getLast() {
        return this.lastArgs;
    }

    private getOrDefineId(handler: (...args: [...T]) => void): string {
        const prop = "bootsharpEventHandlerId";
        if (handler.hasOwnProperty(prop))
            return (handler as unknown as { [index: string]: string })[prop];
        const id = crypto.randomUUID();
        Object.defineProperty(handler, prop, {
            value: id,
            enumerable: false,
            writable: false
        });
        return id;
    }
}

export interface EventBroadcaster<T extends unknown[]> {
    broadcast: (...args: [...T]) => void;
}

export interface EventSubscriber<T extends unknown[]> {
    subscribe: (handler: (...args: [...T]) => void) => string;
    unsubscribe: (handler: (...args: [...T]) => void) => void;
    getLast: () => T | undefined;
}
