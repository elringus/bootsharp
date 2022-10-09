export class Event<T extends any[]> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers = new Map<string, (...args: [...T]) => void>();
    private lastId = Number.MIN_SAFE_INTEGER;

    public broadcast(...args: [...T]) {
        for (const handler of this.handlers.values())
            handler(...args);
    }

    public subscribe(handler: (...args: [...T]) => void): string {
        const id = this.getOrDefineId(handler);
        this.subscribeById(id, handler);
        return id;
    }

    public subscribeById(id: string, handler: (...args: [...T]) => void): void {
        this.handlers.set(id, handler);
        console.log(`Subbed ${id}`);
    }

    public unsubscribe(handler: (...args: [...T]) => void) {
        if (handler == null) return;
        const id = this.getOrDefineId(handler);
        this.unsubscribeById(id);
    }

    public unsubscribeById(id: string) {
        if (this.handlers.has(id)) {
            this.handlers.delete(id);
            console.log(`Un-subbed ${id}`);
        } else console.log(`Failed to un-sub ${id}`);
    }

    private getOrDefineId(handler: (...args: [...T]) => void) {
        const idProperty = "dotnetEventHandlerId";
        if (handler.hasOwnProperty(idProperty))
            return handler[idProperty];
        const id = (++this.lastId).toString();
        Object.defineProperty(handler, idProperty, {
            value: id,
            enumerable: false,
            writable: false
        });
        return id;
    }
}

export interface EventBroadcaster<T extends any[]> {
    broadcast: (...args: [...T]) => void;
}

export interface EventSubscriber<T extends any[]> {
    subscribe: (handler: (...args: [...T]) => void) => string;
    unsubscribe: (handler: (...args: [...T]) => void) => void;
    subscribeById: (id: string, handler: (...args: [...T]) => void) => void;
    unsubscribeById: (id: string) => void;
}
