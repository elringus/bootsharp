import { Event } from "./event.mjs";

/** A cancellation token compatible with C# `CancellationToken`. */
export class CancellationToken {
    /** Occurs when the token is cancelled. */
    readonly onCancellationRequested = new Event<[]>();

    /** Whether cancellation has been requested. */
    get isCancellationRequested(): boolean { return this.cancelled; }

    private cancelled = false;

    /** Signal cancellation. */
    cancel(): void {
        if (this.cancelled) return;
        this.cancelled = true;
        this.onCancellationRequested.broadcast();
    }
}
