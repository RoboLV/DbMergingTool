using System;
using System.Linq;
using System.Threading.Channels;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class FileBlackBoxRemover
    {
        public static void Cleanup(FileInstance file)
        {
            var register = Register.Registers[file.Tables.First().Value.ID];
            bool repeat;
            do
            {
                repeat = false;
                foreach (var table in file.Tables)
                {
                    var pkId = 0;
                    if (table.Value.PrimaryKey != null && table.Value.PrimaryKey.Length > 0)
                        pkId = table.Value.GetColumnId(table.Value.PrimaryKey[0]);

                    // If tables has no FK references, then we can skip
                    // the whole table check
                    if (!register.ForeignKeys.ContainsKey(table.Key))
                        continue;

                    Console.WriteLine($"- Cleanup of table: {table.Key}");

                    // Lets check all columns with FK references and
                    // remove them if they are in BB
                    var columns = register.ForeignKeys[table.Key].Keys.ToList();

                    for (var c = 0; c < columns.Count; c++)
                    {
                        // We need to get column ids by knowing their names
                        var columnId = table.Value.GetColumnId(columns[c]);

                        // Table to which reference is made
                        var targetTable = register.ForeignKeys[table.Key][columns[c]];
                        var canBeZero = false;
                        if (MergingController.Config.Tables.ContainsKey(targetTable))
                            canBeZero = MergingController.Config.Tables[targetTable].ZeroIdInTable;

                        // -- Inserts
                        foreach (var insert in table.Value.Inserts)
                        {
                            // -- Rows
                            for (var r = 0; r < insert.Rows.Count; r++)
                            {
                                var originalId = insert.Rows[r][columnId];

                                // TODO: Zero check, some tables have value as 0
                                if (originalId == "NULL" || (originalId == "0" && canBeZero == false) || !register.InBlackBox(targetTable, originalId)) 
                                    continue;

                                if (table.Value.PrimaryKey != null && table.Value.PrimaryKey.Length == 1)
                                {
                                    register.AddToBlackBox(table.Key, insert.Rows[r][pkId]);
                                }
                                insert.Rows.RemoveAt(r);
                                repeat = true;
                                r--;
                            }
                        }
                    }
                }

                var tables = register.ForeignKeysRule.Keys.ToList();
                foreach (var tableName in tables)
                {
                    var table = file.Tables[tableName];
                    var pkId = 0;
                    if (table.PrimaryKey != null && table.PrimaryKey.Length > 0)
                        pkId = table.GetColumnId(table.PrimaryKey[0]);

                    Console.WriteLine($"- Cleanup via RULE of table: {tableName}");

                    // Lets check all columns with FK references and
                    // remove them if they are in BB
                    var columns = register.ForeignKeysRule[tableName].Keys.ToList();
                    for (var c = 0; c < columns.Count; c++)
                    {
                        // We need to get column ids by knowing their names
                        var columnId = table.GetColumnId(columns[c]);

                        // -- Inserts
                        foreach (var insert in table.Inserts)
                        {
                            // -- Each value rule on columns
                            foreach (var frValue in register.ForeignKeysRule[table.Name][columns[c]])
                            {
                                var targetTable = register.ForeignKeysRule[table.Name][columns[c]][frValue.Key].ForeignKey.TargetTable;

                                // Column ID which holds value that's needed to be checked
                                var ruleColumnId = table.GetColumnId(
                                    register.ForeignKeysRule[table.Name][columns[c]][frValue.Key].Rule.Column);

                                // Value that's required to be equal to execute FK reference
                                var ruleValue = register.ForeignKeysRule[table.Name][columns[c]][frValue.Key].Rule.Value;

                                // -- Rows
                                for (var r = 0; r < insert.Rows.Count; r++)
                                {
                                    // If values are not equal -> skip
                                    if (insert.Rows[r][ruleColumnId] != ruleValue)
                                        continue;

                                    var originalId = insert.Rows[r][columnId];
                                    if (!register.InBlackBox(targetTable, originalId))
                                        continue;

                                    Console.WriteLine($"--- Removed {insert.Rows[r][pkId]}");
                                    register.AddToBlackBox(tableName, insert.Rows[r][pkId]);
                                    insert.Rows.RemoveAt(r);
                                    repeat = true;
                                    r--;
                                }
                            }
                        }
                    }
                }
            } while (repeat);
        }
    }
}
