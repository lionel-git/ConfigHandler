using System;
using System.Collections.Generic;
using ConfigHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NUnitConfigHandler
{
    public class MyConfig : BaseConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Color
        {
            Undefined,
            Red,
            Green,
            Blue
        }

        public Color MyColor { get; set; }
        public List<Color> MyColors { get; set; }

        public string Machine { get; set; }

        public Dictionary<Color, string> ColorMeaning { get; set; }

        [OptionAttribute("Test a date", "yyyy/MM/dd")]
        public DateTime TestDate { get; set; }

        public HashSet<string> TestHash { get; set; }

        public SortedSet<string> TestSorted { get; set; }

        public Dictionary<string, string> TestDico { get; set; }

        public long TestLong { get; set; }

        public List<List<string>> TestList2 { get; set; }

        public MyConfig()
        {
            MyColors = new List<Color>();
            ColorMeaning = new Dictionary<Color, string>();
        }
    }
}
