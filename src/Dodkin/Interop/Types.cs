namespace Dodkin.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Specifies how the message is read in the queue.
/// </summary>
enum ReceiveAction : uint
{
    /// <summary>
    /// Reads the message at the current cursor location and removes it from the queue.
    /// </summary>
    Receive = 0x00000000,

    /// <summary>
    /// Reads the message at the current cursor location but does not remove it from the queue.
    /// The cursor remains pointing at the current message.
    /// </summary>
    PeekCurrent = 0x80000000,

    /// <summary>
    /// Reads the next message in the queue (skipping the message at the current cursor location)
    /// but does not remove it from the queue.
    /// </summary>
    PeekNext = 0x80000001,
}

/// <summary>
/// Specifies how the message is read in the queue.
/// </summary>
enum LookupAction : uint
{
    /// <summary>
    /// Peeks at the message specified by <see cref="Message.LookupId"/>
    /// but does not remove it from the queue.
    /// </summary>
    PeekCurrent = 0x40000010,

    /// <summary>
    /// Peeks at the message following the message specified by <see cref="Message.LookupId"/>
    /// but does not remove it from the queue.
    /// </summary>
    PeekNext = 0x40000011,

    /// <summary>
    /// Peeks at the message preceding the message specified by <see cref="Message.LookupId"/>
    /// but does not remove it from the queue.
    /// </summary>
    PeekPrev = 0x40000012,

    /// <summary>
    /// Peeks at the first message in the queue but does not remove it from the queue.
    /// The <see cref="Message.LookupId"/> parameter must be set to 0.
    /// </summary>
    PeekFirst = 0x40000014,

    /// <summary>
    /// Peeks at the last message in the queue but does not remove it from the queue.
    /// The <see cref="Message.LookupId"/> parameter must be set to 0.
    /// </summary>
    PeekLast = 0x40000018,

    /// <summary>
    /// Retrieves the message specified by <see cref="Message.LookupId"/>
    /// and removes it from the queue.
    /// </summary>
    ReceiveCurrent = 0x40000020,

    /// <summary>
    /// Retrieves the message following the message specified by <see cref="Message.LookupId"/>
    /// and removes it from the queue.
    /// </summary>
    ReceiveNext = 0x40000021,

    /// <summary>
    /// Retrieves the message preceding the message specified by <see cref="Message.LookupId"/>
    /// and removes it from the queue.
    /// </summary>
    ReceivePrev = 0x40000022,

    /// <summary>
    /// Retrieves the first message in the queue and removes it from the queue.
    /// The <see cref="Message.LookupId"/> parameter must be set to 0.
    /// </summary>
    ReceiveFirst = 0x40000024,

    /// <summary>
    /// Retrieves the last message in the queue and removes it from the queue.
    /// The <see cref="Message.LookupId"/> parameter must be set to 0.
    /// </summary>
    ReceiveLast = 0x40000028,
}

[StructLayout(LayoutKind.Sequential)]
class MQPROPS
{
    public int cProp;
    public IntPtr aPropID;
    public IntPtr aPropVar;
    public IntPtr aStatus;

    public bool IsEmpty => this.cProp == 0 || this.aPropID == default || this.aPropVar == default;

    public void Clear()
    {
        this.cProp = 0;
        this.aPropID = default;
        this.aPropVar = default;
        this.aStatus = default;
    }
}

enum VarType : ushort
{
    None = 0,
    Null = 1,
    Byte = 17,
    UShort = 18,
    UInt = 19,
    ULong = 21,
    AnsiString = 30,
    String = 31,
    Guid = 72,
    ByteArray = Byte | 0x1000,
    StringArray = String | 0x1000,
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
struct MQPROPVARIANT
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CAUB
    {
        public uint cElems;
        public IntPtr pElems;
    }

    [FieldOffset(0)]
    public ushort vt;
    [FieldOffset(2)]
    public ushort wReserved1;
    [FieldOffset(4)]
    public ushort wReserved2;
    [FieldOffset(6)]
    public ushort wReserved3;
    [FieldOffset(8)]
    public byte bVal;
    [FieldOffset(8)]
    public short iVal;
    [FieldOffset(8)]
    public ushort uiVal;
    [FieldOffset(8)]
    public int lVal;
    [FieldOffset(8)]
    public uint ulVal;
    [FieldOffset(8)]
    public long hVal;
    [FieldOffset(8)]
    public ulong uhVal;
    [FieldOffset(8)]
    public IntPtr ptr;
    [FieldOffset(8)]
    public CAUB caub;
}