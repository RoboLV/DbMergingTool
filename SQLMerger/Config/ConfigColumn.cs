using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Config
{
    public class ChangeWhereColumnConfig
    {
        public CopyDataWhere Where { get; set; } = new CopyDataWhere();
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
    public class ConfigColumn
    {
        //public string MoveToTable { get; set; }
        public string NewName { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public string Default { get; set; }
        public string PopulateChangeFromFile { get; set; }
        public Dictionary<string,string> Change { get; set; }
        public List<ChangeWhereColumnConfig> ChangeWhere { get; set; }
        public bool DropIfMissingFk { get; set; } = false;
        //public bool IsLikable { get; set; } = true;
    }
}
