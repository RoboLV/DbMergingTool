using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Config;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class ApplyConfigFile
    {
        public static void Apply(FileConfig configLocal, FileInstance file, Config.Config configGlobal)
        {
            var configTables = new[]
            {
                configGlobal.Tables, configLocal.Tables
            };

            foreach (var tables in configTables)
            {
                if(tables == null)
                    continue;

                foreach (var configTable in tables)
                {
                    if (!file.Tables.ContainsKey(configTable.Key))
                    {
                        Logger.LogErrorMessage(
                            $"Table: {configTable.Key} not found, while applying file level configuration");
                        continue;
                    }

                    var table = file.Tables[configTable.Key];


                    if (!string.IsNullOrEmpty(configTable.Value.NewName))
                    {
                        table.NewName = configTable.Value.NewName;
                    }

                    if (configTable.Value.IgnoreColumns != null)
                    {
                        ApplyIgnoreColumn(configTable.Value, table);
                    }

                    if (configTable.Value.NewColumns != null)
                    {
                        ApplyNewColumn(configTable.Value, table);
                    }

                    if (configTable.Value.Columns != null)
                    {
                        foreach (var column in configTable.Value.Columns)
                        {
                            if (!table.Columns.ContainsKey(column.Key))
                            {
                                Logger.LogErrorMessage(
                                    $"Column: {column.Key} not found in table {configTable.Key}, while applying file level configuration");
                                continue;
                            }

                            ApplyChange(configTable.Value, table, table.Columns[column.Key], file);
                        }
                    }

                    if (configTable.Value.DropColumns != null)
                    {
                        ApplyDropColumn(configTable.Value, table);
                    }

                    if (!string.IsNullOrEmpty(configTable.Value.ForceName))
                    {
                        foreach (var insert in table.Inserts)
                        {
                            insert.Table = configTable.Value.ForceName;
                        }
                        file.Tables.Add(configTable.Value.ForceName, table);
                        file.Tables.Remove(table.Name);
                        table.Name = configTable.Value.ForceName;
                    }

                }
            }
        }

        private static void ApplyNewColumn(ConfigTable config, Table table)
        {

        }

        private static void ApplyIgnoreColumn(ConfigTable config, Table table)
        {
            foreach (var columnName in config.DropColumns)
            {
                if (!table.Columns.ContainsKey(columnName))
                {
                    Logger.LogErrorMessage(
                        $"While dropping column; Column: {columnName} not found in table {table.Name}, while applying file level configuration");
                    continue;
                }

                var id = table.Columns[columnName].Order;
                foreach (var insert in table.Inserts)
                {
                    foreach (var row in insert.Rows)
                    {
                        row[id] = table.Columns[columnName].Default ?? "";
                    }
                }
            }
        }

        private static void ApplyDropColumn(ConfigTable config, Table table)
        {
            foreach (var columnName in config.DropColumns)
            {
                var id = table.GetColumnId(columnName);
                if (id == -1)
                {
                    Logger.LogErrorMessage(
                        $"While dropping column; Column: {columnName} not found in table {table.Name}, while applying file level configuration");
                    continue;
                }

                foreach (var insert in table.Inserts)
                    foreach (var row in insert.Rows)
                        row.RemoveAt(id);

                table.Columns.Remove(columnName);
            }
        }

        private static void ApplyValueChange(ConfigTable config, Table table, Column column)
        {
            foreach (var change in config.Columns[column.Name].Change)
            {
                var id = table.GetColumnId(column.Name);
                foreach (var insert in table.Inserts)
                {
                    foreach (var row in insert.Rows)
                    {
                        if (row[id] == change.Key)
                            row[id] = change.Value;
                    }
                }
            }
        }

        private static void ApplyValueChangeWhere(ConfigTable config, Table table, Column column)
        {
            foreach (var change in config.Columns[column.Name].ChangeWhere)
            {
                var columnCheckId = table.GetColumnId(change.Where.Column);
                var id = table.GetColumnId(column.Name);
                var count = 0;

                foreach (var insert in table.Inserts)
                {
                    foreach (var row in insert.Rows)
                    {
                        if (change.Values.ContainsKey(row[id]) && row[columnCheckId] == change.Where.Value)
                        {
                            row[id] = change.Values[row[id]];
                            count++;
                        }
                    }
                }

                Console.WriteLine($"--- Change: (where {change.Where.Column} equals {change.Where.Value}) changed {count} rows");

                //foreach (var value in change.Values)
                //{
                //    foreach (var insert in table.Inserts)
                //    {
                //        foreach (var row in insert.Rows)
                //        {
                //            if (row[id] == value.Key && row[columnCheckId] == change.Where.Value)
                //            {
                //                row[id] = value.Value;
                //                count++;
                //            }
                //        }
                //    }
                //}
            }
        }

        private static void ApplyChange(ConfigTable config, Table table, Column column, FileInstance file)
        {
            var columnConfig = config.Columns[column.Name];

            if (columnConfig.Change != null)
            {
                ApplyValueChange(config, table, column);
            }

            if (columnConfig.ChangeWhere != null && columnConfig.ChangeWhere.Count > 0)
            {
                ApplyValueChangeWhere(config, table, column);
            }

            //column.IsLikable = columnConfig.IsLikable;

            if (!string.IsNullOrEmpty(columnConfig.Default))
            {
                column.Default = columnConfig.Default;
            }

            if (!string.IsNullOrEmpty(columnConfig.DataType))
            {
                column.DataType = columnConfig.DataType;
            }

            if (!string.IsNullOrEmpty(columnConfig.NewName))
            {
                column.NewName = columnConfig.NewName;
            }
        }
    }
}
