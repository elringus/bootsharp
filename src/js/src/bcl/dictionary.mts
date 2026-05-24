/** A dictionary of key-value pairs compatible with C# `IDictionary<TKey, TValue>`. */
export class Dictionary<TKey, TValue> {
    protected readonly map: Map<TKey, TValue>;

    constructor(entries?: Iterable<[TKey, TValue]>) {
        this.map = new Map(entries);
    }

    /** Number of key-value pairs in the dictionary. */
    get count(): number {
        return this.map.size;
    }

    /** Associates the specified value with the specified key. */
    add(key: TKey, value: TValue): void {
        this.map.set(key, value);
    }

    /** Whether the dictionary contains the specified key. */
    containsKey(key: TKey): boolean {
        return this.map.has(key);
    }

    /** Removes the value with the specified key from the dictionary.
     *  @returns true when the key was removed; false when it wasn't found. */
    remove(key: TKey): boolean {
        return this.map.delete(key);
    }

    /** Removes all key-value pairs from the dictionary. */
    clear(): void {
        this.map.clear();
    }

    /** Returns the value associated with the specified key. */
    getAt(key: TKey): TValue {
        return this.map.get(key) as TValue;
    }

    /** Associates the specified value with the specified key. */
    setAt(key: TKey, value: TValue): void {
        this.map.set(key, value);
    }

    /** Returns a fresh array with a snapshot of the current keys. */
    getKeys(): TKey[] {
        return Array.from(this.map.keys());
    }

    /** Returns a fresh array with a snapshot of the current values. */
    getValues(): TValue[] {
        return Array.from(this.map.values());
    }

    [Symbol.iterator](): IterableIterator<[TKey, TValue]> {
        return this.map[Symbol.iterator]();
    }
}
