interface HeapLock {
    release(): any;
}
export declare let currentHeapLock: ManagedHeapLock | null;
export declare function assertHeapNotLocked(): void;
declare class ManagedHeapLock implements HeapLock {
    stringCache: Map<number, string | null>;
    private postReleaseActions?;
    release(): void;
}
export {};
