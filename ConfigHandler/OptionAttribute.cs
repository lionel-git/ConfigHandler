using System;

namespace ConfigHandler
{
    /// <summary>
    /// Meta data for describing an option
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
        /// <summary>
        /// Help on the option
        /// </summary>
        public string HelpMessage { get; set; }

        /// <summary>
        /// Try to display value from ToString('DisplayFormat')
        /// </summary>
        public string DisplayFormat { get; set; }

        /// <summary>
        /// Create an OptionAttribute
        /// </summary>
        /// <param name="helpMessage"></param>
        /// <param name="displayFormat"></param>
        public OptionAttribute(string helpMessage, string displayFormat = null)
        {
            HelpMessage = helpMessage;
            DisplayFormat = displayFormat;
        }
    }
}
