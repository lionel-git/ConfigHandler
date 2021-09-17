using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#pragma warning disable CS1591

namespace ConfigHandler
{
    public class FileData
    {
        public string FullPath { get; }

        public string Content { get; }

        public FileData(string path)
        {
            if (Path.IsPathRooted(path))
                FullPath = path;
            else
                FullPath = Path.Combine(Environment.CurrentDirectory, path);
            Content = File.ReadAllText(FullPath);
        }
    }
}
