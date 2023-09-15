/** Optional configuration of an event instance. */
export type EventOptions<T> = {
    /** Custom warnings handler; by default <code>console.warn</code> is used. */
    warn?: (message: string) => void,
    /** When assigned, will transform broadcast payload with the handler. */
    convert?: (payload: unknown) => T
};

/** Allows attaching handlers and broadcasting events. */
export class Event<T> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers = new Map<string, (payload: T) => void>();
    private readonly warn: (message: string) => void;
    private readonly convert?: (payload: unknown) => T;
    private lastPayload?: T;

    /** Creates new event instance. */
    constructor(options?: EventOptions<T>) {
        this.convert = options?.convert;
        this.warn = options?.warn ?? console.warn;
    }

    /** Notifies attached handlers with specified payload.
     *  @param payload The payload of the notification. */
    public broadcast(payload: T) {
        this.lastPayload = payload;
        if (this.convert !== undefined)
            payload = this.convert(payload);
        for (const handler of this.handlers.values())
            handler(payload);
    }

    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    public subscribe(handler: (payload: T) => void): string {
        const id = this.getOrDefineId(handler);
        this.subscribeById(id, handler);
        return id;
    }

    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    public unsubscribe(handler: (payload: T) => void) {
        if (handler == null) return;
        const id = this.getOrDefineId(handler);
        this.unsubscribeById(id);
    }

    /** Attaches handler with specified identifier for events emitted by this event instance.
     *  @param id Identifier of the handler.
     *  @param handler The handler to attach. */
    public subscribeById(id: string, handler: (payload: T) => void): void {
        if (this.handlers.has(id))
            this.warn(`Failed to subscribe event handler with ID '${id}': handler is already subscribed.`);
        else this.handlers.set(id, handler);
    }

    /** Detaches handler with specified identifier from events emitted by this event instance.
     *  @param id Identifier of the handler. */
    public unsubscribeById(id: string) {
        if (this.handlers.has(id))
            this.handlers.delete(id);
        else this.warn(`Failed to unsubscribe event handler with ID '${id}': handler is not subscribed.`);
    }

    /** In case event was invoked at least once, returns last payload; undefined otherwise. */
    public getLast(): T | undefined {
        return this.lastPayload;
    }

    private getOrDefineId(handler: (payload: T) => void): string {
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
export interface EventBroadcaster<T> {
    /** Notifies attached handlers with specified payload argument.
     *  @param args The payload argument of the notification. */
    broadcast: (payload: T) => void;
}

/** Allows attaching event handlers. */
export interface EventSubscriber<T> {
    /** Attaches specified handler for events emitted by this event instance.
     *  @param handler The handler to attach. */
    subscribe: (handler: (payload: T) => void) => string;
    /** Detaches specified handler from events emitted by this event instance.
     *  @param handler The handler to detach. */
    unsubscribe: (handler: (payload: T) => void) => void;
    /** In case event was invoked at least once, returns last payload; undefined otherwise. */
    getLast: () => T | undefined;
}
