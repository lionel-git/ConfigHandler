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
        private const string file1 = "TempConfig1.json";
        private const string file2 = "TempConfig2.json";
        private const string file3 = "TempConfig3.json";

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
                //args.Add(@"--TestList2=a,b,c");          
            };

            var config = new MyConfig();
            config.MyColor = Color.Red;
            config.MyColors.Add(Color.Red);
            config.MyColors.Add(Color.Blue);

            config.ColorMeaning.Add(Color.Red, "Angry");
            config.ColorMeaning.Add(Color.Green, "Happy");

            config.TestLong = 17;

            config.Save(file1);
            var config2 = BaseConfig.LoadAll<MyConfig>(file1, args.ToArray());
            Assert.AreEqual(config2.TestLong, 197);

            config2.ConfigFile = null; // Load will set config file
            config2.Save(file2);

            var config3 = BaseConfig.LoadAll<MyConfig>(file2);
            config3.ConfigFile = null; // Load will set config file
            config3.Save(file3);


            var content2 = File.ReadAllText(file2);
            var content3 = File.ReadAllText(file3);

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
    }
}
