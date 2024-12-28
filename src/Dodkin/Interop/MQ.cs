namespace Dodkin.Interop;

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

[ComVisible(false), SuppressUnmanagedCodeSecurity]
static partial class MQ
{
    const string MQRT = "mqrt.dll";
    const string KERNEL32 = "kernel32.dll";

    public unsafe delegate void ReceiveCallback(
        HR hrStatus, IntPtr hSource, uint dwTimeout, uint dwAction, 
        IntPtr pMessageProps, NativeOverlapped* lpOverlapped, IntPtr hCursor);

    [DllImport(MQRT, EntryPoint = "MQOpenQueue", CharSet = CharSet.Unicode)]
    public static extern HR OpenQueue(string formatName, QueueAccessMode access, QueueShareMode shareMode, out QueueHandle handle);

    [DllImport(MQRT, EntryPoint = "MQBeginTransaction")]
    public static extern HR BeginTransaction(out ITransaction refTransaction);

    [DllImport(MQRT, EntryPoint = "MQCloseCursor")]
    public static extern HR CloseCursor(IntPtr cursorHandle);

    [DllImport(MQRT, EntryPoint = "MQCloseQueue")]
    public static extern HR CloseQueue(IntPtr handle);

    [DllImport(MQRT, EntryPoint = "MQDeleteQueue", CharSet = CharSet.Unicode)]
    public static extern HR DeleteQueue(string formatName);

    [DllImport(MQRT, EntryPoint = "MQCreateQueue", CharSet = CharSet.Unicode)]
    public static extern HR CreateQueue(IntPtr securityDescriptor, MQPROPS queueProperties, StringBuilder formatName, ref int formatNameLength);

    [DllImport(MQRT, EntryPoint = "MQHandleToFormatName", CharSet = CharSet.Unicode)]
    public static extern HR HandleToFormatName(QueueHandle hQueue, StringBuilder lpwcsFormatName, ref int lpdwCount);

    [DllImport(MQRT, EntryPoint = "MQGetQueueProperties", CharSet = CharSet.Unicode)]
    public static extern HR GetQueueProperties(string formatName, MQPROPS queueProperties);

    [DllImport(MQRT, EntryPoint = "MQSetQueueProperties", CharSet = CharSet.Unicode)]
    public static extern HR SetQueueProperties(string formatName, MQPROPS queueProperties);

    [DllImport(MQRT, EntryPoint = "MQGetOverlappedResult", CharSet = CharSet.Unicode)]
    public unsafe static extern HR GetOverlappedResult(NativeOverlapped* overlapped);

    [DllImport(MQRT, EntryPoint = "MQPathNameToFormatName", CharSet = CharSet.Unicode)]
    public static extern HR PathNameToFormatName(string lpwcsPathName, StringBuilder lpwcsFormatName, ref int lpdwCount);

    [DllImport(MQRT, EntryPoint = "MQCreateCursor")]
    public static extern HR CreateCursor(QueueHandle handle, out QueueCursorHandle cursorHandle);

    [DllImport(MQRT, EntryPoint = "MQMarkMessageRejected")]
    public static extern HR MarkMessageRejected(QueueHandle handle, ulong lookupId);

    [DllImport(MQRT, EntryPoint = "MQMoveMessage")]
    public static extern HR MoveMessage(QueueHandle sourceQueue, QueueHandle targetQueue, ulong lookupId, IntPtr transaction);

    [DllImport(MQRT, EntryPoint = "MQMoveMessage")]
    public static extern HR MoveMessage(QueueHandle sourceQueue, QueueHandle targetQueue, ulong lookupId, ITransaction transaction);

    [DllImport(MQRT, EntryPoint = "MQPurgeQueue")]
    public static extern HR PurgeQueue(QueueHandle sourceQueue);

    [DllImport(MQRT, EntryPoint = "MQReceiveMessage", CharSet = CharSet.Unicode)]
    public unsafe static extern HR ReceiveMessage(
        QueueHandle handle,
        uint timeout,
        ReceiveAction action,
        MQPROPS properties,
        NativeOverlapped* overlapped,
        ReceiveCallback? receiveCallback,
        QueueCursorHandle cursorHandle,
        IntPtr transaction);

    [DllImport(MQRT, EntryPoint = "MQReceiveMessage", CharSet = CharSet.Unicode)]
    public unsafe static extern HR ReceiveMessage(
        QueueHandle handle,
        uint timeout,
        ReceiveAction action,
        MQPROPS properties,
        NativeOverlapped* overlapped,
        ReceiveCallback? receiveCallback,
        QueueCursorHandle cursorHandle,
        ITransaction transaction);

    [DllImport(MQRT, EntryPoint = "MQReceiveMessageByLookupId", CharSet = CharSet.Unicode)]
    public unsafe static extern HR ReceiveMessageByLookupId(
        QueueHandle handle,
        ulong lookupId,
        LookupAction action,
        MQPROPS properties,
        NativeOverlapped* overlapped,
        ReceiveCallback? receiveCallback,
        IntPtr transaction);

    [DllImport(MQRT, EntryPoint = "MQReceiveMessageByLookupId", CharSet = CharSet.Unicode)]
    public unsafe static extern HR ReceiveMessageByLookupId(
        QueueHandle handle,
        ulong lookupId,
        LookupAction action,
        MQPROPS properties,
        NativeOverlapped* overlapped,
        ReceiveCallback? receiveCallback,
        ITransaction transaction);

    [DllImport(MQRT, EntryPoint = "MQSendMessage", CharSet = CharSet.Unicode)]
    public static extern HR SendMessage(QueueHandle handle, MQPROPS properties, IntPtr transaction);

    [DllImport(MQRT, EntryPoint = "MQSendMessage", CharSet = CharSet.Unicode)]
    public static extern HR SendMessage(QueueHandle handle, MQPROPS properties, ITransaction transaction);

    [DllImport(MQRT, EntryPoint = "MQMgmtGetInfo", CharSet = CharSet.Unicode)]
    public static extern HR MgmtGetInfo(string? pMachineName, string pObjectName, MQPROPS pMgmtProps);

    [DllImport(MQRT, EntryPoint = "MQFreeMemory", CharSet = CharSet.Unicode)]
    public static extern void FreeMemory(IntPtr memory);

    [DllImport(KERNEL32, SetLastError = true)]
    [ResourceExposure(ResourceScope.None)]
    public unsafe static extern int GetHandleInformation(QueueHandle handle, out int flags);

    public static uint GetTimeout(TimeSpan? timeout) => timeout switch
    {
        TimeSpan x when x >= TimeSpan.Zero && x != Timeout.InfiniteTimeSpan => (uint)x.TotalMilliseconds,
        _ => 0xFFFFFFFF,
    };

    private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
    private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
    private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

    [DllImport(KERNEL32, CharSet = CharSet.Unicode, BestFitMapping = true), ResourceExposure(ResourceScope.None)]
    internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId,
        [Out] StringBuilder lpBuffer, int nSize, IntPtr va_list_arguments);

    // Gets an error message for a Win32 error code.
    public static string GetErrorMessage(HR errorCode)
    {
        var sb = new StringBuilder(512);
        var result = FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
            FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
            IntPtr.Zero, (int)errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
        return result != 0 ? sb.ToString() : errorCode.ToString();
    }
}
