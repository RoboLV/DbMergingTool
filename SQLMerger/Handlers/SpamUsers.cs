using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Handlers
{
    public struct SpamUserLogData
    {
        public int store_id { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string reason_why_removed { get; set; }
    }

    public static class SpamUsers
    {
        public static Dictionary<string, SpamUserLogData> LogData = new Dictionary<string, SpamUserLogData>();

        private static readonly string[] spamEmailList = {
            "@qq.com",
            "@163.com",
            "@126.com",
            "@comcast.com",
            "@xcfc.com",
            "@dfl.com.cn",
            "@mailermails.info",
            "@mailmefast.info",
            "@mailstome.today",
            "@eemail.info",
            "@internb.com"
        };

        private static string reason;

        public static void Run(Insert insert, int id)
        {
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
            for (var i = 0; i < insert.Rows.Count; i++)
            {
                var row = insert.Rows[i];
                if (IsInSpamList(row[2]) || NameContainsLink(row[12]) || NameContainsLink(row[14]) ||
                    IsJamesSmith(row[12], row[14]) || NameIsSpam(row[12]))
                {
                    Register.Registers[id].AddToBlackBox(insert.Table, row[0]);
                    Console.WriteLine($"-- Added user ({row[2]}) to black box because spam: {row[12]} {row[14]}");
                    LogUser(row);
                    insert.Rows.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void LogUser(IReadOnlyList<string> row)
        {
            var email = Helper.RemoveTags(row[2]);
            if(LogData.ContainsKey(email)) return;

            LogData.Add(email, new SpamUserLogData
            {
                name = Helper.RemoveTags(row[12]),
                surname = Helper.RemoveTags(row[14]),
                reason_why_removed = reason,
                store_id = int.Parse(row[5])
            });
        }

        public static void SaveLog()
        {
            if(string.IsNullOrEmpty( MergingController.Config.LogDir ))
                return;

            var data = JsonSerializer.Serialize(LogData);
            File.WriteAllText(MergingController.Config.LogDir + "\\spam_user_log.json", data);

            LogData.Clear();
        }

        private static bool IsInSpamList(string email)
        {
            foreach (var spamEmailPattern in spamEmailList)
            {
                if (email.EndsWith(spamEmailPattern + "'"))
                {
                    reason = $"Email in spam pattern directory: {spamEmailPattern}";
                    return true;
                }
            }

            return false;
        }

        private static bool NameContainsLink(string name)
        {
            if (!name.Contains("http://") && !name.Contains("https://")) return false;
            reason = $"Name contains link: {name}";
            return true;
        }

        private static bool IsJamesSmith(string name, string surname)
        {
            if (name != "'James'" || surname != "'Smith'") return false;
            reason = $"Spam name surname: {name} {surname}";
            return true;
        }

        private static bool NameIsSpam(string name)
        {
            short flagCount = 0;
            
            // Large name
            if (name.Length > 250)
                flagCount++;

            // Name with many numbers
            short numCount = 0;
            for(var i = 0; i < 10; i++)
                if (name.Contains("" + i))
                    numCount++;
            if (numCount >= 4)
                flagCount++;

            // Name with chinese/japanese/korean characters
            if (Regex.Match(name, @"/[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\uff66-\uff9f]/").Success)
                flagCount++;

            if (flagCount <= 1) return false;

            reason = $"Name - {name}, tripped {flagCount} spam flags.";
            return true;
        }
    }
}
