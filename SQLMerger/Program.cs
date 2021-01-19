using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SQLMerger.Interpreter;
using SQLMerger.Merger;

namespace SQLMerger
{
    class Program
    {
        static Config.Config LoadConfigFile(string path)
        {
            var jsonString = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Config.Config>(jsonString);
        }

        static void Main(string[] args)
        {
            var filePath = "";
            if (args.Length > 0)
            {
                filePath = args[0];
                Console.WriteLine("Using " + filePath);
            } else
            {
                Console.WriteLine("Enter config.json file path:");
                filePath = Console.ReadLine();
            }
            // Console.WriteLine("Enter config.json file path:");
            //var filePath = Console.ReadLine(); //@"C:\Users\denri\Documents\Merging\DEV\config.json";
            //var filePath = @"C:\Users\denri\Documents\Merging\DEV\config_ee_fi.json";
            //var filePath = @"E:\Migration\SPORT\confi.bug.json";
            //var filePath = @"E:\Migration\XSTOYS\Merging\DEV\config_ee_fi.json";

            var config = LoadConfigFile(filePath);

            var controller = new MergingController(config);
            controller.Init();
            controller.Merge();

            //LogDuplicates.Save(@"E:\Migration\SPORT\duplicate-logs");
        }
    }
}
