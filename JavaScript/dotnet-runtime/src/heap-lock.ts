interface HeapLock {
    release();
}

export let currentHeapLock: ManagedHeapLock | null = null;

export function assertHeapNotLocked() {
    if (currentHeapLock)
        throw "Heap is currently locked.";
}

class ManagedHeapLock implements HeapLock {
    stringCache = new Map<number, string | null>();

    private postReleaseActions?: Function[];

    release() {
        if (currentHeapLock !== this)
            throw "Trying to release a lock which isn't current.";

        currentHeapLock = null;

        while (this.postReleaseActions?.length) {
            const nextQueuedAction = this.postReleaseActions.shift()!;
            nextQueuedAction();
            assertHeapNotLocked();
        }
    }
}
