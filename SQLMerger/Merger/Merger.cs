using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class Merger
    {
        public static void Merge(ref FileInstance a, FileInstance b)
        {
            // First RUN
            if (a.Tables.Count == 0)
            {
                a.Tables = b.Tables;
                return;
            }

            foreach (var table in b.Tables)
            {
                // Table Exists
                if (a.Tables.ContainsKey(table.Key))
                {
                    foreach (var column in table.Value.Columns)
                    {
                        // Column Exists
                        if (a.Tables[table.Key].Columns.ContainsKey(column.Key))
                        {
                            // If columns are equal (do nothing) -> skip
                            if (CompareColumns(a.Tables[table.Key].Columns[column.Key], column.Value))
                            {
                                continue;
                            }
                            Logger.LogErrorMessage($"Merging error: Columns Differ - ${column.Key}");
                        }
                        // Column Does NOT Exist
                        else
                        {
                            a.Tables[table.Key].Columns.Add(column.Key, column.Value);
                            a.Tables[table.Key].Columns[column.Key].Order = a.Tables[table.Key].Columns.Count - 1;
                            // TODO: Fix Inserts
                        }
                    }
                    // a.Tables[table.Key].Inserts.AddRange(table.Value.Inserts);
                }

                // Table Does NOT Exist
                else
                {
                    // TODO: Fix order
                    a.Tables.Add(table.Key, table.Value);
                }
            }

        }

        private static bool CompareColumns(Column a, Column b)
        {
            if (string.Compare(a.Name, b.Name) != 0)
                return false;

            if (string.Compare(a.DataType, b.DataType) != 0)
            {
                if (!ResolveDataTypes(a, b))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ResolveDataTypes(Column a, Column b)
        {
            if (a.DataType.StartsWith("varchar"))
            {
                if (b.DataType == "text")
                {
                    a.DataType = b.DataType;
                    return true;
                }
            }
            else if (b.DataType.StartsWith("varchar"))
            {
                if (a.DataType == "text")
                {
                    b.DataType = a.DataType;
                    return true;
                }
            }

            return false;
        }
    }
}
