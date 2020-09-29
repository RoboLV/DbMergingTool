using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SQLMerger.Cache;
using SQLMerger.Config;
using SQLMerger.Handlers;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Interpreter
{
    public class TableInterpreter : IInterpreter
    {
        private Table table;
        private int order;

        public bool Interpret(object data, int ID)
        {
            var lines = (List<string>) data;
            table = new Table {ID = ID};
            order = 0;

            // First Line - Table Name
            var words = lines[0].Split(" ");
            table.Name = Helper.RemoveTags(words[^2]);


            var doSpamUserCheck = (MergingController.Config.Files[ID].Tables != null &&
                                    MergingController.Config.Files[ID].Tables.ContainsKey(table.Name) &&
                                    MergingController.Config.Files[ID].Tables[table.Name].CustomHandler != null &&
                                    MergingController.Config.Files[ID].Tables[table.Name].CustomHandler
                                        .Contains("SpamUsers"));

            if (MergingController.Config.Files[ID].KeepTables != null)
            {
                if (!MergingController.Config.Files[ID].KeepTables.Contains(table.Name))
                    return false;
            }
            else if (MergingController.Config.KeepTables != null)
            {
                if(!MergingController.Config.KeepTables.Contains(table.Name))
                    return false;
            }else if (MergingController.Config.IgnoreTables != null)
            {
                if(MergingController.Config.IgnoreTables.Contains(table.Name))
                    return false;
            }else if (ID != 0 && MergingController.Config.Cache != null && MergingController.Config.Cache.On)
            {
                if (CacheManager.TablesToRemove.Contains(table.Name))
                    return false;
            }
            
            lines.RemoveAt(0);

            Console.WriteLine($"-- Making table ({ID}): {table.Name}");

            var id = 0;
            foreach (var line in lines)
            {
                id++;
                var ln = line.Trim();

                if (ln.StartsWith(") ENGINE="))
                {
                    table.Ending = line;
                    break;
                }

                // Remove comma
                if(ln[^1] == ',')
                    ln = ln.Substring(0, ln.Length - 1);

                if (ln.StartsWith("PRIMARY KEY "))
                {
                    var w = ln.Split(" ");
                    //Console.WriteLine(w[^1]);
                    table.PrimaryKey = Helper.RemoveTags(w[^1]).Split(',');
                    for(var i = 0; i < table.PrimaryKey.Length; i++)
                    {
                        table.PrimaryKey[i] = Helper.RemoveTags(table.PrimaryKey[i]);
                    }

                    if (table.PrimaryKey.Length > 1)
                    {
                        Console.WriteLine("----Multiple-PK-detected");
                    }
                    continue;
                }
                if (ln.StartsWith("UNIQUE KEY "))
                {
                    var w = ln.Split(" ");
                    table.UniqueKey[Helper.RemoveTags(w[2])] = Helper.SplitMultipleKeys(Helper.RemoveTags((w[^1])));
                    continue;
                }
                if (ln.StartsWith("FULLTEXT KEY"))
                {
                    var w = ln.Split(" ");
                    table.FullTextKey[Helper.RemoveTags(w[2])] = Helper.SplitMultipleKeys(Helper.RemoveTags((w[^1])));
                    continue;
                }
                if (ln.StartsWith("KEY "))
                {
                    var w = ln.Split(" ");
                    //Console.WriteLine(w[^1]);
                    table.Keys[Helper.RemoveTags(w[1])] = Helper.RemoveTags(Helper.RemoveTags((w[^1])));
                    continue;
                }
                if(ln.StartsWith("CONSTRAINT "))
                {
                    var w = ln.Split(" ");
                    var key = w[1];
                    if (w[2] == "FOREIGN" && w[3] == "KEY")
                    {
                        var fk = new FKRef
                        {
                            Title = Helper.RemoveTags(w[1]),
                            Table = Helper.RemoveTags(w[6]),
                            Column = Helper.RemoveTags(Helper.RemoveTags(w[7])),
                            Action = string.Join(' ', w.ToList().GetRange(8, w.Length - 8))
                        };
                        table.ForeignKeys[Helper.RemoveTags(Helper.RemoveTags(w[4]))] = fk;
                    }
                    else
                    {
                        Console.WriteLine("NOT FK?");
                        throw new Exception($"Unknown key: {ln}");
                    }
                    continue;
                }


                var column = GetColumn(ln);
                table.Columns.Add(column.Name, column);
            }

            if (id < lines.Count)
            {
                var it = new InsertInterpreter {table = table};
                for (var i = id; i < lines.Count; i++)
                {
                    it.Interpret(lines[i], ID);
                    if (doSpamUserCheck)
                    {
                        var insert = (Insert) it.GetData();
                        SpamUsers.Run(insert, ID);
                        table.Inserts.Add(insert);
                    }
                    else
                    {
                        table.Inserts.Add((Insert)it.GetData());
                    }
                }
            }

            return true;
        }

        public Column GetColumn(string line)
        {
            var column = new Column();

            // Splits into words
            var words = line.Split(" ").ToList();

            // Gets name
            column.Name = Helper.RemoveTags(words[0]);
            column.Order = order++;
            column.DataType = words[1];

            if (words.Count <= 2)
                return column;

            if (words[2] == "unsigned")
                column.DataType += " unsigned";

            // Gets default value
            if (line.IndexOf("COMMENT") >= 0)
            {
                var p = words.IndexOf("COMMENT");
                var subList = words.GetRange(p + 1, words.Count - p - 1);
                var desc = string.Join(" ", subList);
                if (desc == "")
                    Console.WriteLine("EMPTY");
                else
                    column.Description = Helper.RemoveTags(desc);
            }

            // Adds required
            column.IsRequired = line.IndexOf("NOT NULL") >= 0;

            // Adds default
            if (line.IndexOf("DEFAULT") >= 0)
            {
                var p = words.IndexOf("DEFAULT");
                if (words[p + 1][0] != '\'')
                {
                    column.Default = words[p + 1];
                }
                else if (words[p + 1][^1] == '\'')
                {
                    column.Default = Helper.RemoveTags(words[p + 1]);
                    if (column.Default == "")
                        column.Default = "@@Empty@@";
                }
                else
                {
                    var e = p + 1;
                    for (; e < words.Count; e++)
                    {
                        if(words[e][^1] == '\'')
                            break;
                    }
                    var subList = words.GetRange(p + 1, e - p);
                    var def = string.Join(" ", subList);
                    if(def == "")
                        Console.WriteLine("EMPTY");
                    column.Default = def;
                }
            }

            // Adds on update
            if (line.IndexOf("ON UPDATE") >= 0)
            {
                var p = words.IndexOf("UPDATE");
                if (words[p - 1] == "ON")
                    column.OnUpdate = words[p + 1];
                else
                    column.OnUpdate = "CURRENT_TIMESTAMP";
            }

            return column;
        }

        public object GetData()
        {
            return table;
        }
    }
}
