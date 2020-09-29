using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class SkuRegister
    {
        public static Dictionary<string, string> Register { get; set; } = new Dictionary<string, string>();

        public static void Add(string oldSku, string newSku)
        {
            if(Register.ContainsKey(oldSku))
                return;

            Register.Add(oldSku, newSku);
        }

        public static void Update(Table table)
        {
            foreach (var insert in table.Inserts)
            {
                foreach (var row in insert.Rows)
                {
                    if(!Register.ContainsKey(row[2]))
                        continue;

                    row[2] = Register[row[2]];
                }
            }
        }
    
    }
}
