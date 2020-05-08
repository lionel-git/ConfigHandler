using log4net;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConfigHandler
{
    /// <summary>
    /// Base class for definig configs
    /// </summary>
    public class BaseConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BaseConfig));

        /// <summary>
        /// Path to the config file
        /// </summary>
        public string ConfigFile { get; set; }

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
                else
                {
                    property.SetValue(this, TypeDescriptor.GetConverter(property.PropertyType).ConvertFrom(propertyValue));
                }               
            }
            else
            {
                throw new Exception($"Cannot find property: '{propertyName}'");
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
                    UpdateProperty(tokens[0].Substring(2, tokens[0].Length - 2), tokens[1]);
            }
        }
    }
}
