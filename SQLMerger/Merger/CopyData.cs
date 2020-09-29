using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class CopyData
    {

        public static void Copy(List<CopyDataConfig> configs, Table table)
        {
            Console.WriteLine($"--||-- Data copy in table: {table.Name}");

            var copyInserts = new List<Insert>();

            // TODO: Optimization instead of c * i * r do i * r + c, by checking value of r
            foreach (var config in configs)
            {
                var columnId = table.GetColumnId(config.Where.Column);
                if(columnId == -1) continue;
                
                foreach (var insert in table.Inserts)
                {
                    var copyInsert = new Insert
                    {
                        ID = table.ID,
                        Table = table.Name,
                        Rows = new List<List<string>>()
                    };

                    foreach (var row in insert.Rows)
                    {
                        if(row[columnId] != config.Where.Value) continue;
                        copyInsert.Rows.Add(GetCopyRow(table, config.Change, row));
                    }

                    // With this approach all copy data is added to end of inserts, meaning that if main data
                    // already contains same information then value will be skipped when using OnSame: BindToThis(3)
                    if (copyInsert.Rows.Count > 0)
                        copyInserts.Add(copyInsert);

                    Console.WriteLine($"--||--||-- Copied {copyInsert.Rows.Count} rows");
                }
            }

            if (copyInserts.Count > 0)
            {
                table.Inserts.AddRange(copyInserts);
                table.AddedCopy = true;
            }

        }

        private static List<string> GetCopyRow(Table table, Dictionary<string, Dictionary<string, string>> changeConfig,
            List<string> row)
        {
            var copy = new List<string>();
            for (var c = 0; c < row.Count; c++)
                copy.Add(row[c]);

            foreach (var config in changeConfig)
            {
                var colId = table.GetColumnId(config.Key);
                if(colId == -1 || !config.Value.ContainsKey(copy[colId])) continue;
                copy[colId] = config.Value[copy[colId]];
            }

            return copy;
        }
    }
}
