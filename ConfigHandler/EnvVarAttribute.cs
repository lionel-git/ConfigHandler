using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigHandler
{
    /// <summary>
    /// MetaData for specifying Env Var link
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EnvVarAttribute : Attribute
    {
        /// <summary>
        /// Corresponding Env Var
        /// </summary>
        public string EnvVarName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="envVarName"></param>
        public EnvVarAttribute(string envVarName)
        {
            EnvVarName = envVarName;
        }
    }
}
