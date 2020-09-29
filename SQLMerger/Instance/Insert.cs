using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQLMerger.Instance
{
    public class Insert
    {
        public int ID { get; set; }
        public string Table { get; set; }
        public string InsertColumnTxt { get; set; }
        public List<List<string>> Rows { get; set; } = new List<List<string>>();
       // public Dictionary<string, string> ChangedValue { get; set; }

        public void GetSql(in StreamWriter writer)
        {
            if(Rows.Count == 0)
                return;

            writer.Write($"INSERT INTO `{Table}` ({InsertColumnTxt}) VALUES ");
            for(var i = 0; i < Rows.Count - 1; i++) 
                writer.Write($"({string.Join(',', Rows[i])}),");
            writer.WriteLine($"({string.Join(',', Rows[^1])});");

            Console.WriteLine($"-- Insert for table {Table}: Done");
        }
    }
}
