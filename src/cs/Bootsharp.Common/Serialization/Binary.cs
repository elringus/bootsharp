namespace Bootsharp;

public delegate void WriteBinary<T> (ref Writer writer, T? value);
public delegate T? ReadBinary<T> (ref Reader reader);

public sealed class Binary<T> (WriteBinary<T> write, ReadBinary<T> read)
{
    public void Write (ref Writer writer, T? value) => write(ref writer, value);
    public T? Read (ref Reader reader) => read(ref reader);
}
