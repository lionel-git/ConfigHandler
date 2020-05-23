namespace ConfigHandler
{
    /// <summary>
    /// Interface to client logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Emit an info message
        /// </summary>
        /// <param name="msg"></param>
        void InfoMsg(string msg);
        /// <summary>
        /// Emit a warning mesage
        /// </summary>
        /// <param name="msg"></param>
        void WarnMsg(string msg);
        /// <summary>
        /// Emit an error message
        /// </summary>
        /// <param name="msg"></param>
        void ErrorMsg(string msg);
    }
}
