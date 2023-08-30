﻿/** Allows attaching handlers and broadcasting events. */
export class Event<T extends unknown[]> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers = new Map<string, (...args: [...T]) => void>();
    private readonly warn: ((message: string) => void) | null;
    private lastArgs: T | undefined;

    /** Creates new event instance.
     *  @param warn Custom warnings handler; by default <code>console.warn</code> is used. */
    constructor(warn?: ((message: string) => void) | null) {
        this.warn = warn === undefined ? console.warn : warn;
    }

    /** Notifies attached handlers with specified payload arguments.
     *  @param args The payload arguments of the notification. */
    public broadcast(...args: [...T]) {
        this.lastArgs = args;
        for (const handler of this.handlers.values())
            handler(...args);
    }

    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    public subscribe(handler: (...args: [...T]) => void): string {
        const id = this.getOrDefineId(handler);
        this.subscribeById(id, handler);
        return id;
    }

    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    public unsubscribe(handler: (...args: [...T]) => void) {
        if (handler == null) return;
        const id = this.getOrDefineId(handler);
        this.unsubscribeById(id);
    }

    /** Attaches handler with specified identifier for events emitted by this event instance.
     *  @param id Identifier of the handler.
     *  @param handler The handler to attach. */
    public subscribeById(id: string, handler: (...args: [...T]) => void): void {
        if (this.handlers.has(id))
            this.warn?.(`Failed to subscribe event handler with ID '${id}': handler is already subscribed.`);
        else this.handlers.set(id, handler);
    }

    /** Detaches handler with specified identifier from events emitted by this event instance.
     *  @param id Identifier of the handler. */
    public unsubscribeById(id: string) {
        if (this.handlers.has(id))
            this.handlers.delete(id);
        else this.warn?.(`Failed to unsubscribe event handler with ID '${id}': handler is not subscribed.`);
    }

    /** In case event was invoked at least once, returns specified arguments; undefined otherwise. */
    public getLast(): T | undefined {
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

/** Allows broadcasting events. */
export interface EventBroadcaster<T extends unknown[]> {
    /** Notifies attached handlers with specified payload arguments.
     *  @param args The payload arguments of the notification. */
    broadcast: (...args: [...T]) => void;
}

/** Allows attaching event handlers. */
export interface EventSubscriber<T extends unknown[]> {
    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    subscribe: (handler: (...args: [...T]) => void) => string;
    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    unsubscribe: (handler: (...args: [...T]) => void) => void;
    /** In case event was invoked at least once, returns specified arguments; undefined otherwise. */
    getLast: () => T | undefined;
}
