using System;
using ConfigHandler;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var config = BaseConfig.LoadAll<ExampleConfig>("", args);
                Console.WriteLine($"Config:\n{config}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
