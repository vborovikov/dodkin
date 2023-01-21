using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace Dodkin.Interop
{
    [ComVisible(false), SuppressUnmanagedCodeSecurity]
    static partial class MQ
    {
        const string MQRT = "mqrt.dll";

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

        [DllImport(MQRT, EntryPoint = "MQGetOverlappedResult", CharSet = CharSet.Unicode)]
        public unsafe static extern HR GetOverlappedResult(NativeOverlapped* overlapped);

        [DllImport(MQRT, EntryPoint = "MQPathNameToFormatName", CharSet = CharSet.Unicode)]
        public static extern HR PathNameToFormatName(string lpwcsPathName, StringBuilder lpwcsFormatName, ref int lpdwCount);

        [DllImport(MQRT, EntryPoint = "MQCreateCursor")]
        public static extern HR CreateCursor(QueueHandle handle, out QueueCursorHandle cursorHandle);

        [DllImport(MQRT, EntryPoint = "MQMarkMessageRejected")]
        public static extern HR MarkMessageRejected(QueueHandle handle, long lookupId);

        [DllImport(MQRT, EntryPoint = "MQMoveMessage")]
        public static extern HR MoveMessage(QueueHandle sourceQueue, QueueHandle targetQueue, long lookupId, IntPtr transaction);

        [DllImport(MQRT, EntryPoint = "MQMoveMessage")]
        public static extern HR MoveMessage(QueueHandle sourceQueue, QueueHandle targetQueue, long lookupId, ITransaction transaction); //MSMQ internal transaction

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
            ITransaction transaction); //MSMQ internal transaction

        [DllImport(MQRT, EntryPoint = "MQReceiveMessageByLookupId", CharSet = CharSet.Unicode)]
        public unsafe static extern HR ReceiveMessageByLookupId(
            QueueHandle handle,
            long lookupId,
            LookupAction action,
            MQPROPS properties,
            NativeOverlapped* overlapped,
            ReceiveCallback receiveCallback,
            IntPtr transaction);

        [DllImport(MQRT, EntryPoint = "MQReceiveMessageByLookupId", CharSet = CharSet.Unicode)]
        public unsafe static extern HR ReceiveMessageByLookupId(
            QueueHandle handle,
            long lookupId,
            LookupAction action,
            MQPROPS properties,
            NativeOverlapped* overlapped,
            ReceiveCallback receiveCallback,
            ITransaction transaction); //MSMQ internal transaction

        [DllImport(MQRT, EntryPoint = "MQSendMessage", CharSet = CharSet.Unicode)]
        public static extern HR SendMessage(QueueHandle handle, MQPROPS properties, IntPtr transaction);

        [DllImport(MQRT, EntryPoint = "MQSendMessage", CharSet = CharSet.Unicode)]
        public static extern HR SendMessage(QueueHandle handle, MQPROPS properties, ITransaction transaction); //MSMQ internal transaction

        [DllImport(MQRT, EntryPoint = "MQMgmtGetInfo", CharSet = CharSet.Unicode)]
        public static extern HR MgmtGetInfo(string? pMachineName, string pObjectName, MQPROPS pMgmtProps);

        [DllImport(MQRT, EntryPoint = "MQFreeMemory", CharSet = CharSet.Unicode)]
        public static extern void FreeMemory(IntPtr memory);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ResourceExposure(ResourceScope.None)]
        public unsafe static extern int GetHandleInformation(QueueHandle handle, out int flags);

        public static uint GetTimeout(TimeSpan? timeout) => (uint)(timeout ?? Timeout.InfiniteTimeSpan).TotalMilliseconds;

        public static bool IsBufferOverflow(HR hresult)
        {
            return hresult switch
            {
                HR.INFORMATION_FORMATNAME_BUFFER_TOO_SMALL or
                HR.ERROR_BUFFER_OVERFLOW or
                HR.ERROR_FORMATNAME_BUFFER_TOO_SMALL or
                HR.ERROR_SENDERID_BUFFER_TOO_SMALL or
                HR.ERROR_USER_BUFFER_TOO_SMALL or
                HR.ERROR_SENDER_CERT_BUFFER_TOO_SMALL or
                HR.ERROR_RESULT_BUFFER_TOO_SMALL or
                HR.ERROR_LABEL_BUFFER_TOO_SMALL or
                HR.ERROR_SYMM_KEY_BUFFER_TOO_SMALL or
                HR.ERROR_SIGNATURE_BUFFER_TOO_SMALL or
                HR.ERROR_PROV_NAME_BUFFER_TOO_SMALL => true,
                _ => false,
            };
        }

        public static bool IsStaleHandle(HR hresult)
        {
            return hresult switch
            {
                HR.ERROR_STALE_HANDLE or
                HR.ERROR_INVALID_HANDLE or
                HR.ERROR_INVALID_PARAMETER or
                HR.ERROR_QUEUE_DELETED => true,
                _ => false,
            };
        }

        public static bool IsFatalError(HR hresult)
        {
            if (hresult == HR.OK)
                return false;
            if (((uint)hresult & 0xC0000000) == 0x40000000)
                return false;

            return true;
        }
    }
}
