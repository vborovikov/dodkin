namespace Dodkin.Dispatch;

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Unicode;

interface IBufferReader<T>
{
    int WrittenCount { get; }
    ReadOnlyMemory<T> WrittenMemory { get; }
    ReadOnlySpan<T> WrittenSpan { get; }
}

class Utf8MemoryBuffer : IBufferReader<byte>, IBufferWriter<byte>, IDisposable
{
    private const int KByte = 1024;
    private const int DefaultInitialBufferSize = 32 * KByte;

    private byte[]? buffer;
    private Memory<byte> memory;
    private readonly IFormatProvider? provider;
    private int bytesWritten;

    public Utf8MemoryBuffer()
        : this(provider: default)
    {
    }

    public Utf8MemoryBuffer(IFormatProvider? provider = null)
    {
        this.buffer = ArrayPool<byte>.Shared.Rent(DefaultInitialBufferSize);
        this.memory = this.buffer;
        this.provider = provider;
    }

    public Utf8MemoryBuffer(Memory<byte> destination, IFormatProvider? provider = null)
    {
        this.memory = destination;
        this.provider = provider;
    }

    public void Dispose()
    {
        var rentedBuffer = this.buffer;
        if (rentedBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
            this.buffer = null;
            this.memory = Memory<byte>.Empty;
        }
    }

    public int WrittenCount => this.HasCapacity ? this.bytesWritten : this.memory.Length;

    public ReadOnlyMemory<byte> WrittenMemory => this.memory[..this.bytesWritten];

    public ReadOnlySpan<byte> WrittenSpan => this.memory.Span[..this.bytesWritten];

    private bool CanAppend => this.bytesWritten < this.memory.Length;

    public bool HasCapacity => this.bytesWritten <= this.memory.Length;

    public int FreeCapacity => this.memory.Length - this.bytesWritten;

    public void Clear()
    {
        Debug.Assert(this.memory.Length >= this.bytesWritten);
        this.bytesWritten = 0;
    }

    public void Advance(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (this.bytesWritten > this.memory.Length - count)
            throw new InvalidOperationException();

        this.bytesWritten += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);

        if (this.CanAppend)
        {
            var bytesAvailable = this.memory.Length - this.bytesWritten;
            if (sizeHint == 0 || bytesAvailable >= sizeHint)
            {
                return this.memory[this.bytesWritten..];
            }
        }

        return Memory<byte>.Empty;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);

        if (this.CanAppend)
        {
            var bytesAvailable = this.memory.Length - this.bytesWritten;
            if (sizeHint == 0 || bytesAvailable >= sizeHint)
            {
                return this.memory.Span[this.bytesWritten..];
            }
        }

        return [];
    }

    public bool TryAppend(char ch)
    {
        if (!this.CanAppend)
            return false;

        if (char.IsAscii(ch))
        {
            this.memory.Span[this.bytesWritten++] = (byte)ch;
        }
        else if (Utf8.TryWrite(this.memory.Span[this.bytesWritten..], $"{ch}", out var chBytesWritten))
        {
            this.bytesWritten += chBytesWritten;
        }
        else
        {
            return false;
        }

        return true;
    }

    public bool TryAppend(ReadOnlySpan<char> str)
    {
        if (this.CanAppend && Utf8.FromUtf16(str, this.memory.Span[this.bytesWritten..], out _, out var spanBytesWritten) == OperationStatus.Done)
        {
            this.bytesWritten += spanBytesWritten;
            return true;
        }

        return false;
    }

    public bool TryFormat<T>(T value, ReadOnlySpan<char> format = default) where T : struct, IUtf8SpanFormattable
    {
        if (this.CanAppend && value.TryFormat(this.memory.Span[this.bytesWritten..], out var valueBytesWritten, format, this.provider))
        {
            this.bytesWritten += valueBytesWritten;
            return true;
        }

        return false;
    }

    public bool TrySerialize<T>(T value) where T : notnull
    {
        if (!this.CanAppend)
            return false;

        using var jsonWriter = new Utf8JsonWriter(this);
        JsonSerializer.Serialize(jsonWriter, value, value.GetType());
        jsonWriter.Flush();

        return this.CanAppend;
    }

    private void CheckAndResizeBuffer(int sizeHint)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        if (sizeHint == 0)
        {
            sizeHint = 1;
        }

        if (sizeHint > this.FreeCapacity && this.buffer is not null)
        {
            var currentLength = this.buffer.Length;

            // Attempt to grow by the larger of the sizeHint and double the current size.
            var growBy = Math.Max(sizeHint, currentLength);

            if (currentLength == 0)
            {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }

            var newSize = currentLength + growBy;

            if ((uint)newSize > int.MaxValue)
            {
                // Attempt to grow to Array.MaxLength.
                var needed = (uint)(currentLength - this.FreeCapacity + sizeHint);
                Debug.Assert(needed > currentLength);

                if (needed > Array.MaxLength)
                {
                    throw new OutOfMemoryException();
                }

                newSize = Array.MaxLength;
            }

            var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
            Array.Copy(this.buffer, newBuffer, this.buffer.Length);

            var oldBuffer = this.buffer;
            this.buffer = newBuffer;
            this.memory = this.buffer;

            if (oldBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(oldBuffer);
            }
        }

        Debug.Assert(this.FreeCapacity > 0 && this.FreeCapacity >= sizeHint);
    }
}