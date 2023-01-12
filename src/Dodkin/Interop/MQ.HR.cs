namespace Dodkin.Interop;

static partial class MQ
{
    public enum HR : uint
    {
        /////////////////////////////////////////////////////////////////////////
        //
        // Message Queuing Success values
        //
        //
        /////////////////////////////////////////////////////////////////////////

        OK = 0,

        /////////////////////////////////////////////////////////////////////////
        //
        // Message Queuing Information values
        //
        //
        /////////////////////////////////////////////////////////////////////////

        //
        //  Values are 32 bit values laid out as follows:
        //
        //   3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
        //   1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
        //  +---+-+-+-----------------------+-------------------------------+
        //  |Sev|C|R|     Facility          |               Code            |
        //  +---+-+-+-----------------------+-------------------------------+
        //
        //  where
        //
        //      Sev - is the severity code
        //
        //          00 - Success
        //          01 - Informational
        //          10 - Warning
        //          11 - Error
        //
        //      C - is the Customer code flag
        //
        //      R - is a reserved bit
        //
        //      Facility - is the facility code
        //
        //      Code - is the facility's status code
        //
        //
        // Define the facility codes
        //


        //
        // Define the severity codes
        //


        //
        // MessageId: MQ_INFORMATION_PROPERTY
        //
        // MessageText:
        //
        // One or more of the properties passed resulted in a warning, but the function completed.
        //
        INFORMATION_PROPERTY = 0x400E0001,

        //
        // MessageId: MQ_INFORMATION_ILLEGAL_PROPERTY
        //
        // MessageText:
        //
        // The property ID is invalid.
        //
        INFORMATION_ILLEGAL_PROPERTY = 0x400E0002,

        //
        // MessageId: MQ_INFORMATION_PROPERTY_IGNORED
        //
        // MessageText:
        //
        // The property specified was ignored for this operation (this occurs,
        // for example, when PROPID_M_SENDERID is passed to SendMessage()).
        //
        INFORMATION_PROPERTY_IGNORED = 0x400E0003,

        //
        // MessageId: MQ_INFORMATION_UNSUPPORTED_PROPERTY
        //
        // MessageText:
        //
        // The property specified is not supported and was ignored for this operation.
        //
        INFORMATION_UNSUPPORTED_PROPERTY = 0x400E0004,

        //
        // MessageId: MQ_INFORMATION_DUPLICATE_PROPERTY
        //
        // MessageText:
        //
        // The property specified is already in the property identifier array.
        // The duplicate was ignored for this operation.
        //
        INFORMATION_DUPLICATE_PROPERTY = 0x400E0005,

        //
        // MessageId: MQ_INFORMATION_OPERATION_PENDING
        //
        // MessageText:
        //
        // An asynchronous operation is currently pending.
        //
        INFORMATION_OPERATION_PENDING = 0x400E0006,

        //
        // MessageId: MQ_INFORMATION_FORMATNAME_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The format name buffer supplied to MQCreateQueue was too small
        // to hold the format name, however the queue was created successfully.
        //
        INFORMATION_FORMATNAME_BUFFER_TOO_SMALL = 0x400E0009,

        //
        // MessageId: MQ_INFORMATION_INTERNAL_USER_CERT_EXIST
        //
        // MessageText:
        //
        // An internal Message Queuing certificate already exists for this user.
        //
        INFORMATION_INTERNAL_USER_CERT_EXIST = 0x400E000A,

        //
        // MessageId: MQ_INFORMATION_OWNER_IGNORED
        //
        // MessageText:
        //
        // The queue owner was not set during the processing of this call to MQSetQueueSecurity().
        //
        INFORMATION_OWNER_IGNORED = 0x400E000B,

        /////////////////////////////////////////////////////////////////////////
        //
        //  Message Queuing Error values
        //
        //
        /////////////////////////////////////////////////////////////////////////

        //
        // MessageId: MQ_ERROR
        //
        // MessageText:
        //
        // Generic error code.
        //
        ERROR = 0xC00E0001,

        //
        // MessageId: MQ_ERROR_PROPERTY
        //
        // MessageText:
        //
        // One or more of the properties passed are invalid.
        //
        ERROR_PROPERTY = 0xC00E0002,

        //
        // MessageId: MQ_ERROR_QUEUE_NOT_FOUND
        //
        // MessageText:
        //
        // The queue does not exist or you do not have sufficient permissions to perform the operation.
        //
        ERROR_QUEUE_NOT_FOUND = 0xC00E0003,

        //
        // MessageId: MQ_ERROR_QUEUE_NOT_ACTIVE
        //
        // MessageText:
        //
        // The queue is not open or may not exist.
        //
        ERROR_QUEUE_NOT_ACTIVE = 0xC00E0004,

        //
        // MessageId: MQ_ERROR_QUEUE_EXISTS
        //
        // MessageText:
        //
        // A queue with the same path name already exists.
        //
        ERROR_QUEUE_EXISTS = 0xC00E0005,

        //
        // MessageId: MQ_ERROR_INVALID_PARAMETER
        //
        // MessageText:
        //
        // An invalid parameter was passed to a function.
        //
        ERROR_INVALID_PARAMETER = 0xC00E0006,

        //
        // MessageId: MQ_ERROR_INVALID_HANDLE
        //
        // MessageText:
        //
        // An invalid handle was passed to a function.
        //
        ERROR_INVALID_HANDLE = 0xC00E0007,

        //
        // MessageId: MQ_ERROR_OPERATION_CANCELLED
        //
        // MessageText:
        //
        // The operation was canceled before it could be completed.
        //
        ERROR_OPERATION_CANCELLED = 0xC00E0008,

        //
        // MessageId: MQ_ERROR_SHARING_VIOLATION
        //
        // MessageText:
        //
        // There is a sharing violation. The queue is already open for exclusive retrieval.
        //
        ERROR_SHARING_VIOLATION = 0xC00E0009,

        //
        // MessageId: MQ_ERROR_SERVICE_NOT_AVAILABLE
        //
        // MessageText:
        //
        // The Message Queuing service is not available
        //
        ERROR_SERVICE_NOT_AVAILABLE = 0xC00E000B,

        //
        // MessageId: MQ_ERROR_MACHINE_NOT_FOUND
        //
        // MessageText:
        //
        // The computer specified cannot be found.
        //
        ERROR_MACHINE_NOT_FOUND = 0xC00E000D,

        //
        // MessageId: MQ_ERROR_ILLEGAL_SORT
        //
        // MessageText:
        //
        // The sort operation specified in MQLocateBegin is invalid (for example, there are duplicate columns).
        //
        ERROR_ILLEGAL_SORT = 0xC00E0010,

        //
        // MessageId: MQ_ERROR_ILLEGAL_USER
        //
        // MessageText:
        //
        // The user specified is not a valid user.
        //
        ERROR_ILLEGAL_USER = 0xC00E0011,

        //
        // MessageId: MQ_ERROR_NO_DS
        //
        // MessageText:
        //
        // A connection with Active Directory Domain Services cannot be established. Verify that there are sufficient permissions to perform this operation.
        //
        ERROR_NO_DS = 0xC00E0013,

        //
        // MessageId: MQ_ERROR_ILLEGAL_QUEUE_PATHNAME
        //
        // MessageText:
        //
        // The queue path name specified is invalid.
        //
        ERROR_ILLEGAL_QUEUE_PATHNAME = 0xC00E0014,

        //
        // MessageId: MQ_ERROR_ILLEGAL_PROPERTY_VALUE
        //
        // MessageText:
        //
        // The property value specified is invalid.
        //
        ERROR_ILLEGAL_PROPERTY_VALUE = 0xC00E0018,

        //
        // MessageId: MQ_ERROR_ILLEGAL_PROPERTY_VT
        //
        // MessageText:
        //
        // The VARTYPE value specified is invalid.
        //
        ERROR_ILLEGAL_PROPERTY_VT = 0xC00E0019,

        //
        // MessageId: MQ_ERROR_BUFFER_OVERFLOW
        //
        // MessageText:
        //
        // The buffer supplied to MQReceiveMessage for message property retrieval
        // is too small. The message was not removed from the queue, but the part
        // of the message property that was in the buffer was copied.
        //
        ERROR_BUFFER_OVERFLOW = 0xC00E001A,

        //
        // MessageId: MQ_ERROR_IO_TIMEOUT
        //
        // MessageText:
        //
        // The time specified for MQReceiveMessage to wait for the message elapsed.
        //
        ERROR_IO_TIMEOUT = 0xC00E001B,

        //
        // MessageId: MQ_ERROR_ILLEGAL_CURSOR_ACTION
        //
        // MessageText:
        //
        // The MQ_ACTION_PEEK_NEXT value specified for MQReceiveMessage cannot be used with
        // the current cursor position.
        //
        ERROR_ILLEGAL_CURSOR_ACTION = 0xC00E001C,

        //
        // MessageId: MQ_ERROR_MESSAGE_ALREADY_RECEIVED
        //
        // MessageText:
        //
        // The message at which the cursor is currently pointing was removed from
        // the queue by another process or by another call to MQReceiveMessage
        // without the use of this cursor.
        //
        ERROR_MESSAGE_ALREADY_RECEIVED = 0xC00E001D,

        //
        // MessageId: MQ_ERROR_ILLEGAL_FORMATNAME
        //
        // MessageText:
        //
        // The format name specified is invalid.
        //
        ERROR_ILLEGAL_FORMATNAME = 0xC00E001E,

        //
        // MessageId: MQ_ERROR_FORMATNAME_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The format name buffer supplied to the API was too small
        // to hold the format name.
        //
        ERROR_FORMATNAME_BUFFER_TOO_SMALL = 0xC00E001F,

        //
        // MessageId: MQ_ERROR_UNSUPPORTED_FORMATNAME_OPERATION
        //
        // MessageText:
        //
        // Operations of the type requested (for example, deleting a queue using a direct format name)
        // are not supported for the format name specified.
        //
        ERROR_UNSUPPORTED_FORMATNAME_OPERATION = 0xC00E0020,

        //
        // MessageId: MQ_ERROR_ILLEGAL_SECURITY_DESCRIPTOR
        //
        // MessageText:
        //
        // The specified security descriptor is invalid.
        //
        ERROR_ILLEGAL_SECURITY_DESCRIPTOR = 0xC00E0021,

        //
        // MessageId: MQ_ERROR_SENDERID_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The size of the buffer for the user ID property is too small.
        //
        ERROR_SENDERID_BUFFER_TOO_SMALL = 0xC00E0022,

        //
        // MessageId: MQ_ERROR_SECURITY_DESCRIPTOR_TOO_SMALL
        //
        // MessageText:
        //
        // The size of the buffer passed to MQGetQueueSecurity is too small.
        //
        ERROR_SECURITY_DESCRIPTOR_TOO_SMALL = 0xC00E0023,

        //
        // MessageId: MQ_ERROR_CANNOT_IMPERSONATE_CLIENT
        //
        // MessageText:
        //
        // The security credentials cannot be verified because the RPC server
        // cannot impersonate the client application.
        //
        ERROR_CANNOT_IMPERSONATE_CLIENT = 0xC00E0024,

        //
        // MessageId: MQ_ERROR_ACCESS_DENIED
        //
        // MessageText:
        //
        // Access is denied.
        //
        ERROR_ACCESS_DENIED = 0xC00E0025,

        //
        // MessageId: MQ_ERROR_PRIVILEGE_NOT_HELD
        //
        // MessageText:
        //
        // The client does not have sufficient security privileges to perform the operation.
        //
        ERROR_PRIVILEGE_NOT_HELD = 0xC00E0026,

        //
        // MessageId: MQ_ERROR_INSUFFICIENT_RESOURCES
        //
        // MessageText:
        //
        // There are insufficient resources to perform this operation.
        //
        ERROR_INSUFFICIENT_RESOURCES = 0xC00E0027,

        //
        // MessageId: MQ_ERROR_USER_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The request failed because the user buffer is too small to hold the information returned.
        //
        ERROR_USER_BUFFER_TOO_SMALL = 0xC00E0028,

        //
        // MessageId: MQ_ERROR_MESSAGE_STORAGE_FAILED
        //
        // MessageText:
        //
        // A recoverable or journal message could not be stored. The message was not sent.
        //
        ERROR_MESSAGE_STORAGE_FAILED = 0xC00E002A,

        //
        // MessageId: MQ_ERROR_SENDER_CERT_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The buffer for the user certificate property is too small.
        //
        ERROR_SENDER_CERT_BUFFER_TOO_SMALL = 0xC00E002B,

        //
        // MessageId: MQ_ERROR_INVALID_CERTIFICATE
        //
        // MessageText:
        //
        // The user certificate is invalid.
        //
        ERROR_INVALID_CERTIFICATE = 0xC00E002C,

        //
        // MessageId: MQ_ERROR_CORRUPTED_INTERNAL_CERTIFICATE
        //
        // MessageText:
        //
        // The internal Message Queuing certificate is corrupted.
        //
        ERROR_CORRUPTED_INTERNAL_CERTIFICATE = 0xC00E002D,

        //
        // MessageId: MQ_ERROR_INTERNAL_USER_CERT_EXIST
        //
        // MessageText:
        //
        // An internal Message Queuing certificate already exists for this user.
        //
        ERROR_INTERNAL_USER_CERT_EXIST = 0xC00E002E,

        //
        // MessageId: MQ_ERROR_NO_INTERNAL_USER_CERT
        //
        // MessageText:
        //
        // No internal Message Queuing certificate exists for the user.
        //
        ERROR_NO_INTERNAL_USER_CERT = 0xC00E002F,

        //
        // MessageId: MQ_ERROR_CORRUPTED_SECURITY_DATA
        //
        // MessageText:
        //
        // A cryptographic function failed.
        //
        ERROR_CORRUPTED_SECURITY_DATA = 0xC00E0030,

        //
        // MessageId: MQ_ERROR_CORRUPTED_PERSONAL_CERT_STORE
        //
        // MessageText:
        //
        // The personal certificate store is corrupted.
        //
        ERROR_CORRUPTED_PERSONAL_CERT_STORE = 0xC00E0031,

        //
        // MessageId: MQ_ERROR_COMPUTER_DOES_NOT_SUPPORT_ENCRYPTION
        //
        // MessageText:
        //
        // The computer does not support encryption operations.
        //
        ERROR_COMPUTER_DOES_NOT_SUPPORT_ENCRYPTION = 0xC00E0033,

        //
        // MessageId: MQ_ERROR_BAD_SECURITY_CONTEXT
        //
        // MessageText:
        //
        // The security context is invalid.
        //
        ERROR_BAD_SECURITY_CONTEXT = 0xC00E0035,

        //
        // MessageId: MQ_ERROR_COULD_NOT_GET_USER_SID
        //
        // MessageText:
        //
        // The SID cannot be obtained from the thread token.
        //
        ERROR_COULD_NOT_GET_USER_SID = 0xC00E0036,

        //
        // MessageId: MQ_ERROR_COULD_NOT_GET_ACCOUNT_INFO
        //
        // MessageText:
        //
        // The account information for the user cannot be obtained.
        //
        ERROR_COULD_NOT_GET_ACCOUNT_INFO = 0xC00E0037,

        //
        // MessageId: MQ_ERROR_ILLEGAL_MQCOLUMNS
        //
        // MessageText:
        //
        // The MQCOLUMNS parameter is invalid.
        //
        ERROR_ILLEGAL_MQCOLUMNS = 0xC00E0038,

        //
        // MessageId: MQ_ERROR_ILLEGAL_PROPID
        //
        // MessageText:
        //
        // A property identifier is invalid.
        //
        ERROR_ILLEGAL_PROPID = 0xC00E0039,

        //
        // MessageId: MQ_ERROR_ILLEGAL_RELATION
        //
        // MessageText:
        //
        // A relationship parameter is invalid.
        //
        ERROR_ILLEGAL_RELATION = 0xC00E003A,

        //
        // MessageId: MQ_ERROR_ILLEGAL_PROPERTY_SIZE
        //
        // MessageText:
        //
        // The size of the buffer for the message identifier or correlation identifier is invalid.
        //
        ERROR_ILLEGAL_PROPERTY_SIZE = 0xC00E003B,

        //
        // MessageId: MQ_ERROR_ILLEGAL_RESTRICTION_PROPID
        //
        // MessageText:
        //
        // A property identifier specified in MQRESTRICTION is invalid.
        //
        ERROR_ILLEGAL_RESTRICTION_PROPID = 0xC00E003C,

        //
        // MessageId: MQ_ERROR_ILLEGAL_MQQUEUEPROPS
        //
        // MessageText:
        //
        // Either the pointer to the MQQUEUEPROPS structure has a null value, or no properties are specified in it.
        //
        ERROR_ILLEGAL_MQQUEUEPROPS = 0xC00E003D,

        //
        // MessageId: MQ_ERROR_PROPERTY_NOTALLOWED
        //
        // MessageText:
        //
        // The property identifier specified (for example, PROPID_Q_INSTANCE in MQSetQueueProperties)
        // is invalid for the operation requested.
        //
        ERROR_PROPERTY_NOTALLOWED = 0xC00E003E,

        //
        // MessageId: MQ_ERROR_INSUFFICIENT_PROPERTIES
        //
        // MessageText:
        //
        // Not all the properties required for the operation were specified
        // for the input parameters.
        //
        ERROR_INSUFFICIENT_PROPERTIES = 0xC00E003F,

        //
        // MessageId: MQ_ERROR_MACHINE_EXISTS
        //
        // MessageText:
        //
        // The MSMQ Configuration (msmq) object already exists in Active Directory Domain Services.
        //
        ERROR_MACHINE_EXISTS = 0xC00E0040,

        //
        // MessageId: MQ_ERROR_ILLEGAL_MQQMPROPS
        //
        // MessageText:
        //
        // Either the pointer to the MQQMROPS structure has a null value, or no properties are specified in it.
        //
        ERROR_ILLEGAL_MQQMPROPS = 0xC00E0041,

        //
        // MessageId: MQ_ERROR_DS_IS_FULL
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_DS_IS_FULL = 0xC00E0042,

        //
        // MessageId: MQ_ERROR_DS_ERROR
        //
        // MessageText:
        //
        // There is an internal Active Directory Domain Services error.
        //
        ERROR_DS_ERROR = 0xC00E0043,

        //
        // MessageId: MQ_ERROR_INVALID_OWNER
        //
        // MessageText:
        //
        // The object owner is invalid (for example, MQCreateQueue failed because the QM
        // object is invalid).
        //
        ERROR_INVALID_OWNER = 0xC00E0044,

        //
        // MessageId: MQ_ERROR_UNSUPPORTED_ACCESS_MODE
        //
        // MessageText:
        //
        // The access mode specified is unsupported.
        //
        ERROR_UNSUPPORTED_ACCESS_MODE = 0xC00E0045,

        //
        // MessageId: MQ_ERROR_RESULT_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The result buffer specified is too small.
        //
        ERROR_RESULT_BUFFER_TOO_SMALL = 0xC00E0046,

        //
        // MessageId: MQ_ERROR_DELETE_CN_IN_USE
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_DELETE_CN_IN_USE = 0xC00E0048,

        //
        // MessageId: MQ_ERROR_NO_RESPONSE_FROM_OBJECT_SERVER
        //
        // MessageText:
        //
        // There was no response from the object owner.
        //
        ERROR_NO_RESPONSE_FROM_OBJECT_SERVER = 0xC00E0049,

        //
        // MessageId: MQ_ERROR_OBJECT_SERVER_NOT_AVAILABLE
        //
        // MessageText:
        //
        // The object owner is not available.
        //
        ERROR_OBJECT_SERVER_NOT_AVAILABLE = 0xC00E004A,

        //
        // MessageId: MQ_ERROR_QUEUE_NOT_AVAILABLE
        //
        // MessageText:
        //
        // An error occurred while reading from a queue located on a remote computer.
        //
        ERROR_QUEUE_NOT_AVAILABLE = 0xC00E004B,

        //
        // MessageId: MQ_ERROR_DTC_CONNECT
        //
        // MessageText:
        //
        // A connection cannot be established with the Distributed Transaction Coordinator.
        //
        ERROR_DTC_CONNECT = 0xC00E004C,

        //
        // MessageId: MQ_ERROR_TRANSACTION_IMPORT
        //
        // MessageText:
        //
        // The transaction specified cannot be imported.
        //
        ERROR_TRANSACTION_IMPORT = 0xC00E004E,

        //
        // MessageId: MQ_ERROR_TRANSACTION_USAGE
        //
        // MessageText:
        //
        // An attempted action cannot be performed within a transaction.
        //
        ERROR_TRANSACTION_USAGE = 0xC00E0050,

        //
        // MessageId: MQ_ERROR_TRANSACTION_SEQUENCE
        //
        // MessageText:
        //
        // The transaction's operation sequence is incorrect.
        //
        ERROR_TRANSACTION_SEQUENCE = 0xC00E0051,

        //
        // MessageId: MQ_ERROR_MISSING_CONNECTOR_TYPE
        //
        // MessageText:
        //
        // The connector type message property is not specified. This property is required for sending an acknowledgment message or a secure message.
        //
        ERROR_MISSING_CONNECTOR_TYPE = 0xC00E0055,

        //
        // MessageId: MQ_ERROR_STALE_HANDLE
        //
        // MessageText:
        //
        // The Message Queuing service was restarted. Any open queue handles should be closed.
        //
        ERROR_STALE_HANDLE = 0xC00E0056,

        //
        // MessageId: MQ_ERROR_TRANSACTION_ENLIST
        //
        // MessageText:
        //
        // The transaction specified cannot be enlisted.
        //
        ERROR_TRANSACTION_ENLIST = 0xC00E0058,

        //
        // MessageId: MQ_ERROR_QUEUE_DELETED
        //
        // MessageText:
        //
        // The queue was deleted. Messages cannot be received anymore using this
        // queue handle. The handle should be closed.
        //
        ERROR_QUEUE_DELETED = 0xC00E005A,

        //
        // MessageId: MQ_ERROR_ILLEGAL_CONTEXT
        //
        // MessageText:
        //
        // The context parameter for MQLocateBegin is invalid.
        //
        ERROR_ILLEGAL_CONTEXT = 0xC00E005B,

        //
        // MessageId: MQ_ERROR_ILLEGAL_SORT_PROPID
        //
        // MessageText:
        //
        // An invalid property identifier is specified in MQSORTSET.
        //
        ERROR_ILLEGAL_SORT_PROPID = 0xC00E005C,

        //
        // MessageId: MQ_ERROR_LABEL_TOO_LONG
        //
        // MessageText:
        //
        // The message label is too long. Its length should be less than or equal to MQ_MAX_MSG_LABEL_LEN.
        //
        ERROR_LABEL_TOO_LONG = 0xC00E005D,

        //
        // MessageId: MQ_ERROR_LABEL_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The label buffer supplied to the API is too small.
        //
        ERROR_LABEL_BUFFER_TOO_SMALL = 0xC00E005E,

        //
        // MessageId: MQ_ERROR_MQIS_SERVER_EMPTY
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_MQIS_SERVER_EMPTY = 0xC00E005F,

        //
        // MessageId: MQ_ERROR_MQIS_READONLY_MODE
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_MQIS_READONLY_MODE = 0xC00E0060,

        //
        // MessageId: MQ_ERROR_SYMM_KEY_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The buffer passed for the symmetric key is too small.
        //
        ERROR_SYMM_KEY_BUFFER_TOO_SMALL = 0xC00E0061,

        //
        // MessageId: MQ_ERROR_SIGNATURE_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The buffer passed for the signature property is too small.
        //
        ERROR_SIGNATURE_BUFFER_TOO_SMALL = 0xC00E0062,

        //
        // MessageId: MQ_ERROR_PROV_NAME_BUFFER_TOO_SMALL
        //
        // MessageText:
        //
        // The buffer passed for the provider name property is too small.
        //
        ERROR_PROV_NAME_BUFFER_TOO_SMALL = 0xC00E0063,

        //
        // MessageId: MQ_ERROR_ILLEGAL_OPERATION
        //
        // MessageText:
        //
        // The operation is invalid for a foreign message queuing system.
        //
        ERROR_ILLEGAL_OPERATION = 0xC00E0064,

        //
        // MessageId: MQ_ERROR_WRITE_NOT_ALLOWED
        //
        // MessageText:
        //
        // Obsolete; another MQIS server is being installed. Write operations to the database are not allowed at this stage.
        //
        ERROR_WRITE_NOT_ALLOWED = 0xC00E0065,

        //
        // MessageId: MQ_ERROR_WKS_CANT_SERVE_CLIENT
        //
        // MessageText:
        //
        // Independent clients cannot support dependent clients. A Message Queuing server is required.
        //
        ERROR_WKS_CANT_SERVE_CLIENT = 0xC00E0066,

        //
        // MessageId: MQ_ERROR_DEPEND_WKS_LICENSE_OVERFLOW
        //
        // MessageText:
        //
        // The number of dependent clients served by the Message Queuing server reached its upper limit.
        //
        ERROR_DEPEND_WKS_LICENSE_OVERFLOW = 0xC00E0067,

        //
        // MessageId: MQ_CORRUPTED_QUEUE_WAS_DELETED
        //
        // MessageText:
        //
        // The file %1 for the queue %2 in the Lqs folder was deleted because it was corrupted.
        //
        CORRUPTED_QUEUE_WAS_DELETED = 0xC00E0068,

        //
        // MessageId: MQ_ERROR_REMOTE_MACHINE_NOT_AVAILABLE
        //
        // MessageText:
        //
        // The remote computer is not available.
        //
        ERROR_REMOTE_MACHINE_NOT_AVAILABLE = 0xC00E0069,

        //
        // MessageId: MQ_ERROR_UNSUPPORTED_OPERATION
        //
        // MessageText:
        //
        // This operation is not supported for Message Queuing installed in workgroup mode.
        //
        ERROR_UNSUPPORTED_OPERATION = 0xC00E006A,

        //
        // MessageId: MQ_ERROR_ENCRYPTION_PROVIDER_NOT_SUPPORTED
        //
        // MessageText:
        //
        // The cryptographic service provider %1 is not supported by Message Queuing.
        //
        ERROR_ENCRYPTION_PROVIDER_NOT_SUPPORTED = 0xC00E006B,

        //
        // MessageId: MQ_ERROR_CANNOT_SET_CRYPTO_SEC_DESCR
        //
        // MessageText:
        //
        // The security descriptors for the cryptographic keys cannot be set.
        //
        ERROR_CANNOT_SET_CRYPTO_SEC_DESCR = 0xC00E006C,

        //
        // MessageId: MQ_ERROR_CERTIFICATE_NOT_PROVIDED
        //
        // MessageText:
        //
        // A user attempted to send an authenticated message without a certificate.
        //
        ERROR_CERTIFICATE_NOT_PROVIDED = 0xC00E006D,

        //
        // MessageId: MQ_ERROR_Q_DNS_PROPERTY_NOT_SUPPORTED
        //
        // MessageText:
        //
        // The column PROPID_Q_PATHNAME_DNS is not supported for the MQLocateBegin API.
        //
        ERROR_Q_DNS_PROPERTY_NOT_SUPPORTED = 0xC00E006E,

        //
        // MessageId: MQ_ERROR_CANNOT_CREATE_CERT_STORE
        //
        // MessageText:
        //
        // A certificate store cannot be created for the internal certificate.
        //
        ERROR_CANNOT_CREATE_CERT_STORE = 0xC00E006F,

        //
        // MessageId: MQ_ERROR_CANNOT_OPEN_CERT_STORE
        //
        // MessageText:
        //
        // The certificate store for the internal certificate cannot be opened.
        //
        ERROR_CANNOT_OPEN_CERT_STORE = 0xC00E0070,

        //
        // MessageId: MQ_ERROR_ILLEGAL_ENTERPRISE_OPERATION
        //
        // MessageText:
        //
        // This operation is invalid for an MsmqServices object.
        //
        ERROR_ILLEGAL_ENTERPRISE_OPERATION = 0xC00E0071,

        //
        // MessageId: MQ_ERROR_CANNOT_GRANT_ADD_GUID
        //
        // MessageText:
        //
        // The Add GUID permission cannot be granted to the current user.
        //
        ERROR_CANNOT_GRANT_ADD_GUID = 0xC00E0072,

        //
        // MessageId: MQ_ERROR_CANNOT_LOAD_MSMQOCM
        //
        // MessageText:
        //
        // Obsolete: The dynamic-link library Msmqocm.dll cannot be loaded.
        //
        ERROR_CANNOT_LOAD_MSMQOCM = 0xC00E0073,

        //
        // MessageId: MQ_ERROR_NO_ENTRY_POINT_MSMQOCM
        //
        // MessageText:
        //
        // An entry point cannot be located in Msmqocm.dll.
        //
        ERROR_NO_ENTRY_POINT_MSMQOCM = 0xC00E0074,

        //
        // MessageId: MQ_ERROR_NO_MSMQ_SERVERS_ON_DC
        //
        // MessageText:
        //
        // Message Queuing servers cannot be found on domain controllers.
        //
        ERROR_NO_MSMQ_SERVERS_ON_DC = 0xC00E0075,

        //
        // MessageId: MQ_ERROR_CANNOT_JOIN_DOMAIN
        //
        // MessageText:
        //
        // The computer joined the domain, but Message Queuing will continue to run in workgroup mode because it failed to register itself in Active Directory Domain Services.
        //
        ERROR_CANNOT_JOIN_DOMAIN = 0xC00E0076,

        //
        // MessageId: MQ_ERROR_CANNOT_CREATE_ON_GC
        //
        // MessageText:
        //
        // The object was not created on the Global Catalog server specified.
        //
        ERROR_CANNOT_CREATE_ON_GC = 0xC00E0077,

        //
        // MessageId: MQ_ERROR_GUID_NOT_MATCHING
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_GUID_NOT_MATCHING = 0xC00E0078,

        //
        // MessageId: MQ_ERROR_PUBLIC_KEY_NOT_FOUND
        //
        // MessageText:
        //
        // The public key for the computer %1 cannot be found.
        //
        ERROR_PUBLIC_KEY_NOT_FOUND = 0xC00E0079,

        //
        // MessageId: MQ_ERROR_PUBLIC_KEY_DOES_NOT_EXIST
        //
        // MessageText:
        //
        // The public key for the computer %1 does not exist.
        //
        ERROR_PUBLIC_KEY_DOES_NOT_EXIST = 0xC00E007A,

        //
        // MessageId: MQ_ERROR_ILLEGAL_MQPRIVATEPROPS
        //
        // MessageText:
        //
        // The parameters in MQPRIVATEPROPS are invalid. Either the pointer to the MQPRIVATEPROPS structure has a null value, or no properties are specified in it.
        //
        ERROR_ILLEGAL_MQPRIVATEPROPS = 0xC00E007B,

        //
        // MessageId: MQ_ERROR_NO_GC_IN_DOMAIN
        //
        // MessageText:
        //
        // Global Catalog servers cannot be found in the domain specified.
        //
        ERROR_NO_GC_IN_DOMAIN = 0xC00E007C,

        //
        // MessageId: MQ_ERROR_NO_MSMQ_SERVERS_ON_GC
        //
        // MessageText:
        //
        // No Message Queuing servers were found on Global Catalog servers.
        //
        ERROR_NO_MSMQ_SERVERS_ON_GC = 0xC00E007D,

        //
        // MessageId: MQ_ERROR_CANNOT_GET_DN
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_CANNOT_GET_DN = 0xC00E007E,

        //
        // MessageId: MQ_ERROR_CANNOT_HASH_DATA_EX
        //
        // MessageText:
        //
        // Data for an authenticated message cannot be hashed.
        //
        ERROR_CANNOT_HASH_DATA_EX = 0xC00E007F,

        //
        // MessageId: MQ_ERROR_CANNOT_SIGN_DATA_EX
        //
        // MessageText:
        //
        // Data cannot be signed before sending an authenticated message.
        //
        ERROR_CANNOT_SIGN_DATA_EX = 0xC00E0080,

        //
        // MessageId: MQ_ERROR_CANNOT_CREATE_HASH_EX
        //
        // MessageText:
        //
        // A hash object cannot be created for an authenticated message.
        //
        ERROR_CANNOT_CREATE_HASH_EX = 0xC00E0081,

        //
        // MessageId: MQ_ERROR_FAIL_VERIFY_SIGNATURE_EX
        //
        // MessageText:
        //
        // The signature of the message received is not valid.
        //
        ERROR_FAIL_VERIFY_SIGNATURE_EX = 0xC00E0082,

        //
        // MessageId: MQ_ERROR_CANNOT_DELETE_PSC_OBJECTS
        //
        // MessageText:
        //
        // The object that will be deleted is owned by a primary site controller. The operation cannot be performed.
        //
        ERROR_CANNOT_DELETE_PSC_OBJECTS = 0xC00E0083,

        //
        // MessageId: MQ_ERROR_NO_MQUSER_OU
        //
        // MessageText:
        //
        // There is no MSMQ Users organizational unit object in Active Directory Domain Services for the domain. Please create one manually.
        //
        ERROR_NO_MQUSER_OU = 0xC00E0084,

        //
        // MessageId: MQ_ERROR_CANNOT_LOAD_MQAD
        //
        // MessageText:
        //
        // The dynamic-link library Mqad.dll cannot be loaded.
        //
        ERROR_CANNOT_LOAD_MQAD = 0xC00E0085,

        //
        // MessageId: MQ_ERROR_CANNOT_LOAD_MQDSSRV
        //
        // MessageText:
        //
        // Obsolete, kept for backward compatibility
        //
        ERROR_CANNOT_LOAD_MQDSSRV = 0xC00E0086,

        //
        // MessageId: MQ_ERROR_PROPERTIES_CONFLICT
        //
        // MessageText:
        //
        // Two or more of the properties passed cannot co-exist.
        // For example, you cannot set both PROPID_M_RESP_QUEUE and PROPID_M_RESP_FORMAT_NAME when sending a message.
        //
        ERROR_PROPERTIES_CONFLICT = 0xC00E0087,

        //
        // MessageId: MQ_ERROR_MESSAGE_NOT_FOUND
        //
        // MessageText:
        //
        // The message does not exist or was removed from the queue.
        //
        ERROR_MESSAGE_NOT_FOUND = 0xC00E0088,

        //
        // MessageId: MQ_ERROR_CANT_RESOLVE_SITES
        //
        // MessageText:
        //
        // The sites where the computer resides cannot be resolved. Check that the subnets in your network are configured correctly in Active Directory Domain Services and that each site is configured with the appropriate subnet.
        //
        ERROR_CANT_RESOLVE_SITES = 0xC00E0089,

        //
        // MessageId: MQ_ERROR_NOT_SUPPORTED_BY_DEPENDENT_CLIENTS
        //
        // MessageText:
        //
        // This operation is not supported by dependent clients.
        //
        ERROR_NOT_SUPPORTED_BY_DEPENDENT_CLIENTS = 0xC00E008A,

        //
        // MessageId: MQ_ERROR_OPERATION_NOT_SUPPORTED_BY_REMOTE_COMPUTER
        //
        // MessageText:
        //
        // This operation is not supported by the remote Message Queuing service. For example, MQReceiveMessageByLookupId is not supported by MSMQ 1.0/2.0.
        //
        ERROR_OPERATION_NOT_SUPPORTED_BY_REMOTE_COMPUTER = 0xC00E008B,

        //
        // MessageId: MQ_ERROR_NOT_A_CORRECT_OBJECT_CLASS
        //
        // MessageText:
        //
        // The object whose properties are being retrieved from Active Directory Domain Services does not belong to the class requested.
        //
        ERROR_NOT_A_CORRECT_OBJECT_CLASS = 0xC00E008C,

        //
        // MessageId: MQ_ERROR_MULTI_SORT_KEYS
        //
        // MessageText:
        //
        // The value of cCol in MQSORTSET cannot be greater than 1. Active Directory Domain Services supports only a single sort key.
        //
        ERROR_MULTI_SORT_KEYS = 0xC00E008D,

        //
        // MessageId: MQ_ERROR_GC_NEEDED
        //
        // MessageText:
        //
        // An MSMQ Configuration (msmq) object with the GUID supplied cannot be created. By default, an Active Directory Domain Services forest does not support adding an object with a supplied GUID.
        //
        ERROR_GC_NEEDED = 0xC00E008E,

        //
        // MessageId: MQ_ERROR_DS_BIND_ROOT_FOREST
        //
        // MessageText:
        //
        // Binding to the forest root failed. This error usually indicates a problem in the DNS configuration.
        //
        ERROR_DS_BIND_ROOT_FOREST = 0xC00E008F,

        //
        // MessageId: MQ_ERROR_DS_LOCAL_USER
        //
        // MessageText:
        //
        // A local user is authenticated as an anonymous user and cannot access Active Directory Domain Services. You need to log on as a domain user to access Active Directory Domain Services.
        //
        ERROR_DS_LOCAL_USER = 0xC00E0090,

        //
        // MessageId: MQ_ERROR_Q_ADS_PROPERTY_NOT_SUPPORTED
        //
        // MessageText:
        //
        // The column PROPID_Q_ADS_PATH is not supported for the MQLocateBegin API.
        //
        ERROR_Q_ADS_PROPERTY_NOT_SUPPORTED = 0xC00E0091,

        //
        // MessageId: MQ_ERROR_BAD_XML_FORMAT
        //
        // MessageText:
        //
        // The given property is not a valid XML document.
        //
        ERROR_BAD_XML_FORMAT = 0xC00E0092,

        //
        // MessageId: MQ_ERROR_UNSUPPORTED_CLASS
        //
        // MessageText:
        //
        // The Active Directory Domain Services object specified is not an instance of a supported class.
        //
        ERROR_UNSUPPORTED_CLASS = 0xC00E0093,

        //
        // MessageId: MQ_ERROR_UNINITIALIZED_OBJECT
        //
        // MessageText:
        //
        // The MSMQManagement object must be initialized before it is used.
        //
        ERROR_UNINITIALIZED_OBJECT = 0xC00E0094,

        //
        // MessageId: MQ_ERROR_CANNOT_CREATE_PSC_OBJECTS
        //
        // MessageText:
        //
        // The object that will be created should be owned by a primary site controller. The operation cannot be performed.
        //
        ERROR_CANNOT_CREATE_PSC_OBJECTS = 0xC00E0095,

        //
        // MessageId: MQ_ERROR_CANNOT_UPDATE_PSC_OBJECTS
        //
        // MessageText:
        //
        // The object that will be updated is owned by a primary site controller. The operation cannot be performed.
        //
        ERROR_CANNOT_UPDATE_PSC_OBJECTS = 0xC00E0096,

        //
        // MessageId: MQ_ERROR_RESOLVE_ADDRESS
        //
        // MessageText:
        //
        // Message Queuing is not able to resolve the address specified by the user. The address may be wrong or DNS look-up for address failed.
        //
        ERROR_RESOLVE_ADDRESS = 0xC00E0099,

        //
        // MessageId: MQ_ERROR_TOO_MANY_PROPERTIES
        //
        // MessageText:
        //
        // Too many properties passed to the function. Message Queuing can process up to 128 properties in one call.
        //
        ERROR_TOO_MANY_PROPERTIES = 0xC00E009A,

        //
        // MessageId: MQ_ERROR_MESSAGE_NOT_AUTHENTICATED
        //
        // MessageText:
        //
        // The queue only accepts authenticated messages.
        //
        ERROR_MESSAGE_NOT_AUTHENTICATED = 0xC00E009B,

        //
        // MessageId: MQ_ERROR_MESSAGE_LOCKED_UNDER_TRANSACTION
        //
        // MessageText:
        //
        // The message is currently being processed under a transaction. Till the transaction outcome is determined, the message cannot be processed in any other transaction.
        //
        ERROR_MESSAGE_LOCKED_UNDER_TRANSACTION = 0xC00E009C,
    }
}