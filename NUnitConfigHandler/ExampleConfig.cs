using System;
using System.Collections.Generic;
using ConfigHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace NUnitConfigHandler
{
    // Just a dummy example
    public class ExampleConfig : BaseConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Priority
        {
            Undefined,
            Low,
            Medium,
            High
        }

        [EnvVar("EXAMPLE_BATCH_PRIORITY")]
        [Option("Priority of batch")]
        public Priority BatchPriority { get; set; }

        [EnvVar("EXAMPLE_SERVER")]
        public string Server { get; set; }

        [Option("Test date", "yyyy/MM/dd")]
        public DateTime TestDate { get; set; }

        [Option("List of mail addresses")]
        public List<string> MailRecipients { get; set; }

        [Option("Level")]
        [DefaultValue(true)]
        public int Level { get; set; }

        [Option("Some data")]
        public List<string> Data { get; set; }

        [Option("Test date (nullable)", "yyyy/MM/dd")]
        public DateTime? TestDateNullable { get; set; }

        public ExampleConfig()
        {
        }
    }
}
