using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class ForceMerger
    {
        public static bool Merge(Table table, List<ForceMerge> configForceMerges)
        {
            if(table.PrimaryKey.Length != 1) return false;

            var didMerge = false;
            var pkId = table.GetColumnId(table.PrimaryKey[0]);
            foreach (var config in configForceMerges)
            {
                var isDone = false;
                foreach (var insert  in table.Inserts)
                {
                    for(var r = 0; r < insert.Rows.Count; r++)
                    {
                        if (insert.Rows[r][pkId] != config.IdSource) continue;

                        Register.Registers[table.ID].UpdatePk(table.Name, insert.Rows[r][pkId], config.IdTarget);
                        insert.Rows.RemoveAt(r);
                        isDone = true;
                        didMerge = true;
                        break;
                    }

                    if (isDone) break;
                }
            }

            return didMerge;
        }
    }
}
