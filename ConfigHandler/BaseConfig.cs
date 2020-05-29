using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConfigHandler
{
    /// <summary>
    /// Base class for defining a config class
    /// </summary>
    public class BaseConfig
    {
        private const String EmptyValue = "\"\"";

        /// <summary>
        /// Options for "--Version" command line option
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum VersionOption
        {
            /// <summary>
            /// Do not display version infos
            /// </summary>
            False,
            /// <summary>
            /// Display infos for non system assemblies
            /// </summary>
            True,
            /// <summary>
            /// Display infos for all assemblies
            /// </summary>
            All
        }

        /// <summary>
        /// Path to the config file
        /// </summary>
        [Option("The config file to use for startup")]
        public string ConfigFile { get; set; }

        /// <summary>
        /// Optional parent config file
        /// If set, first load parent config and then populate from this config file
        /// This can be recursive up to 10 levels
        /// </summary>
        [Option("Optional parent config file")]
        public string ParentConfigFile { get; set; }

        /// <summary>
        /// If set, display help
        /// </summary>
        [Option("Display help")]
        public bool Help { get; set; }

        /// <summary>
        /// If set, display versions informations
        /// </summary>
        [Option("Display versions information")]
        public VersionOption Version { get; set; }

        private static bool _customJsonSerializerSettings = false;

        private static ILogger _logger;

        /// <summary>
        /// Option to redefine serialisation settings (json options)
        /// Default is:
        ///   MissingMemberHandling.Error
        ///   NullValueHandling.Ignore
        ///   DefaultValueHandling.Ignore
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

        /// <summary>
        /// Return true if Version is True or All
        /// </summary>
        /// <returns></returns>
        public bool IsVersionSet()
        {
            return Version == VersionOption.All || Version == VersionOption.True;
        }

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

        private static string GetCustomAttributes<T>(Assembly assembly, string propertyName) where T : class
        {
            var sb = new StringBuilder();
            var attributes = assembly.GetCustomAttributes(typeof(T));
            foreach (var attribute in attributes)
            {
                var customAttribute = attribute as T;
                var property = customAttribute.GetType().GetProperty(propertyName);
                var value = property.GetValue(customAttribute).ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    sb.AppendLine($"\t{propertyName} = {value}");
            }
            return sb.ToString();
        }

        private static bool IsDisplayedAssembly(string name, VersionOption versionOption)
        {
            return (versionOption == VersionOption.All) ||
                    (!name.StartsWith("System.", StringComparison.Ordinal) &&
                     !name.StartsWith("Microsoft.", StringComparison.Ordinal));
        }

        /// <summary>
        /// Return infos on the currently loaded dlls + referenced ones
        /// </summary>
        /// <param name="versionOption">Control type of infos</param>
        /// <returns></returns>
        public static string GetVersion(VersionOption versionOption)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Assembly Versions:");
            sb.AppendLine($"Entry    : {GetShortName(Assembly.GetEntryAssembly())}");
            sb.AppendLine($"Executing: {GetShortName(Assembly.GetExecutingAssembly())}");
            sb.AppendLine("=====");
            sb.AppendLine("Loaded assemblies:");
            var assemblyLoaded = new HashSet<string>();
            var assemblyReferenced = new HashSet<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var fullName = assembly.FullName;
                if (IsDisplayedAssembly(fullName, versionOption))
                {
                    assemblyLoaded.Add(assembly.FullName);
                    foreach (var name in assembly.GetReferencedAssemblies())
                    {
                        var assemblyName = name.ToString();
                        if (IsDisplayedAssembly(assemblyName, versionOption))
                            assemblyReferenced.Add(assemblyName);
                    }
                    var location = assembly.IsDynamic ? "Dynamic" : assembly.Location;
                    sb.AppendLine($"{GetShortName(assembly)} ({location})");
                    sb.Append(GetCustomAttributes<TargetFrameworkAttribute>(assembly, "FrameworkName"));
                    sb.Append(GetCustomAttributes<AssemblyInformationalVersionAttribute>(assembly, "InformationalVersion"));
                    sb.Append(GetCustomAttributes<AssemblyConfigurationAttribute>(assembly, "Configuration"));
                    sb.Append(GetCustomAttributes<AssemblyDescriptionAttribute>(assembly, "Description"));
                    sb.Append(GetCustomAttributes<AssemblyCopyrightAttribute>(assembly, "Copyright"));
                    sb.Append(GetCustomAttributes<AssemblyCompanyAttribute>(assembly, "Company"));
                    sb.AppendLine();
                }
            }
            // Display referenced
            foreach (var loaded in assemblyLoaded)
                assemblyReferenced.Remove(loaded);
            sb.AppendLine("Referenced assemblies (not loaded):");
            foreach (var referenced in assemblyReferenced)
                sb.AppendLine($"\t{referenced}");
            return sb.ToString();
        }


        /// <summary>
        /// Display version infos
        /// </summary>
        /// <param name="exitProgram"></param>
        public virtual void ShowVersion(bool exitProgram)
        {
            Console.WriteLine(GetVersion(Version));
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
                    else if (targetType == typeof(VersionOption))
                        property.SetValue(this, VersionOption.True);
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
            if (IsVersionSet() && showHelpVersion)
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

        private void UpdateFromConfig(string updateConfigPath)
        {
            var json = File.ReadAllText(updateConfigPath);
            JsonConvert.PopulateObject(json, this,
                new JsonSerializerSettings()
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                });
        }

        /// <summary>
        /// Init default values from env vars
        /// </summary>
        private void UpdateFromEnvVars()
        {
            foreach (var property in GetType().GetProperties())
            {
                var envVarAttributes = property.GetCustomAttributes(typeof(EnvVarAttribute), false) as EnvVarAttribute[];
                if (envVarAttributes.Length >= 1)
                {
                    var enVarName = envVarAttributes.First().EnvVarName;
                    var value = Environment.GetEnvironmentVariable(enVarName, EnvironmentVariableTarget.Process);
                    if (value != null)
                        UpdateProperty(property.Name, value);
                }
            }
        }

        private static Stack<string> GetStackParents<T>(string configFile) where T : BaseConfig
        {
            var stack = new Stack<string>();
            while (!string.IsNullOrEmpty(configFile))
            {
                stack.Push(configFile);
                if (stack.Count > 10)
                    throw new ConfigHandlerException($"Recursion level exceeded: {stack.Count} => {string.Join(",", stack.ToList())}");
                configFile = Load<T>(configFile).ParentConfigFile;
            }
            return stack;
        }


        /// <summary>
        /// Load config from optional config file, with optional arguments override
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configFile">The file to load, may recursively reference other config files (see ParentConfigFile)</param>
        /// <param name="args">the command line args to parse</param>
        /// <param name="showHelpVersion">Display Help or Version if either option is set</param>
        /// <returns></returns>
        public static T LoadAll<T>(string configFile, string[] args = null, bool showHelpVersion = true) where T : BaseConfig, new()
        {
            var configFileFinal = GetConfigFileFromCmdLine<T>(args, configFile, showHelpVersion);
            var config = new T();
            config.UpdateFromEnvVars();
            if (!string.IsNullOrEmpty(configFileFinal))
            {
                _logger?.InfoMsg($"Loading config file: '{configFileFinal}'");
                var stack = GetStackParents<T>(configFileFinal);
                while (stack.Count > 0)
                    config.UpdateFromConfig(stack.Pop());
            }
            config.UpdateFromCmdLine(args, false);
            return config;
        }
    }
}
