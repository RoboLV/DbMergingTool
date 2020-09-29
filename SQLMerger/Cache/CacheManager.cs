using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Cache
{
    public static class CacheManager
    {
        public static List<string> TablesToRemove = new List<string>();

        public static void BuildTablesToRemove(CacheConfig config)
        {
            TablesToRemove = config.IgnoreTablesWhenOn.Except(config.Tables.Keys).ToList();
        }

        /**
         * Moved to Table Interpreter
         */
        //public static void CleanUp(List<FileInstance> files)
        //{
        //    for (var f = 1; f < files.Count; f++)
        //    {
        //        foreach (var tableToRemove in TablesToRemove)
        //        {
        //            files[f].Tables.Remove(tableToRemove);
        //        }
        //    }
        //}

        public static void Build(List<FileInstance> files, CacheConfig config)
        {
            // 1. Create hash table on column names
            var hashTable = new Dictionary<string, Dictionary<string, HashValue>>();
            var columnCacheIDs = new Dictionary<string, List<int>>();

            foreach (var table in config.Tables)
            {
                hashTable.Add(table.Key, new Dictionary<string, HashValue>());

                columnCacheIDs.Add(table.Key, new List<int>());
                foreach (var columnName in table.Value)
                    columnCacheIDs[table.Key].Add(files[0].Tables[table.Key].GetColumnId(columnName));

                var i = 0;
                foreach (var insert in files[0].Tables[table.Key].Inserts)
                {
                    for (var r = 0; r < insert.Rows.Count; r++)
                    {
                        var hashName = new StringBuilder();
                        hashName.Append(table.Key + "::");
                        foreach (var columnId in columnCacheIDs[table.Key])
                            hashName.Append(insert.Rows[r][columnId].ToLower() + "?/$__::__$/?");

                        hashTable[table.Key].Add(hashName.ToString(), new HashValue
                        {
                            RowId = r,
                            FileId = 0,
                            InsertId = i
                        });
                    }

                    i++;
                }
            }

            // 2. Bind into
            for (var f = 1; f < files.Count; f++)
            {
                foreach (var table in config.Tables)
                {
                    var pkId = files[f].Tables[table.Key].GetColumnId(files[f].Tables[table.Key].PrimaryKey[0]);
                    foreach (var insert in files[f].Tables[table.Key].Inserts)
                    {
                        for (var r = 0; r < insert.Rows.Count; r++)
                        {
                            var hashName = new StringBuilder();
                            hashName.Append(table.Key + "::");
                            foreach (var columnId in columnCacheIDs[table.Key])
                                hashName.Append(insert.Rows[r][columnId].ToLower() + "?/$__::__$/?");

                            var hashNameStr = hashName.ToString();
                            if (hashTable[table.Key].ContainsKey(hashNameStr))
                            {
                                var target = hashTable[table.Key][hashNameStr];
                                // Bind
                                Register.Registers[f].UpdatePk(table.Key, insert.Rows[r][pkId],
                                    files[0].Tables[table.Key].Inserts[target.InsertId].Rows[target.RowId][pkId]);
                            }
                            else
                            {
                                Register.Registers[f].AddToBlackBox(table.Key, insert.Rows[r][pkId]);
                            }

                            // Remove
                            insert.Rows.RemoveAt(r);
                            r--;
                        }
                    }

                    files[f].Tables.Remove(table.Key);
                }
            }
        }
    }
}
