using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class MoveData
    {
        public static void Move(List<MoveDataConfig> configs, FileInstance file, Table table)
        {
            Console.WriteLine($"--||-- Data move in table: {table.Name}");


            foreach (var config in configs)
            {
                var targetTable = file.Tables[config.To];
                var columnId = table.GetColumnId(config.Where.Column);
                var stripColumnIds = new List<int>();
                foreach (var columnName in config.StripTagsFor)
                {
                    stripColumnIds.Add(table.GetColumnId(columnName));   
                }

                if (columnId == -1) continue;

                foreach (var insert in table.Inserts)
                {
                    var moveInsert = new Insert
                    {
                        ID = table.ID,
                        Table = targetTable.Name,
                        Rows = new List<List<string>>()
                    };

                    for (var r = 0; r < insert.Rows.Count; r++)
                    {
                        if (insert.Rows[r][columnId] != config.Where.Value) continue;

                        foreach (var stripId in stripColumnIds)
                        {
                            if(insert.Rows[r][stripId].Length >= 2 && insert.Rows[r][stripId] != "NULL")
                                insert.Rows[r][stripId] = Helper.RemoveTags(insert.Rows[r][stripId]);
                        }

                        moveInsert.Rows.Add(insert.Rows[r]);
                        insert.Rows.RemoveAt(r);
                        r--;
                    }

                    if (moveInsert.Rows.Count > 0)
                    {
                        targetTable.Inserts.Add(moveInsert);
                        targetTable.AddedCopy = true;
                    }

                    Console.WriteLine($"--||--||-- Moved {moveInsert.Rows.Count} rows");
                }
            }

        }
    }
}
