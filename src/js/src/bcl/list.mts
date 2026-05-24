import { Collection } from "./collection.mjs";

/** A list of items compatible with C# `IList<T>`. */
export class List<T> extends Collection<T> {
    /** Returns the item at the specified index. */
    getAt(index: number): T {
        return this.items[index];
    }

    /** Assigns the specified item at the specified index. */
    setAt(index: number, item: T): void {
        this.items[index] = item;
    }

    /** Returns the index of the first occurrence of the specified item, or -1 when not found. */
    indexOf(item: T): number {
        return this.items.indexOf(item);
    }

    /** Inserts the specified item at the specified index. */
    insert(index: number, item: T): void {
        this.items.splice(index, 0, item);
    }

    /** Removes the item at the specified index. */
    removeAt(index: number): void {
        this.items.splice(index, 1);
    }
}
