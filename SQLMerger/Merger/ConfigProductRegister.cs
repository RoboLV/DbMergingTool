using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Instance;

namespace SQLMerger.Merger
{
    public static class ConfigProductRegister
    {
        public static Dictionary<int, Dictionary<int,bool>> ProductList = new Dictionary<int, Dictionary<int, bool>>();
        public static List<string> ConfigParent = new List<string>();

        public static void Build(Table table)
        {
            if (!ProductList.ContainsKey(table.ID))
            {
                ProductList.Add(table.ID, new Dictionary<int, bool>());
            }

            int productId;
            foreach (var insert in table.Inserts)
            {
                foreach (var row in insert.Rows)
                {
                    productId = int.Parse(row[1]);
                    if(!ProductList[table.ID].ContainsKey(productId))
                        ProductList[table.ID].Add(productId, true);
                }
            }
        }
    }
}
