using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLMerger.Instance
{
    public struct FKRef
    {
        public string Title { get; set; }
        public string Table { get; set; }
        public string Column { get; set; }
        public string Action { get; set; }
    }
    public class Table: ISql
    {
        public int ID { get; set; }

        /**
         * TABLE INFO
         */
        public string Name { get; set; }
        public string NewName { get; set; }
        public string Description { get; set; }
        public string Ending { get; set; }

        /**
         * COLUMN DATA
         */
        public Dictionary<string, Column> Columns { get; set; } = new Dictionary<string, Column>();

        /**
         * KEYS
         */
        public string[] PrimaryKey { get; set; }
        public Dictionary<string, FKRef> ForeignKeys { get; set; } = new Dictionary<string, FKRef>();
        public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, List<string>> UniqueKey { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> FullTextKey { get; set; } = new Dictionary<string, List<string>>();

        /**
         * INSERTS TO THIS TABLE
         */
        public List<Insert> Inserts { get; set; } = new List<Insert>();

        public bool AddedCopy { get; set; } = false;

        public int GetColumnId(string column)
        {
            if (Columns.ContainsKey(column))
            {
                return Columns[column].Order;
            }

            return -1;
        }

        public void BuildInsertColumnText()
        {
            var columns_txt = string.Join(',', Columns.Keys);
            foreach (var insert in Inserts)
            {
                insert.InsertColumnTxt = columns_txt;
            }
        }

        public void GetSql(in StreamWriter writer)
        {
            //Console.WriteLine();
            //Console.WriteLine($"Table Start: {Name}");

            //var output = $"DROP TABLE IF EXISTS `{Name}`;\n";
            //output += $"CREATE TABLE `{Name}` (\n";

            //foreach (var column in Columns)
            //{
            //    var nullTxt = column.Value.IsRequired ? "NOT NULL" : "NULL";

            //    var updateTxt = string.IsNullOrEmpty(column.Value.OnUpdate)
            //        ? ""
            //        : $"ON UPDATE {column.Value.OnUpdate} ";
            //    var autoTxt = column.Key == PrimaryKey ? "AUTO_INCREMENT " : "";
            //    var defaultTxt = column.Value.Default == "@@Empty@@" ? "DEFAULT '' " :
            //        string.IsNullOrEmpty(column.Value.Default) ? "" : $"DEFAULT {column.Value.Default} ";
            //    var commentTxt = string.IsNullOrEmpty(column.Value.Description)
            //        ? ""
            //        : $"COMMENT '{column.Value.Description}'";

            //    output += $"  `{column.Value.Name}` {column.Value.DataType} {nullTxt} {autoTxt}{defaultTxt}{updateTxt}{commentTxt},\n";
            //}

            //if (!string.IsNullOrEmpty(PrimaryKey))
            //{
            //    output += $"  PRIMARY KEY (`{PrimaryKey}`),\n";
            //}

            //foreach (var key in UniqueKey)
            //{
            //    List<string> newUnique = new List<string>();
            //    foreach (var column in key.Value)
            //    {
            //        newUnique.Add($"`{column}`");
            //    }
            //    output += $"  UNIQUE KEY `{key.Key}` ({string.Join(',', newUnique)}),\n";
            //}

            //foreach (var key in Keys)
            //{
            //    output += $"  KEY `{key.Value}` (`{key.Key}`),\n";
            //}

            //foreach (var key in FullTextKey)
            //{
            //    List<string> newFullText = new List<string>();
            //    foreach (var column in key.Value)
            //    {
            //        newFullText.Add($"`{column}`");
            //    }
            //    output += $"  FULLTEXT KEY `{key.Key}` ({string.Join(',', newFullText)}),\n";
            //}

            //foreach (var key in ForeignKeys)
            //{
            //    output += $"  CONSTRAINT `{key.Value.Title}` FOREIGN KEY (`{key.Key}`) REFERENCES `{key.Value.Table}` (`{key.Value.Column}`) {key.Value.Action},\n";
            //}

            //if (output[^1] == ',')
            //{
            //    output = output.Substring(0, output.Length - 1) + '\n';
            //}else if (output[^2] == ',')
            //{
            //    output = output.Substring(0, output.Length - 2) + '\n';
            //}

            //output += Ending;
            //writer.WriteLine(output);
            writer.WriteLine();
            writer.WriteLine($"SELECT 'Starting table: {Name}' AS msg;");
            writer.WriteLine($"LOCK TABLES `{Name}` WRITE;");
            writer.WriteLine($"TRUNCATE TABLE `{Name}`;");
            foreach (var insert in Inserts)
            {
                insert.GetSql(in writer);
            }
            writer.WriteLine("UNLOCK TABLES;");
            writer.WriteLine($"SELECT '--- DONE: {Name}' AS msg;");
            writer.WriteLine();
            writer.WriteLine();
        }
    }
}
