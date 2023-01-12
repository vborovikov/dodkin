namespace Dodkin.Interop;

using System;
using System.Reflection.PortableExecutable;
using Microsoft.Win32.SafeHandles;

class QueueHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public static readonly QueueHandle InvalidHandle = new InvalidMQHandle();

    public QueueHandle() : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
        var status = MQ.CloseQueue(this.handle);
        return !MQ.IsFatalError(status);
    }

    public override bool IsInvalid => base.IsInvalid || this.IsClosed;

    // Prevent exception when MQRT.DLL is not installed
    sealed class InvalidMQHandle : QueueHandle
    {
        protected override bool ReleaseHandle() => true;
    }
}

class QueueCursorHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public static readonly QueueCursorHandle None = new InvalidCursorHandle();

    protected QueueCursorHandle() : base(true)
    {
    }

    public static QueueCursorHandle Create(QueueHandle handle)
    {
        MessageQueueException.ThrowOnError(MQ.CreateCursor(handle, out var cursorHandle));
        return cursorHandle;
    }

    protected override bool ReleaseHandle()
    {
        MQ.CloseCursor(this.handle);
        return true;
    }

    public override bool IsInvalid => base.IsInvalid || this.IsClosed;

    // Prevent exception when MQRT.DLL is not installed
    sealed class InvalidCursorHandle : QueueCursorHandle
    {
        protected override bool ReleaseHandle() => true;
    }
}