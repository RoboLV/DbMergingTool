using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Handlers
{
    public static class CustomAttributeOptionBuilder
    {
        private const string TABLE = "eav_attribute";
        private const string OPTION_TABLE = "eav_attribute_option";
        private const string OPTION_VALUE_TABLE = "eav_attribute_option_value";

        private static List<string> referenceRow = null;
        private static List<string> referenceRowValue = null;

        public static int LastId { get; set; } = 0;

        public static void AddLastIdReference(FileInstance baseFile,
            Dictionary<string, CustomAttributeOptionConfig> config)
        {
            if (!baseFile.Tables.ContainsKey(TABLE) ||
                !baseFile.Tables.ContainsKey(OPTION_TABLE) ||
                !baseFile.Tables.ContainsKey(OPTION_VALUE_TABLE))
                return;

            var codeColumnId = baseFile.Tables[TABLE].GetColumnId("attribute_code");
            if (codeColumnId < 0) return;

            LastId = 0;
            foreach (var customOptionAttributeConfig in config)
            {
                var configLastId = customOptionAttributeConfig.Value.Offset +
                                   customOptionAttributeConfig.Value.Map.Count + 1;
                if (LastId < configLastId)
                    LastId = configLastId;
            }

            if (LastId != 0)
            {
                referenceRow = new List<string> { LastId.ToString(), "1", "0"};
                referenceRowValue = new List<string> { LastId.ToString(), LastId.ToString(), "0", "tmp"};
                baseFile.Tables[OPTION_TABLE].Inserts[^1].Rows.Add(referenceRow);
                baseFile.Tables[OPTION_VALUE_TABLE].Inserts[^1].Rows.Add(referenceRowValue);
            }
        }

        public static void RemoveReferenceRows(FileInstance baseFile)
        {
            if (referenceRow != null)
            {
                baseFile.Tables[OPTION_TABLE].Inserts[^1].Rows.Remove(referenceRow);
                baseFile.Tables[OPTION_VALUE_TABLE].Inserts[^1].Rows.Remove(referenceRowValue);
            }
        }

        public static void Build(FileInstance baseFile, Dictionary<string, CustomAttributeOptionConfig> config)
        {
            if (!baseFile.Tables.ContainsKey(TABLE) || 
                !baseFile.Tables.ContainsKey(OPTION_TABLE) || 
                !baseFile.Tables.ContainsKey(OPTION_VALUE_TABLE)) 
                return;

            var codeColumnId = baseFile.Tables[TABLE].GetColumnId("attribute_code");
            if(codeColumnId < 0) return;


            foreach (var customOptionAttributeConfig in config)
            {
                var attributeId = FindAttribute(baseFile.Tables[TABLE], customOptionAttributeConfig.Key, codeColumnId);
                if(attributeId == -1) continue;

                var id = customOptionAttributeConfig.Value.Offset;
                var sortOrder = 0;

                baseFile.Tables[OPTION_TABLE].Inserts.Add(new Insert
                {
                    Table = OPTION_TABLE,
                    ID = 0
                });
                baseFile.Tables[OPTION_VALUE_TABLE].Inserts.Add(new Insert
                {
                    Table = OPTION_VALUE_TABLE,
                    ID = 0
                });

                foreach (var insert in customOptionAttributeConfig.Value.Map)
                {
                    id++;
                    baseFile.Tables[OPTION_TABLE].Inserts[^1].Rows.Add(new List<string>{id.ToString(), attributeId.ToString(), sortOrder.ToString()});
                    baseFile.Tables[OPTION_VALUE_TABLE].Inserts[^1].Rows.Add(new List<string> { id.ToString(), id.ToString(), "0", insert});
                    sortOrder++;
                }
            }
        }

        private static int FindAttribute(Table table, string attribute, int columnId)
        {
            foreach (var insert in table.Inserts)
            {
                foreach (var row in insert.Rows)
                {
                    if (row[columnId] == attribute)
                        return int.Parse(row[0]);
                }
            }
            return -1;
        }
    }
}
