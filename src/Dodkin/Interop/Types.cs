namespace Dodkin.Interop;

using System.Reflection;
using System.Runtime.InteropServices;

enum HResult : uint
{
    S_OK            = 0x00000000,
    E_ABORT         = 0x80004004,
    E_ACCESSDENIED  = 0x80070005,
    E_FAIL          = 0x80004005,
    E_HANDLE        = 0x80070006,
    E_INVALIDARG    = 0x80070057,
    E_NOINTERFACE   = 0x80004002,
    E_NOTIMPL       = 0x80004001,
    E_OUTOFMEMORY   = 0x8007000E,
    E_POINTER       = 0x80004003,
    E_UNEXPECTED    = 0x8000FFFF,
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
    None        = 0,
    Null        = 1,
    Byte        = 17,
    UShort      = 18,
    UInt        = 19,
    ULong       = 21,
    AnsiString  = 30,
    String      = 31,
    Guid        = 72,
    ByteArray   = Byte | 0x1000,
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
