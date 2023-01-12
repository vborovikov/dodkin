namespace Dodkin
{
    /// <summary>
    /// Specifies how the message is read in the queue.
    /// </summary>
    enum ReadAction : uint
    {
        /// <summary>
        /// Reads the message at the current cursor location and removes it from the queue.
        /// </summary>
        Receive     = 0x00000000,

        /// <summary>
        /// Reads the message at the current cursor location but does not remove it from the queue.
        /// The cursor remains pointing at the current message.
        /// </summary>
        PeekCurrent = 0x80000000,

        /// <summary>
        /// Reads the next message in the queue (skipping the message at the current cursor location)
        /// but does not remove it from the queue.
        /// </summary>
        PeekNext    = 0x80000001,
    }
}