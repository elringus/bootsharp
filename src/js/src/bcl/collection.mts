/** A collection of items compatible with C# `ICollection<T>`. */
export class Collection<T> {
    protected readonly items: T[];

    constructor(items?: Iterable<T>) {
        this.items = items != null ? Array.from(items) : [];
    }

    /** Number of items in the collection. */
    get count(): number {
        return this.items.length;
    }

    /** Adds the specified item to the collection. */
    add(item: T): void {
        this.items.push(item);
    }

    /** Removes the first occurrence of the specified item from the collection.
     *  @returns true when the item was removed; false when it wasn't found. */
    remove(item: T): boolean {
        const idx = this.items.indexOf(item);
        if (idx < 0) return false;
        this.items.splice(idx, 1);
        return true;
    }

    /** Removes all items from the collection. */
    clear(): void {
        this.items.length = 0;
    }

    /** Whether the collection contains the specified item. */
    contains(item: T): boolean {
        return this.items.indexOf(item) >= 0;
    }

    /** Returns a fresh array with a snapshot of the current items. */
    copy(): T[] {
        return this.items.slice();
    }

    [Symbol.iterator](): IterableIterator<T> {
        return this.copy()[Symbol.iterator]();
    }
}
