namespace Dodkin.Interop;

static partial class MQ
{
    public static class PROPID
    {
        // Message Properties
        public static class M
        {
            public const int Count = 75;

            public const int BASE                  = 0;
            public const int CLASS                 = (BASE + 1);     // VT_UI2
            public const int MSGID                 = (BASE + 2);     // VT_UI1|VT_VECTOR
            public const int CORRELATIONID         = (BASE + 3);     // VT_UI1|VT_VECTOR
            public const int PRIORITY              = (BASE + 4);     // VT_UI1
            public const int DELIVERY              = (BASE + 5);     // VT_UI1
            public const int ACKNOWLEDGE           = (BASE + 6);     // VT_UI1
            public const int JOURNAL               = (BASE + 7);     // VT_UI1
            public const int APPSPECIFIC           = (BASE + 8);     // VT_UI4
            public const int BODY                  = (BASE + 9);     // VT_UI1|VT_VECTOR
            public const int BODY_SIZE             = (BASE + 10);    // VT_UI4
            public const int LABEL                 = (BASE + 11);    // VT_LPWSTR
            public const int LABEL_LEN             = (BASE + 12);    // VT_UI4
            public const int TIME_TO_REACH_QUEUE   = (BASE + 13);    // VT_UI4
            public const int TIME_TO_BE_RECEIVED   = (BASE + 14);    // VT_UI4
            public const int RESP_QUEUE            = (BASE + 15);    // VT_LPWSTR
            public const int RESP_QUEUE_LEN        = (BASE + 16);    // VT_UI4
            public const int ADMIN_QUEUE           = (BASE + 17);    // VT_LPWSTR
            public const int ADMIN_QUEUE_LEN       = (BASE + 18);    // VT_UI4
            public const int VERSION               = (BASE + 19);    // VT_UI4
            public const int SENDERID              = (BASE + 20);    // VT_UI1|VT_VECTOR
            public const int SENDERID_LEN          = (BASE + 21);    // VT_UI4
            public const int SENDERID_TYPE         = (BASE + 22);    // VT_UI4
            public const int PRIV_LEVEL            = (BASE + 23);    // VT_UI4
            public const int AUTH_LEVEL            = (BASE + 24);    // VT_UI4
            public const int AUTHENTICATED         = (BASE + 25);    // VT_UI1
            public const int HASH_ALG              = (BASE + 26);    // VT_UI4
            public const int ENCRYPTION_ALG        = (BASE + 27);    // VT_UI4
            public const int SENDER_CERT           = (BASE + 28);    // VT_UI1|VT_VECTOR
            public const int SENDER_CERT_LEN       = (BASE + 29);    // VT_UI4
            public const int SRC_MACHINE_ID        = (BASE + 30);    // VT_CLSID
            public const int SENTTIME              = (BASE + 31);    // VT_UI4
            public const int ARRIVEDTIME           = (BASE + 32);    // VT_UI4
            public const int DEST_QUEUE            = (BASE + 33);    // VT_LPWSTR
            public const int DEST_QUEUE_LEN        = (BASE + 34);    // VT_UI4
            public const int EXTENSION             = (BASE + 35);    // VT_UI1|VT_VECTOR
            public const int EXTENSION_LEN         = (BASE + 36);    // VT_UI4
            public const int SECURITY_CONTEXT      = (BASE + 37);    // VT_UI4
            public const int CONNECTOR_TYPE        = (BASE + 38);    // VT_CLSID
            public const int XACT_STATUS_QUEUE     = (BASE + 39);    // VT_LPWSTR
            public const int XACT_STATUS_QUEUE_LEN = (BASE + 40);    // VT_UI4
            public const int TRACE                 = (BASE + 41);    // VT_UI1
            public const int BODY_TYPE             = (BASE + 42);    // VT_UI4
            public const int DEST_SYMM_KEY         = (BASE + 43);    // VT_UI1|VT_VECTOR
            public const int DEST_SYMM_KEY_LEN     = (BASE + 44);    // VT_UI4
            public const int SIGNATURE             = (BASE + 45);    // VT_UI1|VT_VECTOR
            public const int SIGNATURE_LEN         = (BASE + 46);    // VT_UI4
            public const int PROV_TYPE             = (BASE + 47);    // VT_UI4
            public const int PROV_NAME             = (BASE + 48);    // VT_LPWSTR
            public const int PROV_NAME_LEN         = (BASE + 49);    // VT_UI4
            public const int FIRST_IN_XACT         = (BASE + 50);    // VT_UI1
            public const int LAST_IN_XACT          = (BASE + 51);    // VT_UI1
            public const int XACTID                = (BASE + 52);    // VT_UI1|VT_VECTOR
            public const int AUTHENTICATED_EX      = (BASE + 53);    // VT_UI1
            public const int RESP_FORMAT_NAME      = (BASE + 54);    // VT_LPWSTR
            public const int RESP_FORMAT_NAME_LEN  = (BASE + 55);    // VT_UI4
            public const int DEST_FORMAT_NAME      = (BASE + 58);    // VT_LPWSTR
            public const int DEST_FORMAT_NAME_LEN  = (BASE + 59);    // VT_UI4
            public const int LOOKUPID              = (BASE + 60);    // VT_UI8
            public const int SOAP_ENVELOPE         = (BASE + 61);    // VT_LPWSTR
            public const int SOAP_ENVELOPE_LEN     = (BASE + 62);    // VT_UI4
            public const int COMPOUND_MESSAGE      = (BASE + 63);    // VT_UI1|VT_VECTOR
            public const int COMPOUND_MESSAGE_SIZE = (BASE + 64);    // VT_UI4
            public const int SOAP_HEADER           = (BASE + 65);    // VT_LPWSTR
            public const int SOAP_BODY             = (BASE + 66);    // VT_LPWSTR
            public const int DEADLETTER_QUEUE      = (BASE + 67);    // VT_LPWSTR
            public const int DEADLETTER_QUEUE_LEN  = (BASE + 68);    // VT_UI4
            public const int ABORT_COUNT           = (BASE + 69);    // VT_UI4
            public const int MOVE_COUNT            = (BASE + 70);    // VT_UI4
            public const int LAST_MOVE_TIME        = (BASE + 75);    // VT_UI4
        }

        // Queue Properties
        public static class Q
        {
            public const int Count = 26;

            public const int BASE              = 100;
            public const int INSTANCE          = (BASE +  1);    // VT_CLSID
            public const int TYPE              = (BASE +  2);    // VT_CLSID
            public const int PATHNAME          = (BASE +  3);    // VT_LPWSTR
            public const int JOURNAL           = (BASE +  4);    // VT_UI1
            public const int QUOTA             = (BASE +  5);    // VT_UI4
            public const int BASEPRIORITY      = (BASE +  6);    // VT_I2
            public const int JOURNAL_QUOTA     = (BASE +  7);    // VT_UI4
            public const int LABEL             = (BASE +  8);    // VT_LPWSTR
            public const int CREATE_TIME       = (BASE +  9);    // VT_I4
            public const int MODIFY_TIME       = (BASE + 10);    // VT_I4
            public const int AUTHENTICATE      = (BASE + 11);    // VT_UI1
            public const int PRIV_LEVEL        = (BASE + 12);    // VT_UI4
            public const int TRANSACTION       = (BASE + 13);    // VT_UI1
            public const int PATHNAME_DNS      = (BASE + 24);    // VT_LPWSTR
            public const int MULTICAST_ADDRESS = (BASE + 25);    // VT_LPWSTR
            public const int ADS_PATH          = (BASE + 26);    // VT_LPWSTR

            public enum TRANSACTIONAL : byte
            {
                NONE = 0,
                TRUE = 1,
            }
        }

        //  Machine Properties
        public static class QM
        {
            public const int BASE                   = 200;
            public const int SITE_ID                = (BASE +  1);    // VT_CLSID
            public const int MACHINE_ID             = (BASE +  2);    // VT_CLSID
            public const int PATHNAME               = (BASE +  3);    // VT_LPWSTR
            public const int CONNECTION             = (BASE +  4);    // VT_LPWSTR|VT_VECTOR
            public const int ENCRYPTION_PK          = (BASE +  5);    // VT_UI1|VT_VECTOR
            public const int ENCRYPTION_PK_BASE     = (BASE + 31);    // VT_UI1|VT_VECTOR
            public const int ENCRYPTION_PK_ENHANCED = (BASE + 32);    // VT_UI1|VT_VECTOR
            public const int PATHNAME_DNS           = (BASE + 33);    // VT_LPWSTR
            public const int ENCRYPTION_PK_AES      = (BASE + 44);    // VT_UI1|VT_VECTOR
        }

        //  Private Computer Properties
        public static class PC
        {
            public const int BASE       = 5800;
            public const int VERSION    = (BASE + 1);   // VT_UI4
            public const int DS_ENABLED = (BASE + 2);   // VT_BOOL
        }

        //  Local Admin MSMQ Machine Properties
        public static class MGMT_MSMQ
        {
            public const int Count = 6;

            public const int BASE                = 0;
            public const int ACTIVEQUEUES        = (BASE + 1);    // VT_LPWSTR | VT_VECTOR
            public const int PRIVATEQ            = (BASE + 2);    // VT_LPWSTR | VT_VECTOR
            public const int DSSERVER            = (BASE + 3);    // VT_LPWSTR
            public const int CONNECTED           = (BASE + 4);    // VT_LPWSTR
            public const int TYPE                = (BASE + 5);    // VT_LPWSTR
            public const int BYTES_IN_ALL_QUEUES = (BASE + 6);    // VT_UI8
        }

        //  Local Admin MSMQ Queue Properties
        public static class MGMT_QUEUE
        {
            public const int Count = 27;

            public const int BASE                  = 0;
            public const int PATHNAME              = (BASE + 1);     // VT_LPWSTR
            public const int FORMATNAME            = (BASE + 2);     // VT_LPWSTR
            public const int TYPE                  = (BASE + 3);     // VT_LPWSTR
            public const int LOCATION              = (BASE + 4);     // VT_LPWSTR
            public const int XACT                  = (BASE + 5);     // VT_LPWSTR
            public const int FOREIGN               = (BASE + 6);     // VT_LPWSTR
            public const int MESSAGE_COUNT         = (BASE + 7);     // VT_UI4
            public const int BYTES_IN_QUEUE        = (BASE + 8);     // VT_UI4
            public const int JOURNAL_MESSAGE_COUNT = (BASE + 9);     // VT_UI4
            public const int BYTES_IN_JOURNAL      = (BASE + 10);    // VT_UI4
            public const int STATE                 = (BASE + 11);    // VT_LPWSTR
            public const int NEXTHOPS              = (BASE + 12);    // VT_LPWSTR|VT_VECTOR
            public const int EOD_LAST_ACK          = (BASE + 13);    // VT_BLOB
            public const int EOD_LAST_ACK_TIME     = (BASE + 14);    // VT_I4
            public const int EOD_LAST_ACK_COUNT    = (BASE + 15);    // VT_UI4
            public const int EOD_FIRST_NON_ACK     = (BASE + 16);    // VT_BLOB
            public const int EOD_LAST_NON_ACK      = (BASE + 17);    // VT_BLOB
            public const int EOD_NEXT_SEQ          = (BASE + 18);    // VT_BLOB
            public const int EOD_NO_READ_COUNT     = (BASE + 19);    // VT_UI4
            public const int EOD_NO_ACK_COUNT      = (BASE + 20);    // VT_UI4
            public const int EOD_RESEND_TIME       = (BASE + 21);    // VT_I4
            public const int EOD_RESEND_INTERVAL   = (BASE + 22);    // VT_UI4
            public const int EOD_RESEND_COUNT      = (BASE + 23);    // VT_UI4
            public const int EOD_SOURCE_INFO       = (BASE + 24);    // VT_VARIANT|VT_VECTOR
            public const int CONNECTION_HISTORY    = (BASE + 25);    // VT_BLOB | VT_VECTOR
            public const int SUBQUEUE_COUNT        = (BASE + 26);    // VT_UI4
            public const int SUBQUEUE_NAMES        = (BASE + 27);    // VT_LPWSTR|VT_VECTOR
        }
    }
}
