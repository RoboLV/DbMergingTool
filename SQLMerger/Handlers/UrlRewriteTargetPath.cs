using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLMerger.Instance;
using SQLMerger.Merger;

namespace SQLMerger.Handlers
{
    public static class UrlRewriteTargetPath
    {
        public static void Run(Insert insert)
        {
            /*
             * 0 - url_rewrite_id
             * 1 - entity_type
             * 2 - entity_id
             * 3 - request_path
             * 4 - target_path
             */
            foreach (var row in insert.Rows)
            {
                if(row[4].Length > 1 && row[4][^2] == '/')
                    continue;

                var path = Helper.RemoveTags(row[4]);
                var segments = path.Split('/');

                // Updates last ID (target ID)
                segments[^1] = row[2];

                row[4] = "'" + string.Join('/', segments) + "'";
            }
        }
    }
}
