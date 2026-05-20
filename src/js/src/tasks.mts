/** Tracks JS-side pending promises awaiting completion from C#.
 *  Used by Bootsharp's generated async bridge for JS → C# exports that return Task/Task&lt;T&gt;.
 *  Storage is a sparse array indexed by integer ID, with a free-list of recycled IDs. */

type Slot = { resolve: (value: unknown) => void, reject: (reason?: unknown) => void };

const slots: (Slot | null)[] = [];
const freeList: number[] = [];

export const pendingExports = {
    /** Reserves a slot for the specified resolve/reject callbacks and returns its identifier. */
    alloc(resolve: (value: unknown) => void, reject: (reason?: unknown) => void): number {
        const slot: Slot = { resolve, reject };
        if (freeList.length > 0) {
            const id = freeList.pop()!;
            slots[id] = slot;
            return id;
        }
        slots.push(slot);
        return slots.length - 1;
    },
    /** Resolves the pending promise with the specified value and recycles the slot. */
    resolve(id: number, value?: unknown): void {
        const slot = slots[id]!;
        slots[id] = null;
        freeList.push(id);
        slot.resolve(value);
    },
    /** Rejects the pending promise with the specified reason and recycles the slot. */
    reject(id: number, reason?: unknown): void {
        const slot = slots[id]!;
        slots[id] = null;
        freeList.push(id);
        slot.reject(new Error(typeof reason === "string" ? reason : String(reason)));
    }
};
