using log4net;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ConfigHandler
{
    /// <summary>
    /// Base class for definig configs
    /// </summary>
    public class BaseConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BaseConfig));

        private const String EmptyValue = "\"\"";

        /// <summary>
        /// Path to the config file
        /// </summary>
        public string ConfigFile { get; set; }

        /// <summary>
        /// If set, display help
        /// </summary>
        public bool Help { get; set; }

        /// <summary>
        /// Instanciate an empty BaseConfig
        /// </summary>
        public BaseConfig()
        {
            ConfigFile = "baseConfig.json";
        }

        /// <summary>
        /// Load a config from a file
        /// </summary>
        /// <typeparam name="T">Type of config to create</typeparam>
        /// <param name="path">Path to file</param>
        /// <returns></returns>
        public static T Load<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        /// <summary>
        /// Save config to a file
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Return config as json string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        private string GetPropertyValue(PropertyInfo property)
        {
            var value = property.GetValue(this);
            string valueString = value?.ToString();            
            var list = value as IList;
            if (list != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(list[i]);
                }
                valueString = sb.ToString();
            }
            return string.IsNullOrEmpty(valueString) ? EmptyValue : valueString;
        }

        // System.String => String
        private string GetLastType(string typeString)
        {
            var tokens = typeString.Split('.');
            return tokens[tokens.Length - 1];
        }

        // System.Collections.Generic.List`1[System.String] => List<String>
        private string GetPropertyType(PropertyInfo property)
        {
            var tokens = property.PropertyType.ToString().Split('[');
            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i > 0)
                    sb.Append("<");
                sb.Append(GetLastType(tokens[i]));
            }
            for (int i = 0; i < tokens.Length-1; i++)
                sb.Append(">");
            return sb.ToString();
        }

        /// <summary>
        /// Display help
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine($"Syntax: {Assembly.GetExecutingAssembly().GetName().Name} --option1=... -option2=...");
            Console.WriteLine($"List are comma separated");
            foreach (var property in GetType().GetProperties())
            {
                Console.WriteLine($"--{property.Name,-20} {GetPropertyValue(property),-25} ({GetPropertyType(property)})");
            }
        }

        private void UpdateProperty(string propertyName, string propertyValue)
        {
            var property = GetType().GetProperty(propertyName);
            if (property != null)
            {
                if (property.PropertyType.IsGenericType)
                {
                    if (property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var itemType = property.PropertyType.GetGenericArguments()[0];
                        var listType = typeof(List<>).MakeGenericType(new[] { itemType });
                        var newList = (IList)Activator.CreateInstance(listType);
                        var tokens = propertyValue.Split(",");
                        foreach (var token in tokens)
                        {
                            newList.Add(TypeDescriptor.GetConverter(itemType).ConvertFrom(token));
                        }
                        property.SetValue(this, newList);
                    }
                    else
                    {
                        throw new Exception($"Generic type '{property.PropertyType.FullName}' not handled!");
                    }
                }
                else if (propertyValue != null)
                {
                    property.SetValue(this, TypeDescriptor.GetConverter(property.PropertyType).ConvertFrom(propertyValue));
                }
                else
                {
                    // property Value null allowed for bool
                    if (property.PropertyType == typeof(bool))
                        property.SetValue(this, true);
                    else
                    {
                        throw new Exception($"No value passed for '{propertyName}'?");
                    }
                }
            }
            else
            {
                throw new Exception($"Cannot find property: '{propertyName}'");
            }
            if (propertyName == nameof(Help))
            {
                ShowHelp();
            }
        }

        /// <summary>
        /// Return name of config file to load
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string CheckCmdConfigFile(string[] args)
        {
            foreach (var arg in args)
            {
                var tokens = arg.Split('=');
                if (tokens[0].StartsWith("--") && tokens[0].Substring(2, tokens[0].Length - 2) == nameof(ConfigFile))
                        return tokens[1];
            }
            return ConfigFile;
        }

        /// <summary>
        /// Override the current config from cmdline parameters
        /// </summary>
        /// <param name="args"></param>
        public void UpdateFromCmdLine(string[] args)
        {
            foreach (var arg in args)
            {
                var tokens = arg.Split('=');
                if (tokens[0].StartsWith("--"))
                {
                    var value = tokens.Length >= 2 ? tokens[1] : null;
                    UpdateProperty(tokens[0].Substring(2, tokens[0].Length - 2), value);
                }
            }
        }
    }
}
