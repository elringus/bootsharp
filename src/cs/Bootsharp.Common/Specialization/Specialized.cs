using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bootsharp;

/// <summary>
/// Built-in specializations Bootsharp ships by default; backed by JS implementations under src/bcl.
/// User can add custom specializations using the same attributes.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Not practical due to coupling with the generated code; covered in E2E.")]
public static class Specialized
{
    private const string iterJS = "[Symbol.iterator]() { return this.copy()[Symbol.iterator](); }";
    private const string iterDecl = "[Symbol.iterator](): IterableIterator<T>;";

    [SpecializeImport(typeof(ICollection<>), JS: iterJS, Decl: iterDecl)]
    public abstract class CollectionImport<T> (int id) : SpecializedImport(id), ICollection<T>
    {
        public abstract int Count { get; }
        public bool IsReadOnly => false;

        public abstract void Add (T item);
        public abstract bool Remove (T item);
        public abstract void Clear ();
        public abstract bool Contains (T item);
        public abstract T[] Copy ();

        public void CopyTo (T[] array, int arrayIndex)
        {
            foreach (var item in Copy())
                array[arrayIndex++] = item;
        }

        public IEnumerator<T> GetEnumerator ()
        {
            foreach (var item in Copy())
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
    }

    [SpecializeExport(typeof(ICollection<>))]
    public class CollectionExport<T> (ICollection<T> cl) : SpecializedExport(cl)
    {
        public int Count => cl.Count;
        public void Add (T item) => cl.Add(item);
        public bool Remove (T item) => cl.Remove(item);
        public void Clear () => cl.Clear();
        public bool Contains (T item) => cl.Contains(item);
        public T[] Copy () => cl.ToArray();
    }

    [SpecializeImport(typeof(IList<>), JS: iterJS, Decl: iterDecl)]
    public abstract class ListImport<T> (int id) : CollectionImport<T>(id), IList<T>
    {
        public T this [int index] { get => GetAt(index); set => SetAt(index, value); }
        public abstract int IndexOf (T item);
        public abstract void Insert (int index, T item);
        public abstract void RemoveAt (int index);
        public abstract T GetAt (int index);
        public abstract void SetAt (int index, T item);
    }

    [SpecializeExport(typeof(IList<>))]
    public class ListExport<T> (IList<T> list) : CollectionExport<T>(list)
    {
        public int IndexOf (T item) => list.IndexOf(item);
        public void Insert (int index, T item) => list.Insert(index, item);
        public void RemoveAt (int index) => list.RemoveAt(index);
        public T GetAt (int index) => list[index];
        public void SetAt (int index, T item) => list[index] = item;
    }

    [SpecializeImport(
        typeof(IDictionary<,>),
        JS: """
            [Symbol.iterator]() {
                const keys = this.getKeys(), values = this.getValues();
                return keys.map((key, i) => [key, values[i]])[Symbol.iterator]();
            }
            """,
        Decl: "[Symbol.iterator](): IterableIterator<[TKey, TValue]>;")]
    public abstract class DictionaryImport<TKey, TValue> (int id) : SpecializedImport(id), IDictionary<TKey, TValue>
    {
        public abstract int Count { get; }
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => GetKeys();
        public ICollection<TValue> Values => GetValues();
        public TValue this [TKey key] { get => GetAt(key); set => SetAt(key, value); }

        public abstract void Add (TKey key, TValue value);
        public abstract bool ContainsKey (TKey key);
        public abstract bool Remove (TKey key);
        public abstract void Clear ();
        public abstract TValue GetAt (TKey key);
        public abstract void SetAt (TKey key, TValue value);
        public abstract TKey[] GetKeys ();
        public abstract TValue[] GetValues ();

        public bool TryGetValue (TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (!ContainsKey(key))
            {
                value = default;
                return false;
            }
            value = GetAt(key);
            return true;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add (KeyValuePair<TKey, TValue> kv) => Add(kv.Key, kv.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains (KeyValuePair<TKey, TValue> kv) => ContainsKey(kv.Key);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove (KeyValuePair<TKey, TValue> kv) => Remove(kv.Key);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var pair in this)
                array[arrayIndex++] = pair;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
        {
            var keys = GetKeys();
            var values = GetValues();
            for (var i = 0; i < keys.Length; i++)
                yield return new(keys[i], values[i]);
        }

        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
    }

    [SpecializeExport(typeof(IDictionary<,>))]
    public class DictionaryExport<TKey, TValue> (IDictionary<TKey, TValue> dic) : SpecializedExport(dic)
    {
        public int Count => dic.Count;
        public void Add (TKey key, TValue value) => dic.Add(key, value);
        public bool ContainsKey (TKey key) => dic.ContainsKey(key);
        public bool Remove (TKey key) => dic.Remove(key);
        public void Clear () => dic.Clear();
        public TValue GetAt (TKey key) => dic[key];
        public void SetAt (TKey key, TValue value) => dic[key] = value;
        public TKey[] GetKeys () => dic.Keys.ToArray();
        public TValue[] GetValues () => dic.Values.ToArray();
    }

    [SpecializeImport(typeof(CancellationToken))]
    public abstract class CancellationTokenImport (int id) : SpecializedImport(id)
    {
        public abstract event Action OnCancellationRequested;
        public abstract bool IsCancellationRequested { get; }

        internal static readonly ConditionalWeakTable
            <CancellationTokenSource, CancellationTokenImport> ImportBySrc = new();
        private CancellationTokenSource? src;

        protected internal override object Unwrap ()
        {
            if (src == null)
            {
                ImportBySrc.Add(src = new(), this);
                if (IsCancellationRequested) src.Cancel();
                else OnCancellationRequested += src.Cancel;
            }
            return src.Token;
        }
    }

    [SpecializeExport(typeof(CancellationToken))]
    public sealed class CancellationTokenExport : SpecializedExport
    {
        public event Action? OnCancellationRequested;
        public bool IsCancellationRequested => ct.IsCancellationRequested;

        private readonly CancellationToken ct;

        public CancellationTokenExport (CancellationToken ct) : base(Resolve(ct))
        {
            this.ct = ct;
            if (ct.CanBeCanceled && !ct.IsCancellationRequested)
                ct.Register(() => OnCancellationRequested?.Invoke());
        }

        private static object Resolve (CancellationToken ct)
        {
            // Preserving the imported token's cancellation sources identity
            // when they round trip via an exported interop API.
            if (GetSource(ref ct) is not { } src) return ct;
            return CancellationTokenImport.ImportBySrc.TryGetValue(src, out var i) ? i : ct;
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_source")]
            static extern ref CancellationTokenSource? GetSource (ref CancellationToken ct);
        }
    }
}
