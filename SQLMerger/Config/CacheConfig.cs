using System;
using System.Collections.Generic;
using System.Text;

namespace SQLMerger.Config
{
    public struct CustomAttributeOptionConfig
    {
        public string Comment { get; set; }
        public int Offset { get; set; }
        public List<string> Map { get; set; }
    }
    public class CacheConfig
    {
        public bool On { get; set; }
        public CacheAction IfMissing { get; set; } = CacheAction.Drop;
        public Dictionary<string, List<string>> Tables { get; set; } // Table name -> columns
        public List<string> IgnoreTablesWhenOn { get; set; }
        public string BaseFileOverridePath { get; set; }
        public Dictionary<string, CustomAttributeOptionConfig> CustomAttributeOption { get; set; }
    }
}
