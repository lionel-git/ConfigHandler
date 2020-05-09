using log4net;
using Microsoft.VisualBasic.CompilerServices;
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
using System.Text.RegularExpressions;

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
        [OptionAttribute("The config file to use for startup")]
        public string ConfigFile { get; set; }

        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings() 
            { 
                MissingMemberHandling = MissingMemberHandling.Error 
            };

        /// <summary>
        /// If set, display help
        /// </summary>
        [OptionAttribute("Display help")]
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
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), DefaultJsonSerializerSettings);
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

        private string GetValueFormatted(object value, string displayFormat, string propertyName)
        {
            if (string.IsNullOrEmpty(displayFormat) || value == null)
                return value?.ToString();
            else
            {
                // Try calling value.ToString('displayFormat');
                //return $"Format:{value?.ToString()}";
                var toStringMethod = value.GetType().GetMethod("ToString", new Type[] { displayFormat.GetType() });
                if (toStringMethod != null)
                    return toStringMethod.Invoke(value, new object[] { displayFormat }).ToString();
                else
                {
                    Logger.Warn($"Formatting not supported for type '{value.GetType()}' (propertyName='{propertyName}')");
                    return value.ToString();
                }
            }
        }


        private string GetPropertyValue(PropertyInfo property, string displayFormat)
        {
            var value = property.GetValue(this);
            IEnumerable list = null;
            if (property.PropertyType != typeof(string)) // string is an Enumerable of char
                list = value as IEnumerable;
            string valueString = list != null ? Helpers.GetEnumerableAsString(list) : GetValueFormatted(value, displayFormat, property.Name);
            return string.IsNullOrEmpty(valueString) ? EmptyValue : valueString;
        }

        // System.String => String
        private string GetLastType(Type type)
        {
            var tokens = type.ToString().Split('.');
            return tokens[tokens.Length - 1];
        }

        private string GetGenericTypes(Type[] types)
        {
            var list = new List<string>();
            foreach (var type in types)
                list.Add(GetLastType(type));
            if (list.Count > 0)
                return $"<{Helpers.GetEnumerableAsString(list, ",")}>";
            else
                return "";
        }

        // System.Collections.Generic.List`1[System.String] => List<String>
        private string GetPropertyType(PropertyInfo property)
        {
            return $"{property.PropertyType.Name}{GetGenericTypes(property.PropertyType.GetGenericArguments())}";
        }

        private string GetEnumValues(PropertyInfo property)
        {
            var itemType = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;
            if (itemType.IsEnum)
                return Helpers.GetEnumerableAsString(Enum.GetValues(itemType));
            else 
                return null;
        }

        private string GetOptionHelp(PropertyInfo property, out string displayFormat)
        {
            var optionAttributes = property.GetCustomAttributes(typeof(OptionAttribute), false) as OptionAttribute[];
            if (optionAttributes.Length >= 1)
            {
                displayFormat = optionAttributes.First().DisplayFormat;
                return optionAttributes.First().HelpMessage;
            }
            else
            {
                displayFormat = null;
                return null;
            }
        }

        /// <summary>
        /// Display help
        /// </summary>
        public void ShowHelp()
        {
            Console.WriteLine($"Syntax: {Assembly.GetExecutingAssembly().GetName().Name} --option1=... --option2=...");
            Console.WriteLine($"Lists are comma separated");
            foreach (var property in GetType().GetProperties())
            {
                Console.WriteLine($"--{property.Name,-20}  ({GetPropertyType(property)})");
                var helpMessage = GetOptionHelp(property, out string displayFormat);
                if (!string.IsNullOrEmpty(helpMessage))
                    Console.WriteLine($"\tHelp: {helpMessage,-30}");
                Console.WriteLine($"\tCurent value: {GetPropertyValue(property, displayFormat),-25}");
                var enumValues = GetEnumValues(property);
                if (!string.IsNullOrEmpty(enumValues))
                    Console.WriteLine($"\tPossible values: {enumValues}");
                Console.WriteLine();
            }
        }

        private void UpdateProperty(string propertyName, string propertyValue)
        {
            var property = GetType().GetProperty(propertyName);
            if (property != null)
            {
                var targetType = property.PropertyType;
                if (targetType.IsGenericType)
                {
                    if (targetType.GetGenericArguments().Length == 1)
                    {
                        var itemType = targetType.GetGenericArguments()[0];
                        var addMethod = targetType.GetMethod("Add", new Type[] { itemType });
                        if (addMethod != null)
                        {
                            var newList = Activator.CreateInstance(targetType);
                            var tokens = propertyValue.Split(",");
                            foreach (var token in tokens)
                            {
                                addMethod.Invoke(newList, new object[] { TypeDescriptor.GetConverter(itemType).ConvertFrom(token) });
                            }
                            property.SetValue(this, newList);
                        }
                        else
                        {
                            Logger.Warn($"method Add on type '{itemType}' not found");
                        }
                    }
                    else
                    {
                        Logger.Warn($"Generic type '{targetType.FullName}' not handled!");
                    }
                }
                else if (propertyValue != null)
                {
                    property.SetValue(this, TypeDescriptor.GetConverter(targetType).ConvertFrom(propertyValue));
                }
                else
                {
                    // property Value null allowed for bool
                    if (targetType == typeof(bool))
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
