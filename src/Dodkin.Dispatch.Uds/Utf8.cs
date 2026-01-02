namespace Dodkin.Dispatch;

using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Unicode;

struct Utf8MemoryBuffer : IDisposable
{
    private const int KByte = 1024;
    private const int DefaultBufferSize = 32 * KByte;

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
        this.buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
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

    public readonly int WrittenCount => this.HasCapacity ? this.bytesWritten : this.memory.Length;

    public readonly ReadOnlyMemory<byte> WrittenMemory => this.memory[..this.bytesWritten];

    public readonly ReadOnlySpan<byte> WrittenSpan => this.memory.Span[..this.bytesWritten];

    private readonly bool CanAppend => this.bytesWritten < this.memory.Length;

    public readonly Memory<byte> Capacity => this.CanAppend ? this.memory[this.bytesWritten..] : Memory<byte>.Empty;

    public readonly bool HasCapacity => this.bytesWritten <= this.memory.Length;

    public void Clear()
    {
        this.bytesWritten = 0;
    }

    public bool Advance(int count)
    {
        this.bytesWritten += count;
        return this.CanAppend;
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

        var bufferWriter = new Utf8MemoryWriter(this.Capacity);
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        JsonSerializer.Serialize(jsonWriter, value, value.GetType());
        jsonWriter.Flush();

        return Advance(bufferWriter.BytesWritten);
    }
}

file sealed class Utf8MemoryWriter : IBufferWriter<byte>
{
    private readonly Memory<byte> memory;
    private int bytesWritten;

    public Utf8MemoryWriter(Memory<byte> memory)
    {
        this.memory = memory;
    }

    public bool HasCapacity => this.bytesWritten <= this.memory.Length;

    public int BytesWritten => this.HasCapacity ? this.bytesWritten : this.memory.Length;

    private bool CanAppend => this.bytesWritten < this.memory.Length;

    public void Advance(int count)
    {
        this.bytesWritten += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
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

    public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;
}