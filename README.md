# ConfigHandler

## Summary
ConfigHandler allows to load a config file (json format) and possibly override some parameters from command line arguments

The default values may also be set from environment variables.

The config may recursively reference a "parent" config file.

- Usage example:
```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ConfigHandler;

 public class DummyConfig : BaseConfig
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

        public MyConfig()
        {
            MyColors = new List<Color>();
        }
    }

....

    static void Main(string[] args)
    {
        var config = BaseConfig.LoadAll<ExampleConfig>("exampleConfig.json", args);
        ...
    }

```

- Example config files:

exampleConfig.json
```json
{
  "MyColor": "Red",
  "MyColors": [
    "Red",
    "Blue"
  ],
  "ColorMeaning": {
    "Red": "Angry",
    "Green": "Happy"
  }
}

```
