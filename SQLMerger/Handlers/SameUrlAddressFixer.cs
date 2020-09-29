using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Handlers
{
    public static class SameUrlAddressFixer
    {
        private const string TABLE = "catalog_product_entity_varchar";
        private const string REPEAT_HASH = "-item-";
        /**
         * 0 - value_id,
         * 1 - attribute_id,
         * 2 - store_id,
         * 3 - entity_id,
         * 4 - value
         */
        public static void Run(FileInstance file, List<string> args)
        {
            if (!file.Tables.ContainsKey(TABLE))
                return;

            var memory = new Dictionary<int, Dictionary<string, bool>>();
            var lastId = 1;

            foreach (var insert in file.Tables[TABLE].Inserts)
            {
                foreach (var row in insert.Rows)
                {
                    if(row[1] != args[0]) 
                        continue;

                    var store = int.Parse(row[2]);
                    if (!memory.ContainsKey(store))
                        memory.Add(store, new Dictionary<string, bool>());

                    if (memory[store].ContainsKey(row[4]))
                        row[4] = "'" + Helper.RemoveTags(row[4]) + REPEAT_HASH + lastId++ + "'";

                    memory[store].Add(row[4], true);
                }
            }
        }
    }
}
