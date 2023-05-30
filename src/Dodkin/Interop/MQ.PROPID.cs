namespace Dodkin.Interop;

static partial class MQ
{
    public static class PROPID
    {
        // Message Properties
        public enum M
        {
            BASE                  = 0,
            CLASS                 = (BASE + 1),     // VT_UI2
            MSGID                 = (BASE + 2),     // VT_UI1|VT_VECTOR
            CORRELATIONID         = (BASE + 3),     // VT_UI1|VT_VECTOR
            PRIORITY              = (BASE + 4),     // VT_UI1
            DELIVERY              = (BASE + 5),     // VT_UI1
            ACKNOWLEDGE           = (BASE + 6),     // VT_UI1
            JOURNAL               = (BASE + 7),     // VT_UI1
            APPSPECIFIC           = (BASE + 8),     // VT_UI4
            BODY                  = (BASE + 9),     // VT_UI1|VT_VECTOR
            BODY_SIZE             = (BASE + 10),    // VT_UI4
            LABEL                 = (BASE + 11),    // VT_LPWSTR
            LABEL_LEN             = (BASE + 12),    // VT_UI4
            TIME_TO_REACH_QUEUE   = (BASE + 13),    // VT_UI4
            TIME_TO_BE_RECEIVED   = (BASE + 14),    // VT_UI4
            RESP_QUEUE            = (BASE + 15),    // VT_LPWSTR
            RESP_QUEUE_LEN        = (BASE + 16),    // VT_UI4
            ADMIN_QUEUE           = (BASE + 17),    // VT_LPWSTR
            ADMIN_QUEUE_LEN       = (BASE + 18),    // VT_UI4
            VERSION               = (BASE + 19),    // VT_UI4
            SENDERID              = (BASE + 20),    // VT_UI1|VT_VECTOR
            SENDERID_LEN          = (BASE + 21),    // VT_UI4
            SENDERID_TYPE         = (BASE + 22),    // VT_UI4
            PRIV_LEVEL            = (BASE + 23),    // VT_UI4
            AUTH_LEVEL            = (BASE + 24),    // VT_UI4
            AUTHENTICATED         = (BASE + 25),    // VT_UI1
            HASH_ALG              = (BASE + 26),    // VT_UI4
            ENCRYPTION_ALG        = (BASE + 27),    // VT_UI4
            SENDER_CERT           = (BASE + 28),    // VT_UI1|VT_VECTOR
            SENDER_CERT_LEN       = (BASE + 29),    // VT_UI4
            SRC_MACHINE_ID        = (BASE + 30),    // VT_CLSID
            SENTTIME              = (BASE + 31),    // VT_UI4
            ARRIVEDTIME           = (BASE + 32),    // VT_UI4
            DEST_QUEUE            = (BASE + 33),    // VT_LPWSTR
            DEST_QUEUE_LEN        = (BASE + 34),    // VT_UI4
            EXTENSION             = (BASE + 35),    // VT_UI1|VT_VECTOR
            EXTENSION_LEN         = (BASE + 36),    // VT_UI4
            SECURITY_CONTEXT      = (BASE + 37),    // VT_UI4
            CONNECTOR_TYPE        = (BASE + 38),    // VT_CLSID
            XACT_STATUS_QUEUE     = (BASE + 39),    // VT_LPWSTR
            XACT_STATUS_QUEUE_LEN = (BASE + 40),    // VT_UI4
            TRACE                 = (BASE + 41),    // VT_UI1
            BODY_TYPE             = (BASE + 42),    // VT_UI4
            DEST_SYMM_KEY         = (BASE + 43),    // VT_UI1|VT_VECTOR
            DEST_SYMM_KEY_LEN     = (BASE + 44),    // VT_UI4
            SIGNATURE             = (BASE + 45),    // VT_UI1|VT_VECTOR
            SIGNATURE_LEN         = (BASE + 46),    // VT_UI4
            PROV_TYPE             = (BASE + 47),    // VT_UI4
            PROV_NAME             = (BASE + 48),    // VT_LPWSTR
            PROV_NAME_LEN         = (BASE + 49),    // VT_UI4
            FIRST_IN_XACT         = (BASE + 50),    // VT_UI1
            LAST_IN_XACT          = (BASE + 51),    // VT_UI1
            XACTID                = (BASE + 52),    // VT_UI1|VT_VECTOR
            AUTHENTICATED_EX      = (BASE + 53),    // VT_UI1
            RESP_FORMAT_NAME      = (BASE + 54),    // VT_LPWSTR
            RESP_FORMAT_NAME_LEN  = (BASE + 55),    // VT_UI4
            DEST_FORMAT_NAME      = (BASE + 58),    // VT_LPWSTR
            DEST_FORMAT_NAME_LEN  = (BASE + 59),    // VT_UI4
            LOOKUPID              = (BASE + 60),    // VT_UI8
            SOAP_ENVELOPE         = (BASE + 61),    // VT_LPWSTR
            SOAP_ENVELOPE_LEN     = (BASE + 62),    // VT_UI4
            COMPOUND_MESSAGE      = (BASE + 63),    // VT_UI1|VT_VECTOR
            COMPOUND_MESSAGE_SIZE = (BASE + 64),    // VT_UI4
            SOAP_HEADER           = (BASE + 65),    // VT_LPWSTR
            SOAP_BODY             = (BASE + 66),    // VT_LPWSTR
            DEADLETTER_QUEUE      = (BASE + 67),    // VT_LPWSTR
            DEADLETTER_QUEUE_LEN  = (BASE + 68),    // VT_UI4
            ABORT_COUNT           = (BASE + 69),    // VT_UI4
            MOVE_COUNT            = (BASE + 70),    // VT_UI4
            LAST_MOVE_TIME        = (BASE + 75),    // VT_UI4
        }

        // Queue Properties
        public enum Q
        {
            BASE              = 100,
            INSTANCE          = (BASE +  1),    // VT_CLSID
            TYPE              = (BASE +  2),    // VT_CLSID
            PATHNAME          = (BASE +  3),    // VT_LPWSTR
            JOURNAL           = (BASE +  4),    // VT_UI1
            QUOTA             = (BASE +  5),    // VT_UI4
            BASEPRIORITY      = (BASE +  6),    // VT_I2
            JOURNAL_QUOTA     = (BASE +  7),    // VT_UI4
            LABEL             = (BASE +  8),    // VT_LPWSTR
            CREATE_TIME       = (BASE +  9),    // VT_I4
            MODIFY_TIME       = (BASE + 10),    // VT_I4
            AUTHENTICATE      = (BASE + 11),    // VT_UI1
            PRIV_LEVEL        = (BASE + 12),    // VT_UI4
            TRANSACTION       = (BASE + 13),    // VT_UI1
            PATHNAME_DNS      = (BASE + 24),    // VT_LPWSTR
            MULTICAST_ADDRESS = (BASE + 25),    // VT_LPWSTR
            ADS_PATH          = (BASE + 26),    // VT_LPWSTR
        }

        //  Machine Properties
        public enum QM
        {
            BASE                   = 200,
            SITE_ID                = (BASE +  1),    // VT_CLSID
            MACHINE_ID             = (BASE +  2),    // VT_CLSID
            PATHNAME               = (BASE +  3),    // VT_LPWSTR
            CONNECTION             = (BASE +  4),    // VT_LPWSTR|VT_VECTOR
            ENCRYPTION_PK          = (BASE +  5),    // VT_UI1|VT_VECTOR
            ENCRYPTION_PK_BASE     = (BASE + 31),    // VT_UI1|VT_VECTOR
            ENCRYPTION_PK_ENHANCED = (BASE + 32),    // VT_UI1|VT_VECTOR
            PATHNAME_DNS           = (BASE + 33),    // VT_LPWSTR
            ENCRYPTION_PK_AES      = (BASE + 44),    // VT_UI1|VT_VECTOR
        }

        //  Private Computer Properties
        public enum PC
        {
            BASE       = 5800,
            VERSION    = (BASE + 1),   // VT_UI4
            DS_ENABLED = (BASE + 2),   // VT_BOOL
        }

        //  Local Admin MSMQ Machine Properties
        public enum MGMT_MSMQ
        {
            BASE                = 0,
            ACTIVEQUEUES        = (BASE + 1),    // VT_LPWSTR | VT_VECTOR
            PRIVATEQ            = (BASE + 2),    // VT_LPWSTR | VT_VECTOR
            DSSERVER            = (BASE + 3),    // VT_LPWSTR
            CONNECTED           = (BASE + 4),    // VT_LPWSTR
            TYPE                = (BASE + 5),    // VT_LPWSTR
            BYTES_IN_ALL_QUEUES = (BASE + 6),    // VT_UI8
        }

        //  Local Admin MSMQ Queue Properties
        public enum MGMT_QUEUE
        {
            BASE                  = 0,
            PATHNAME              = (BASE + 1),     // VT_LPWSTR
            FORMATNAME            = (BASE + 2),     // VT_LPWSTR
            TYPE                  = (BASE + 3),     // VT_LPWSTR
            LOCATION              = (BASE + 4),     // VT_LPWSTR
            XACT                  = (BASE + 5),     // VT_LPWSTR
            FOREIGN               = (BASE + 6),     // VT_LPWSTR
            MESSAGE_COUNT         = (BASE + 7),     // VT_UI4
            BYTES_IN_QUEUE        = (BASE + 8),     // VT_UI4
            JOURNAL_MESSAGE_COUNT = (BASE + 9),     // VT_UI4
            BYTES_IN_JOURNAL      = (BASE + 10),    // VT_UI4
            STATE                 = (BASE + 11),    // VT_LPWSTR
            NEXTHOPS              = (BASE + 12),    // VT_LPWSTR|VT_VECTOR
            EOD_LAST_ACK          = (BASE + 13),    // VT_BLOB
            EOD_LAST_ACK_TIME     = (BASE + 14),    // VT_I4
            EOD_LAST_ACK_COUNT    = (BASE + 15),    // VT_UI4
            EOD_FIRST_NON_ACK     = (BASE + 16),    // VT_BLOB
            EOD_LAST_NON_ACK      = (BASE + 17),    // VT_BLOB
            EOD_NEXT_SEQ          = (BASE + 18),    // VT_BLOB
            EOD_NO_READ_COUNT     = (BASE + 19),    // VT_UI4
            EOD_NO_ACK_COUNT      = (BASE + 20),    // VT_UI4
            EOD_RESEND_TIME       = (BASE + 21),    // VT_I4
            EOD_RESEND_INTERVAL   = (BASE + 22),    // VT_UI4
            EOD_RESEND_COUNT      = (BASE + 23),    // VT_UI4
            EOD_SOURCE_INFO       = (BASE + 24),    // VT_VARIANT|VT_VECTOR
            CONNECTION_HISTORY    = (BASE + 25),    // VT_BLOB | VT_VECTOR
            SUBQUEUE_COUNT        = (BASE + 26),    // VT_UI4
            SUBQUEUE_NAMES        = (BASE + 27),    // VT_LPWSTR|VT_VECTOR
        }
    }
}
