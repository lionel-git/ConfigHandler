using System;



namespace ConfigHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigHandlerException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public ConfigHandlerException()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public ConfigHandlerException(string message)
            : base(message)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public ConfigHandlerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
