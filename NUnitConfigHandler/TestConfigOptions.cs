using System;
using System.Collections.Generic;
using System.IO;
using ConfigHandler;
using DummyLibrary;
using Newtonsoft.Json;
using NUnit.Framework;
using static NUnitConfigHandler.MyConfig;

namespace NUnitConfigHandler
{
    public class Tests
    {
        private const string File1 = "TempConfig1.json";
        private const string File2 = "TempConfig2.json";
        private const string File3 = "TempConfig3.json";

        [SetUp]
        public void Setup()
        {
            BaseConfig.SetDefaultJsonConfig();
            BaseConfig.SetLogger(new ConfigHandlerLogger());
        }

        [Test]
        public void TestCreateSaveLoad()
        {
            var args = new List<string>()
            {
                @"--TestDate=2020/05/02",
                @"--TestHash=abcd,def",
                @"--TestDico=abcd,def",
                @"--TestSorted=gh,zz,abcd",
                @"--TestLong=197"
            };

            var config = new MyConfig
            {
                MyColor = Color.Red
            };
            config.MyColors.Add(Color.Red);
            config.MyColors.Add(Color.Blue);

            config.ColorMeaning.Add(Color.Red, "Angry");
            config.ColorMeaning.Add(Color.Green, "Happy");

            config.TestLong = 17;

            config.Save(File1);
            var config2 = BaseConfig.LoadAll<MyConfig>(File1, args.ToArray());
            Assert.AreEqual(config2.TestLong, 197);

            config2.ConfigFile = null; // Load will set config file
            config2.Save(File2);

            var config3 = BaseConfig.LoadAll<MyConfig>(File2);
            config3.ConfigFile = null; // Load will set config file
            config3.Save(File3);


            var content2 = File.ReadAllText(File2);
            var content3 = File.ReadAllText(File3);

            Assert.AreEqual(content2, content3);
        }

        [Test]
        public void TestCmdLineError()
        {
            bool exception = false;
            var args = new string[] { "toto" };
            try
            {
                var config2 = BaseConfig.LoadAll<MyConfig>("MyConfig1.json", args);

            }
            catch (ConfigHandlerException)
            {
                exception = true;
            }
            Assert.IsTrue(exception);

        }

        [Test]
        public void TestCmdLineOnly()
        {
            var args = new List<string>()
            {
                @"--TestDate=2020/05/02",
                @"--TestHash=abcd,def",
                @"--TestDico=abcd,def",
                @"--TestSorted=gh,zz,abcd",
                @"--TestLong=197",
           //     @"--TestList2=a,b,c"     
            };

            _ = new Dummy();

            var config = BaseConfig.LoadAll<MyConfig>(null, args.ToArray());
            Assert.IsTrue(config.TestLong == 197 && config.TestDate == new DateTime(2020, 05, 02));
        }

        [Test]
        public void TestDisplayVersion()
        {
            var args = new List<string>()
            {
                @"--Version=All"
           //     @"--TestList2=a,b,c"     
            };
            //test display version
            BaseConfig.LoadAll<MyConfig>(null, args.ToArray());



        }

        [Test]
        public void TestNoConfigNoOptions()
        {
            var config = BaseConfig.LoadAll<MyConfig>(null, null);
            Console.WriteLine(config);
        }

        [Test]
        public void TestEnvVar()
        {
            Environment.SetEnvironmentVariable("MYCONFIG_MYCOLOR", "Blue", EnvironmentVariableTarget.Process);
            var config = BaseConfig.LoadAll<MyConfig>(null, null);
            Console.WriteLine(config);
        }

        [Test]
        public void TestHelp()
        {
            var args = new List<string>()
            {
                @"--Help"
           //     @"--TestList2=a,b,c"     
            };
            BaseConfig.LoadAll<ExampleConfig>(null, args.ToArray());
        }

        [Test]
        public void TestExample()
        {
            var args = new List<string>()
            {
                @"--Help"
           //     @"--TestList2=a,b,c"     
            };
            var finalConfig = BaseConfig.LoadAll<ExampleConfig>("exampleConfig.json", args.ToArray());
            Console.WriteLine(finalConfig);

            // Just o display an example of --Version output
            args = new List<string>()
            {
                @"--Version"
           //     @"--TestList2=a,b,c"     
            };
            BaseConfig.LoadAll<ExampleConfig>(null, args.ToArray());
        }

        [Test]
        public void TestException()
        {
            Assert.Throws<JsonSerializationException>(() => BaseConfig.LoadAll<ExampleConfig>("errorConfig.json"));
        }
    }
}
