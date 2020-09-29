using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Interpreter
{
    public class InsertInterpreter : IInterpreter
    {
        private const string TMP = "tmp01fso1432__";
        private const string BINARY = "_binary ";
        private const string BINARY_TMP = BINARY + TMP;

        private Insert insert;
        public Table table { get; set; }

        public bool Interpret(object data, int ID)
        {
            insert = new Insert();
            insert.ID = ID;
            var line = (string) data;
            //line = line.Replace(@"\'", "__&&@@quote@@&&__");

            var memory = new Dictionary<string, string>();
            ID = 0;
            var text = Regex.Replace(line, @"\'(\\.|[^\'])*\'", m =>
            {
                var key = TMP + ID++;
                memory[key] = '\'' + Helper.RemoveTags( m.ToString()).Trim() + '\'';
                return key;
            });

            var words = text.Split(' ');
            insert.Table = Helper.RemoveTags(words[2]);



            Dictionary<string, List<string>> blackBox = null;
            Dictionary<string, List<string>> whiteBox = null;

            List<string> rowAppend = null;
            if (MergingController.Config.Files[insert.ID].Tables != null && 
                MergingController.Config.Files[insert.ID].Tables.ContainsKey(insert.Table))
            {
                blackBox = MergingController.Config.Files[insert.ID].Tables[insert.Table].BlackBox;
                rowAppend = MergingController.Config.Files[insert.ID].Tables[insert.Table].RowAppend;

                whiteBox = MergingController.Config.Files[insert.ID].Tables[insert.Table].WhiteBox;
            }

            var columnIDs = new Dictionary<string, int>();
            if (blackBox != null)
            {
                foreach (var column in blackBox)
                    columnIDs[column.Key] = table.GetColumnId(column.Key);
            }

            var columnIDsWhiteBox = new Dictionary<string, int>();
            if (whiteBox != null)
            {
                foreach (var column in whiteBox)
                    columnIDsWhiteBox[column.Key] = table.GetColumnId(column.Key);
            }

            var groups = Regex.Replace(text, @"\([^\)\(]*\)", m =>
                {
                    var txt = Helper.RemoveTags(m.ToString());
                    var values = txt.Split(",");
                    var row = new List<string>();

                    foreach (var value in values)
                    {
                        if (value.StartsWith(BINARY_TMP))
                        {
                            var val = value.Substring(BINARY.Length);
                            if (memory.ContainsKey(val))
                                row.Add(BINARY + memory[val]);
                            else
                                row.Add(value);
                        }
                        else
                        {
                            if (memory.ContainsKey(value))
                                row.Add(memory[value]);
                            else
                                row.Add(value);
                        }
                    }

                    if (rowAppend != null)
                        row.AddRange(rowAppend);

                    var addedToBlackBox = false;
                    // WhiteBox
                    if (whiteBox != null)
                    {
                        foreach (var column in columnIDsWhiteBox)
                        {
                            if (!whiteBox[column.Key].Contains(row[column.Value]))
                            {
                                //Register.Registers[insert.ID].AddToBlackBox(insert.Table, row[table.GetColumnId(table.PrimaryKey[0])]);
                                Console.WriteLine($"---- Adding to white box INSERT: {row[column.Value]}");
                                addedToBlackBox = true;
                            }
                        }
                    }

                    // BlackBox
                    if (blackBox != null && addedToBlackBox == false)
                    {
                        foreach (var column in columnIDs)
                        {
                            if (blackBox[column.Key].Contains(row[column.Value]))
                            {
                                Register.Registers[insert.ID].AddToBlackBox(insert.Table, row[table.GetColumnId(table.PrimaryKey[0])]);
                                Console.WriteLine($"---- Adding to black box INSERT: {row[column.Value]}");
                                addedToBlackBox = true;
                            }
                        }
                    }

                    if(!addedToBlackBox)
                        insert.Rows.Add(row);

                    return "";
                }
            );

            return true;

        }

        public object GetData()
        {
            return insert;
        }
    }
}
