using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.Runtime.Versioning;

namespace ConfigHandler
{
    /// <summary>
    /// Base class for definig configs
    /// </summary>
    public class BaseConfig
    {
        private const String EmptyValue = "\"\"";

        /// <summary>
        /// Path to the config file
        /// </summary>
        [OptionAttribute("The config file to use for startup")]
        public string ConfigFile { get; set; }

        /// <summary>
        /// Optional parent config file
        /// If set, first load parent config and then populate from this config file
        /// This can be recursive up to 10 levels
        /// </summary>
        [OptionAttribute("Optional parent config file")]
        public string ParentConfigFile { get; set; }

        /// <summary>
        /// If set, display help
        /// </summary>
        [OptionAttribute("Display help")]
        public bool Help { get; set; }

        /// <summary>
        /// If set, display versions informations
        /// </summary>
        [OptionAttribute("Display versions information")]
        public bool Version { get; set; }

        private static bool _customJsonSerializerSettings = false;

        private static ILogger _logger;

        /// <summary>
        /// Option to redefine serialisation settings
        /// </summary>
        /// <param name="settings"></param>
        public static void SetDefaultJsonConfig(JsonSerializerSettings settings)
        {
            // This seems local to the assembly
            JsonConvert.DefaultSettings = () => settings;
            _customJsonSerializerSettings = true;
        }

        // Rem: place [System.ComponentModel.DefaultValueAttribute(true)] to force default value (enum)
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings()
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        static BaseConfig()
        {
            // Set default settings only if user has not defined them
            if (!_customJsonSerializerSettings)
                SetDefaultJsonConfig(DefaultJsonSerializerSettings);
        }

        /// <summary>
        /// Instanciate an empty BaseConfig
        /// </summary>
        public BaseConfig()
        {
        }

        /// <summary>
        /// Set Logger
        /// </summary>
        /// <param name="logger"></param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        private static T Load<T>(string path) where T : BaseConfig
        {
            var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            config.ConfigFile = path;
            return config;
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

        private static string GetValueFormatted(object value, string displayFormat, string propertyName)
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
                    _logger?.WarnMsg($"Formatting not supported for type '{value.GetType()}' (propertyName='{propertyName}')");
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
        private static string GetLastType(Type type)
        {
            var tokens = type.ToString().Split('.');
            return tokens[tokens.Length - 1];
        }

        private static string GetGenericTypes(Type[] types)
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
        private static string GetPropertyType(PropertyInfo property)
        {
            return $"{property.PropertyType.Name}{GetGenericTypes(property.PropertyType.GetGenericArguments())}";
        }

        private static string GetEnumValues(PropertyInfo property)
        {
            var itemType = property.PropertyType.IsGenericType ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;
            if (itemType.IsEnum)
                return Helpers.GetEnumerableAsString(Enum.GetValues(itemType));
            else
                return null;
        }

        private static string GetOptionHelp(PropertyInfo property, out string displayFormat)
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

        private static void CheckExit(bool exitProgram)
        {
            if (exitProgram)
                Environment.Exit(0);
        }

        /// <summary>
        /// Display help on console, with an optional exit
        /// </summary>
        /// <param name="exitProgram"></param>
        public virtual void ShowHelp(bool exitProgram)
        {
            Console.WriteLine($"Syntax: {Assembly.GetEntryAssembly().GetName().Name} --option1=... --option2=...");
            Console.WriteLine("Lists are comma separated");
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
            CheckExit(exitProgram);
        }

        private static string GetShortName(Assembly assembly)
        {
            return $"{assembly.GetName().Name}, {assembly.GetName().Version}";
        }

        private static void ShowCustomAttributes<T>(Assembly assembly, string propertyName) where T : class 
        {
            var attributes = assembly.GetCustomAttributes(typeof(T));
            foreach (var attribute in attributes)
            {
                var customAttribute = attribute as T;
                var property = customAttribute.GetType().GetProperty(propertyName);
                Console.WriteLine($"\t{propertyName} = {property.GetValue(customAttribute)}");
            }
        }

        /// <summary>
        /// Display version infos
        /// </summary>
        /// <param name="exitProgram"></param>
        public virtual void ShowVersion(bool exitProgram)
        {
            Console.WriteLine("Assembly Versions:");
            Console.WriteLine($"Entry    : {GetShortName(Assembly.GetEntryAssembly())}");
            Console.WriteLine($"Executing: {GetShortName(Assembly.GetExecutingAssembly())}");
            Console.WriteLine("=====");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var fullName = assembly.FullName;
                if (!fullName.StartsWith("System.", StringComparison.Ordinal))
                {
                    var location = assembly.IsDynamic ? "Dynamic" : assembly.Location;
                    Console.WriteLine($"{GetShortName(assembly)} ({location})");
                    ShowCustomAttributes<TargetFrameworkAttribute>(assembly, "FrameworkName");
                    ShowCustomAttributes<AssemblyInformationalVersionAttribute>(assembly, "InformationalVersion");
                    ShowCustomAttributes<AssemblyConfigurationAttribute>(assembly, "Configuration");
                    Console.WriteLine();
                }
            }
            CheckExit(exitProgram);
        }

        private void UpdateProperty(string propertyName, string propertyValue)
        {
            var property = GetType().GetProperty(propertyName);
            if (property != null)
            {
                var targetType = property.PropertyType;
                if (propertyValue != null)
                {
                    if (targetType.IsGenericType)
                    {
                        if (targetType.GetGenericArguments().Length == 1)
                        {
                            var itemType = targetType.GetGenericArguments()[0];
                            var addMethod = targetType.GetMethod("Add", new Type[] { itemType });
                            if (addMethod != null)
                            {
                                var newList = Activator.CreateInstance(targetType);
                                var tokens = propertyValue.Split(',');
                                foreach (var token in tokens)
                                {
                                    addMethod.Invoke(newList, new object[] { TypeDescriptor.GetConverter(itemType).ConvertFrom(token) });
                                }
                                property.SetValue(this, newList);
                            }
                            else
                            {
                                _logger?.WarnMsg($"method Add on type '{itemType}' not found");
                            }
                        }
                        else
                        {
                            _logger?.WarnMsg($"Generic type '{targetType.FullName}' not handled!");
                        }
                    }
                    else
                    {
                        property.SetValue(this, TypeDescriptor.GetConverter(targetType).ConvertFrom(propertyValue));
                    }
                }
                else
                {
                    // property Value null allowed for bool
                    if (targetType == typeof(bool))
                        property.SetValue(this, true);
                    else
                    {
                        throw new ConfigHandlerException($"No value passed for '{propertyName}'?");
                    }
                }
            }
            else
            {
                throw new ConfigHandlerException($"Invalid config parameter: '{propertyName}'. Use --Help to display available parameters");
            }
        }

        private void UpdateFromCmdLine(string[] args, bool showHelpVersion)
        {
            if (args != null)
            {
                foreach (var arg in args)
                {
                    var tokens = arg.Split('=');
                    if (tokens[0].StartsWith("--", StringComparison.Ordinal))
                    {
                        var value = tokens.Length >= 2 ? tokens[1] : null;
                        UpdateProperty(tokens[0].Substring(2), value);
                    }
                    else
                    {
                        var msg = $"Invalid parameter: '{arg}'";
                        _logger?.ErrorMsg(msg);
                        ShowHelp(false);
                        throw new ConfigHandlerException(msg);
                    }
                }
            }
            if (Version && showHelpVersion)
                ShowVersion(false);
            if (Help && showHelpVersion)
                ShowHelp(false);
        }

        private static string GetConfigFileFromCmdLine<T>(string[] args, string defaultConfigfile, bool showHelpVersion) where T : BaseConfig, new()
        {
            var config = new T();
            config.UpdateFromCmdLine(args, showHelpVersion);
            if (config.ConfigFile != null)
                return config.ConfigFile;
            else
                return defaultConfigfile;
        }

        private void UpdateFromSpecificConfig(string updateConfigPath)
        {
            var json = File.ReadAllText(updateConfigPath);
            JsonConvert.PopulateObject(json, this,
                new JsonSerializerSettings()
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <param name="DefaultConfigFile"></param>
        /// <param name="showHelpVersion"></param>
        /// <returns></returns>
        public static T LoadAll<T>(string DefaultConfigFile, string[] args = null, bool showHelpVersion = true) where T : BaseConfig, new()
        {
            var configFile = GetConfigFileFromCmdLine<T>(args, DefaultConfigFile, showHelpVersion);
            T config;
            if (!string.IsNullOrEmpty(configFile))
            {
                _logger?.InfoMsg($"Loading config file: '{configFile}'");
                var stack = new Stack<string>();
                while (!string.IsNullOrEmpty(configFile))
                {
                    stack.Push(configFile);
                    configFile = Load<T>(configFile).ParentConfigFile;
                    if (stack.Count > 10)
                        throw new ConfigHandlerException($"Recursion level exceeded: {stack.Count} => {string.Join(",", stack.ToList())}");
                }
                config = Load<T>(stack.Pop()); // throw if 0 elts, should not happen
                while (stack.Count > 0)
                    config.UpdateFromSpecificConfig(stack.Pop());
            }
            else
                config = new T();
            config.UpdateFromCmdLine(args, false);
            return config;
        }
    }
}
