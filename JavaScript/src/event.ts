export class Event<T extends any[]> implements EventBroadcaster<T>, EventSubscriber<T> {
    private readonly handlers = new Array<(...args: [...T]) => void>();

    public broadcast(...args: [...T]) {
        for (const handler of this.handlers)
            handler(...args);
    }

    public subscribe(handler: (...args: [...T]) => void) {
        this.handlers.push(handler);
    }

    public unsubscribe(handler: (...args: [...T]) => void) {
        const index = this.handlers.indexOf(handler);
        if (index >= 0) this.handlers.splice(index, 1);
    }
}

export interface EventBroadcaster<T extends any[]> {
    broadcast: (...args: [...T]) => void;
}

export interface EventSubscriber<T extends any[]> {
    subscribe: (handler: (...args: [...T]) => void) => void;
    unsubscribe: (handler: (...args: [...T]) => void) => void;
}
