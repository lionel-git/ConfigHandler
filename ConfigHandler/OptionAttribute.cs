using System;
using System.Collections.Generic;
using System.Text;

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
        /// Create an OptionAttribute
        /// </summary>
        public OptionAttribute()
        {
        }

        /// <summary>
        /// Create an OptionAttribute
        /// </summary>
        /// <param name="helpMessage"></param>
        public OptionAttribute(string helpMessage)
        {
            HelpMessage = helpMessage;
        }
    }
}
