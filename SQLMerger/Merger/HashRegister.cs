using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Merger
{
    public struct HashValue
    {
        public int FileId { get; set; }
        public int InsertId { get; set; }
        public int RowId { get; set; }
    }

    public static class HashRegister
    {
        // string table, string hash, int file_id, int insert_id, row_id
        public static Dictionary<string, Dictionary<string, HashValue>> register = new Dictionary<string, Dictionary<string, HashValue>>();

        public static HashValue? Find(string table, string hash)
        {
            if (register.ContainsKey(table) && register[table].ContainsKey(hash))
                return register[table][hash];

            return null;
        }

        public static void AddHashValue(string table, string hash, HashValue data)
        {
            if(!register.ContainsKey(table))
                register.Add(table, new Dictionary<string, HashValue>());
            if(!register[table].ContainsKey(hash))
                register[table].Add(hash, data);
        }
    }
}
