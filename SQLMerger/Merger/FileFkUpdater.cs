using System;
using System.Linq;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class FileFkUpdater
    {
        public static void UpdateFk(FileInstance file, int executionId)
        {
            var register = Register.Registers[file.Tables.First().Value.ID];
            var orgRegister = Register.Registers[0];
            foreach (var table in file.Tables)
            {
                var exceptionOnMissingFK = true;
                if (MergingController.Config.Tables.ContainsKey(table.Key))
                    exceptionOnMissingFK = MergingController.Config.Tables[table.Key].ExceptionOnMissingFk;

                // If tables has no FK references, then we can skip
                // the whole table check
                if (!register.ForeignKeys.ContainsKey(table.Key))
                    continue;

                Console.WriteLine($"- Update of FK in table: {table.Key}");


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

                    var removeIfMissing = false;
                    if (MergingController.Config.Files.Count > table.Value.ID &&
                        MergingController.Config.Files[table.Value.ID].Tables != null &&
                        MergingController.Config.Files[table.Value.ID].Tables.ContainsKey(table.Key) &&
                        MergingController.Config.Files[table.Value.ID].Tables[table.Key].Columns != null &&
                        MergingController.Config.Files[table.Value.ID].Tables[table.Key].Columns.ContainsKey(columns[c])
                    )
                        removeIfMissing = MergingController.Config.Files[table.Value.ID].Tables[table.Key]
                            .Columns[columns[c]].DropIfMissingFk;

                    // -- Inserts
                foreach (var insert in table.Value.Inserts)
                    {
                        // -- Rows
                        for (var r = 0; r < insert.Rows.Count; r++)
                        {
                            var originalId = insert.Rows[r][columnId];

                            if ((originalId != "0" || canBeZero) && originalId != "NULL")
                            {
                                try
                                {
                                    insert.Rows[r][columnId] = register.GetPK(table.Key, columns[c], originalId);
                                }
                                catch (Exception)
                                {
                                    if (exceptionOnMissingFK == false)
                                    {
                                        insert.Rows[r][columnId] = "NULL";
                                    }
                                    else
                                    {
                                        if (!removeIfMissing)
                                        {
                                            throw new Exception($"Missing FK for table {table.Value} on column {columnId} with value {originalId}!");
                                        }

                                        if (table.Value.PrimaryKey.Length == 1)
                                        {
                                            register.AddToBlackBox(table.Key, insert.Rows[r][table.Value.GetColumnId(table.Value.PrimaryKey[0])]);
                                        }
                                        insert.Rows.RemoveAt(r);
                                        r--;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void UpdateFkRule(FileInstance file, int executionId)
        {
            var register = Register.Registers[file.Tables.First().Value.ID];
            var tables = register.ForeignKeysRule.Keys.ToList();
            var orgRegister = Register.Registers[0];

            foreach (var tableName in tables)
            {
                var table = file.Tables[tableName];

                var exceptionOnMissingFK = true;
                if (MergingController.Config.Tables.ContainsKey(tableName))
                    exceptionOnMissingFK = MergingController.Config.Tables[tableName].ExceptionOnMissingFk;

                Console.WriteLine($"- Update of FK via RULE in table: {tableName}");

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
                                if (originalId != "NULL")
                                {
                                    try
                                    {
                                        insert.Rows[r][columnId] = register.GetPkRule(tableName, columns[c], frValue.Key, originalId);
                                    }
                                    catch (Exception)
                                    {
                                        if (executionId == 0)
                                        {
                                            if (exceptionOnMissingFK == false)
                                                insert.Rows[r][columnId] = "NULL";
                                            else
                                                throw new Exception("Missing FK");
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
