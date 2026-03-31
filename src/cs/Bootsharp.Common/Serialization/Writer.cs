using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bootsharp;

public unsafe ref struct Writer ()
{
    private const int initialCapacity = 256;
    private byte* buffer = (byte*)NativeMemory.Alloc(initialCapacity);
    private int capacity = initialCapacity;
    private int offset = 0;

    public long Detach ()
    {
        var handle = (long)(nint)buffer;
        buffer = null;
        capacity = 0;
        offset = 0;
        return handle;
    }

    public void WriteMeta (int value)
    {
        var zigzag = (uint)((value << 1) ^ (value >> 31));
        Ensure(5);
        var position = offset;
        while (zigzag >= 0x80)
        {
            buffer[position++] = (byte)(zigzag | 0x80);
            zigzag >>= 7;
        }
        buffer[position++] = (byte)zigzag;
        offset = position;
    }

    public void WriteString (string? value)
    {
        if (value is null)
        {
            WriteMeta(-1);
            return;
        }
        WriteMeta(value.Length);
        var bytes = value.Length * 2;
        Ensure(bytes);
        value.CopyTo(new((char*)(buffer + offset), value.Length));
        offset += bytes;
    }

    public void WriteBytes (ReadOnlySpan<byte> value)
    {
        Ensure(value.Length);
        value.CopyTo(new(buffer + offset, value.Length));
        offset += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte (byte value)
    {
        Ensure(1);
        buffer[offset++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte (sbyte value)
    {
        WriteByte((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool (bool value)
    {
        WriteByte(value ? (byte)1 : (byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16 (ushort value)
    {
        Ensure(2);
        WriteU16(buffer + offset, value);
        offset += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16 (short value)
    {
        Ensure(2);
        WriteU16(buffer + offset, unchecked((ushort)value));
        offset += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32 (uint value)
    {
        Ensure(4);
        WriteU32(buffer + offset, value);
        offset += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32 (int value)
    {
        Ensure(4);
        WriteU32(buffer + offset, unchecked((uint)value));
        offset += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64 (ulong value)
    {
        Ensure(8);
        WriteU64(buffer + offset, value);
        offset += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64 (long value)
    {
        Ensure(8);
        WriteU64(buffer + offset, unchecked((ulong)value));
        offset += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingle (float value)
    {
        WriteInt32(BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble (double value)
    {
        WriteInt64(BitConverter.DoubleToInt64Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteU16 (byte* destination, ushort value)
    {
        ref var target = ref Unsafe.AsRef<byte>(destination);
        Unsafe.WriteUnaligned(ref target, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteU32 (byte* destination, uint value)
    {
        ref var target = ref Unsafe.AsRef<byte>(destination);
        Unsafe.WriteUnaligned(ref target, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteU64 (byte* destination, ulong value)
    {
        ref var target = ref Unsafe.AsRef<byte>(destination);
        Unsafe.WriteUnaligned(ref target, value);
    }

    private void Ensure (int count)
    {
        if (capacity - offset >= count) return;
        capacity = Math.Max(capacity * 2, offset + count);
        buffer = (byte*)NativeMemory.Realloc(buffer, (nuint)capacity);
    }
}
