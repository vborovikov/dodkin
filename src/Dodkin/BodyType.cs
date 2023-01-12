namespace Dodkin
{
    using Interop;

    /// <summary>
    /// The type of the <see cref="Message.Body"/>
    /// </summary>
    public enum BodyType : uint
    {
        /// <summary>Default, same as <see cref="ByteArray"/></summary>
        None = 0,

        /// <summary>The body is a null terminated ASCII string</summary>
        AnsiString = VarType.AnsiString,

        /// <summary>The body is a null terminated UTF-16 string</summary>
        UnicodeString = VarType.String,

        /// <summary>The body is an array of bytes</summary>
        ByteArray = VarType.ByteArray,
    }
}
