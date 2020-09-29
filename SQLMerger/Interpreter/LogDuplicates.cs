using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using SQLMerger.Merger;

namespace SQLMerger.Interpreter
{
    public static class LogDuplicates
    {
        public static Dictionary<string, List<string>> Register { get; set; } = new Dictionary<string, List<string>>();

        public static void Log(string file, string msg)
        {
            if(!Register.ContainsKey(file))
                Register.Add(file, new List<string>());

            Register[file].Add(msg);
        }

        public static void Save(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var file in Register)
            {
                var data = JsonSerializer.Serialize(file.Value);
                File.WriteAllText( $"{path}\\log-{file.Key}.json", data);
            }
        }

    }
}
