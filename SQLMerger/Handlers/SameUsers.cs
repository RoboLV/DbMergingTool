using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Handlers
{

    public static class SameUsers
    {
        private static Dictionary<string, int> LogData = new Dictionary<string, int>();

        private static Dictionary<string, string> hashData = new Dictionary<string, string>();

        private const string TABLE_NAME = "customer_entity";
        /*
         * 0 - entity_id
         * 1 - website_id
         * 2 - email
         * 3 - group_id
         * 4 - increment_id
         * 5 - store_id
         * 6 - created_at
         * 7 - updated_at
         * 8 - is_active
         * 9 - disable_auto_group_change
         * 10 - created_at
         * 11 - prefix
         * 12 - firstname
         * 13 - middlename
         * 14 - lastname
         */
        private static string GetHashName(IReadOnlyList<string> row) => 
            "@@#hash_user::" + row[1] + "$$@@$$::" + row[2].ToLower();

        private static void BuildFirstHash(FileInstance headFile)
        {
            foreach (var insert in headFile.Tables[TABLE_NAME].Inserts)
            {
                foreach (var row in insert.Rows)
                {
                    hashData.Add(GetHashName(row), row[0]);
                }
            }
        }

        public static bool Run(FileInstance outputFile, List<FileInstance> rawFiles, bool firstTime = false)
        {
            if(!outputFile.Tables.ContainsKey(TABLE_NAME))
                return false;

            var output = false;
            hashData.Clear();

            // TODO: Add support for multiple files 
            var start = 1;
            var end = rawFiles.Count;
            if (firstTime)
            {
                start = 1;
                end = 2;
            }
            else
            {
                BuildFirstHash(outputFile);
            }

            for (var f = start; f < end; f++)
            {
                foreach (var insert in rawFiles[f].Tables[TABLE_NAME].Inserts)
                {
                    for (var r = 0; r < insert.Rows.Count; r++)
                    {
                        var key = GetHashName(insert.Rows[r]);
                        if (hashData.ContainsKey(key))
                        {
                            // Merge
                            Register.Registers[f].UpdatePk(TABLE_NAME, insert.Rows[r][0], hashData[key]);
                            Console.WriteLine($"--||-- Merging same user {insert.Rows[r][2]}");

                            var email = Helper.RemoveTags(insert.Rows[r][2]);
                            if (!LogData.ContainsKey(email))
                                LogData.Add(email, int.Parse(insert.Rows[r][5]));

                            insert.Rows.RemoveAt(r);
                            r--;
                            output = true;
                        }
                        else
                        {
                            hashData.Add(key, insert.Rows[r][0]);
                        }
                    }
                }
            }

            return output;
        }

        public static void SaveLog()
        {
            if (string.IsNullOrEmpty(MergingController.Config.LogDir))
                return;

            var data = JsonSerializer.Serialize(LogData);
            File.WriteAllText(MergingController.Config.LogDir + "\\merged_user_log.json", data);

            LogData.Clear();
        }
    }
}
