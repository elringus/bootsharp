using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bootsharp;

public unsafe ref struct Reader (long handle)
{
    private readonly byte* buffer = (byte*)(nint)handle;
    private int offset = 0;

    public void Dispose ()
    {
        NativeMemory.Free(buffer);
    }

    public int ReadMeta ()
    {
        uint result = 0;
        int shift = 0;
        byte next;
        var position = offset;
        do
        {
            next = buffer[position++];
            result |= (uint)(next & 0x7F) << shift;
            shift += 7;
        }
        while ((next & 0x80) != 0);
        offset = position;
        return (int)((result >> 1) ^ (uint)-(int)(result & 1));
    }

    public string? ReadString ()
    {
        var count = ReadMeta();
        if (count < 0) return null;
        var bytes = checked(count * 2);
        var result = new string((char*)(buffer + offset), 0, count);
        offset += bytes;
        return result;
    }

    public void ReadBytes (Span<byte> destination)
    {
        new ReadOnlySpan<byte>(buffer + offset, destination.Length).CopyTo(destination);
        offset += destination.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte ()
    {
        return buffer[offset++];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte ()
    {
        return (sbyte)ReadByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool ()
    {
        return ReadByte() != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16 ()
    {
        var result = ReadU16(buffer + offset);
        offset += 2;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16 ()
    {
        var result = unchecked((short)ReadU16(buffer + offset));
        offset += 2;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32 ()
    {
        var result = ReadU32(buffer + offset);
        offset += 4;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32 ()
    {
        var result = unchecked((int)ReadU32(buffer + offset));
        offset += 4;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64 ()
    {
        var result = ReadU64(buffer + offset);
        offset += 8;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64 ()
    {
        var result = unchecked((long)ReadU64(buffer + offset));
        offset += 8;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle ()
    {
        return BitConverter.Int32BitsToSingle(ReadInt32());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble ()
    {
        return BitConverter.Int64BitsToDouble(ReadInt64());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadU16 (byte* source)
    {
        ref var origin = ref Unsafe.AsRef<byte>(source);
        return Unsafe.ReadUnaligned<ushort>(ref origin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadU32 (byte* source)
    {
        ref var origin = ref Unsafe.AsRef<byte>(source);
        return Unsafe.ReadUnaligned<uint>(ref origin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadU64 (byte* source)
    {
        ref var origin = ref Unsafe.AsRef<byte>(source);
        return Unsafe.ReadUnaligned<ulong>(ref origin);
    }
}
