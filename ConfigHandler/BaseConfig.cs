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
    public class BaseConfig
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BaseConfig));

        /// <summary>
        /// Path to the config file
        /// </summary>
        public string ConfigFile { get; set; }

        public BaseConfig()
        {
            ConfigFile = "baseConfig.json";
        }

        public static T Load<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

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
