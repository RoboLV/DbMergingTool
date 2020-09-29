using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Handlers
{
    public static class CatalogCategoryPath
    {
        public static void Run(Insert insert)
        {
            /*
             * 0 - entity_id
             * 1 - attribute_set_id
             * 2 - parent_id
             * 3 - created_at
             * 4 - updated_at
             * 5 - path
             */
            foreach (var row in insert.Rows)
            {
                var path = Helper.RemoveTags(row[5]);
                var segments = path.Split('/');

                // Updates last ID (own ID)
                segments[^1] = row[0];

                // Updates rest of IDs
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    int value;
                    if (segments[i] != "" && int.TryParse(segments[i], out value))
                    {
                        try
                        {
                            segments[i] = Register.Registers[insert.ID].PrimaryKeys[insert.Table][segments[i]];
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }

                row[5] = "'" + string.Join('/', segments) + "'";
            }
        }
    }
}
