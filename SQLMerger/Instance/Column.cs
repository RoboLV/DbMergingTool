using System;
using System.Collections.Generic;
using System.Text;
using SQLMerger.Config;

namespace SQLMerger.Instance
{
    public class Column : ConfigColumn
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string OnUpdate { get; set; }
        public string Description { get; set; }
    }
}
