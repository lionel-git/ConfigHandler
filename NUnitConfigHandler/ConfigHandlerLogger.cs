using System;

namespace NUnitConfigHandler
{
    public class ConfigHandlerLogger : ConfigHandler.ILogger
    {
        public void InfoMsg(string msg)
        {
            Console.WriteLine($"INFO: {msg}");
        }

        public void WarnMsg(string msg)
        {
            Console.WriteLine($"WARN: {msg}");
        }

        public void ErrorMsg(string msg)
        {
            Console.WriteLine($"ERROR: {msg}");
        }
    }
}
