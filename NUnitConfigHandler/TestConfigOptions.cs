using System;
using System.Collections.Generic;
using System.IO;
using ConfigHandler;
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

        // Rem: place [System.ComponentModel.DefaultValueAttribute(true)] to force default value (enum)
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new JsonSerializerSettings()
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        private static void SetDefaultJsonConfig()
        {
            JsonConvert.DefaultSettings = () => DefaultJsonSerializerSettings;
        }

        [SetUp]
        public void Setup()
        {
            SetDefaultJsonConfig();
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
            var config = BaseConfig.LoadAll<MyConfig>(null, args.ToArray());
            Assert.IsTrue(config.TestLong == 197 && config.TestDate == new DateTime(2020, 05, 02));
        }
    }
}
