using System;
using System.Collections.Generic;
using ConfigHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NUnitConfigHandler
{
    public class ExampleConfig : BaseConfig
    {

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Color
        {
            Undefined,
            Red,
            Green,
            Blue
        }

        [EnvVar("MYCONFIG_MYCOLOR")]
        public Color MyColor { get; set; }

        public List<Color> MyColors { get; set; }

        [EnvVar("MYCONFIG_MYMACHINE")]
        public string Machine { get; set; }

        [Option("Test a date", "yyyy/MM/dd")]
        public DateTime TestDate { get; set; }

        public ExampleConfig()
        {
            MyColors = new List<Color>();
        }
    }
}
