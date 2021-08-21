# ConfigHandler

## Summary
ConfigHandler allows to load a config file (json format) and possibly override some parameters from command line arguments

The default values may also be set from environment variables.

The config may recursively reference a "parent" config file.

Create a class that derives from ConfigHandler.BaseConfig with all the properties you want to configure, then call :

```csharp
var config = BaseConfig.LoadAll<ExampleConfig>("exampleConfig.json", args);
```
And you will get the config loaded from file & arguments


- Usage example:
```csharp
using System;
using System.Collections.Generic;
using ConfigHandler;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        public ExampleConfig()
        {
        }
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
  "ParentConfigFile": "GenericExampleConfig.json",
  "MailRecipients": [
    "joe@joe.com",
    "bill@bill.com"
  ]
}
```

GenericExampleConfig.json
```json
{
  "BatchPriority": "High",
  "MailRecipients": [
    "test@test.com"
  ]
}
```

Final config will be:
```
INFO: Loading config file: 'exampleConfig.json'
{
  "BatchPriority": "High",
  "MailRecipients": [
    "joe@joe.com",
    "bill@bill.com"
  ],
  "ParentConfigFile": "GenericExampleConfig.json"
}
```

--Help option wil display:
```
Syntax: testhost --option1=... --option2=... (lists are comma separated)
Options for config ExampleConfig:

--Help                  (Boolean)
	Help: Display help
	Curent value: True

--BatchPriority         (Priority)
	Help: Priority of batch
	Curent value: Undefined
	Possible values: Undefined,Low,Medium,High
	Associated Env. Var: EXAMPLE_BATCH_PRIORITY

--Server                (String)
	Curent value: ""
	Associated Env. Var: EXAMPLE_SERVER

--TestDate              (DateTime)
	Help: Test date
	Curent value: 0001/01/01

--MailRecipients        (List`1<String>)
	Help: List of mail addresses
	Curent value: ""

--ConfigFile            (String)
	Help: The config file to use for startup
	Curent value: ""

--ParentConfigFile      (String)
	Help: Optional parent config file
	Curent value: ""

--Version               (VersionOption)
	Help: Display versions information
	Curent value: False
	Possible values: False,True,All

```

--Version will display
```
Assembly Versions:
Entry    : testhost, 15.0.0.0
Executing: ConfigHandler, 1.2.1.0
=====
Loaded assemblies:
testhost, 15.0.0.0 (g:\GlobalNugetsCache\microsoft.testplatform.testhost\16.4.0\build\netcoreapp2.1\x64\testhost.dll)
	FrameworkName = .NETCoreApp,Version=v2.1
	InformationalVersion = 16.4.0
	Copyright = © Microsoft Corporation. All rights reserved.
	Company = Microsoft Corporation

netstandard, 2.1.0.0 (C:\Program Files\dotnet\shared\Microsoft.NETCore.App\3.1.4\netstandard.dll)
	InformationalVersion = 3.1.4+059a4a19e602494bfbed473dbbb18f2dbfbd0878
	Description = netstandard
	Copyright = © Microsoft Corporation. All rights reserved.
	Company = Microsoft Corporation

Newtonsoft.Json, 12.0.0.0 (G:\my_projects\ConfigHandler\NUnitConfigHandler\bin\Release\netcoreapp3.1\Newtonsoft.Json.dll)
	FrameworkName = .NETStandard,Version=v2.0
	InformationalVersion = 12.0.3+7c3d7f8da7e35dde8fa74188b0decff70f8f10e3
	Configuration = Release
	Description = Json.NET is a popular high-performance JSON framework for .NET
	Copyright = Copyright © James Newton-King 2008
	Company = Newtonsoft
...
```