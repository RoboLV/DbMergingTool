using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;
using Exception = System.Exception;

namespace SQLMerger.Merger
{
    public static class Register
    {
        public static List<FileRegister> Registers { get; set; } = new List<FileRegister>();
    }

    public class FileRegister
    {
        // Table > OriginalID > NewID
        public Dictionary<string, Dictionary<string, string>> PrimaryKeys { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        // Table > Field > Target Table
        /*
         * Table Name:
         * - Fields: Target
         */
        public Dictionary<string, Dictionary<string, string>> ForeignKeys { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        public Dictionary<string, Dictionary<string, Dictionary<string,FkRuleConfig>>> ForeignKeysRule { get; set; } = new Dictionary<string, Dictionary<string, Dictionary<string, FkRuleConfig>>>();


        // Table ID
        public Dictionary<string, List<string>> BlackBox { get; set; } = new Dictionary<string, List<string>>();

        public void RefreshPk()
        {
            var tmp = new Dictionary<string, Dictionary<string, string>>();
            foreach (var table in PrimaryKeys)
            {
                tmp.Add(table.Key, new Dictionary<string, string>());
                foreach (var ids in table.Value)
                {
                    if(!tmp[table.Key].ContainsKey(ids.Value))
                        tmp[table.Key].Add(ids.Value, ids.Value);
                }
            }
            PrimaryKeys = tmp;
        }

        public void AddToBlackBox(string table, string id)
        {
            if (!BlackBox.ContainsKey(table))
                BlackBox.Add(table, new List<string>());
            BlackBox[table].Add(id);
            if (PrimaryKeys.ContainsKey(table) && PrimaryKeys[table].ContainsKey(id))
            {
                PrimaryKeys[table].Remove(id);
            }
        }

        public void AddFkRule(string table, string column, FkRuleConfig rule)
        {
            if (!ForeignKeysRule.ContainsKey(table))
                ForeignKeysRule.Add(table, new Dictionary<string, Dictionary<string, FkRuleConfig>>());
            if (!ForeignKeysRule[table].ContainsKey(column))
            {
                ForeignKeysRule[table].Add(column, new Dictionary<string, FkRuleConfig>());
            }
            ForeignKeysRule[table][column].Add(rule.Rule.Value, rule);
        }

        public bool InBlackBox(string table, string id)
        {
            return BlackBox.ContainsKey(table) && BlackBox[table].Contains(id);
        }

        public void AddPK(string table, string originalId)
        {
            if (!PrimaryKeys.ContainsKey(table))
                PrimaryKeys.Add(table, new Dictionary<string, string>());

            if(!PrimaryKeys[table].ContainsKey(originalId))
                PrimaryKeys[table].Add(originalId, originalId);
        }

        public void UpdatePk(string table, string originalId, string newId)
        {
            AddPK(table, originalId);
            PrimaryKeys[table][originalId] = newId;
        }

        public void AddFK(string table, string field, string targetTable)
        {
            if (!ForeignKeys.ContainsKey(table))
                ForeignKeys.Add(table, new Dictionary<string, string>());

            if (!ForeignKeys[table].ContainsKey(field))
                ForeignKeys[table].Add(field, targetTable);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="field"></param>
        /// <param name="originalId"></param>
        /// <returns>new PK of param teble</returns>
        public string GetPK(string table, string field, string originalId)
        {
            var targetTable = ForeignKeys[table][field];
            return PrimaryKeys[targetTable][originalId];
        }

        public string GetPkRule(string table, string field, string value, string originalId)
        {
            var targetTable = ForeignKeysRule[table][field][value].ForeignKey.TargetTable;
            return PrimaryKeys[targetTable][originalId];
        }
    }
}
